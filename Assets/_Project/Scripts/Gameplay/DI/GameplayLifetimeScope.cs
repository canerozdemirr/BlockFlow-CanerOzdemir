using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Per-level composition root. Registers every service a single round of
/// gameplay needs: the cell-space math, the model/view factories, the grid
/// builder, the input pipeline, the level loader, and the Phase 6 core loop
/// (event bus, timer, grinder service, ice melt service, evaluators, UI).
/// Scene-authored prefabs and catalogs are exposed as inspector fields so
/// designers can rewire them without touching code.
///
/// Registration order within <see cref="Configure"/> follows dependency
/// direction: primitives → data → per-level state → factories → builders →
/// input → level loading → core loop → scene-owned. This keeps the file
/// scannable; VContainer itself does not care about declaration order.
///
/// Nothing here reaches into <see cref="ProjectLifetimeScope"/> yet — the
/// project scope is still empty. As soon as persistent services (save,
/// audio bus, analytics) land, they will be resolved up the parent chain.
/// </summary>
public class GameplayLifetimeScope : LifetimeScope
{
    [Header("Scene refs")]
    [SerializeField, Tooltip("Camera fitter in the scene, positioned and sized to frame the grid.")]
    private GameplayCameraFitter cameraFitter;

    [SerializeField, Tooltip("Transform all runtime-spawned views are parented under.")]
    private Transform viewParent;

    [Header("Prefabs")]
    [SerializeField, Tooltip("Ground tile prefab stamped once per cell by the GridBuilder.")]
    private GameObject groundTilePrefab;

    [SerializeField, Tooltip("Wall prefab stamped on cells flagged IsWall and on uncovered grid edges.")]
    private GameObject wallPrefab;

    [SerializeField, Tooltip("Corner wall prefab placed at the four grid corners to bridge edge wall gaps.")]
    private GameObject cornerWallPrefab;

    [Header("Data")]
    [SerializeField, Tooltip("Palette used to resolve color ids to display colors.")]
    private ColorPalette defaultPalette;

    [SerializeField, Tooltip("Catalog of every block definition in the project.")]
    private BlockDefinitionCatalog blockCatalog;

    [SerializeField, Tooltip("Catalog of every grinder definition in the project.")]
    private GrinderDefinitionCatalog grinderCatalog;

    [SerializeField, Tooltip("Ordered level list consumed by LevelProgressionService. Optional; leave null to fall back to the bootstrapper's fallbackLevel.")]
    private LevelCatalog levelCatalog;

    [SerializeField, Tooltip("Audio cue library consumed by AudioService. Optional; missing cues no-op so the scene still runs.")]
    private AudioLibrary audioLibrary;

    [SerializeField, Tooltip("Decal material for block movement arrows. Uses SP_Arrow_BlockMove_03 texture.")]
    private Material arrowDecalMaterial;

    [Header("Feel configs")]
    [SerializeField, Tooltip("Grinder consume tween timing and slide distance.")]
    private GrinderFeelConfig grinderFeel;

    [SerializeField, Tooltip("Ice overlay tint and per-level opacity.")]
    private IceFeelConfig iceFeel;

    [SerializeField, Tooltip("UI Toolkit popup show/hide tween tuning. Assigned to the static UIToolkitPopupAnimator.Config at bootstrap.")]
    private PopupAnimationConfig popupAnimation;

    [SerializeField, Tooltip("Grind particle tint tuning. Assigned to the static BlockShatterEffect.Config at bootstrap.")]
    private ShatterFeelConfig shatterFeel;

    [Header("Tuning")]
    [SerializeField, Min(0.1f), Tooltip("World-space size of a single cell. 1 is a sensible default; bump for chunkier layouts.")]
    private float cellSize = 1f;

    [SerializeField, Tooltip("How far grinders sit beyond the grid boundary. 0 = pivot on boundary. Increase until the grinder opening is flush with tiles.")]
    private float grinderDepthOffset = 0f;

    protected override void Configure(IContainerBuilder builder)
    {
        // ---------- utilities ----------

        var cellSpace = new CellSpace(cellSize);
        builder.RegisterInstance(cellSpace);

        // ---------- core events ----------

        builder.Register<IEventBus, EventBus>(Lifetime.Singleton);

        // ---------- data (optional so the scene still compiles pre-assignment) ----------

        if (defaultPalette  != null) builder.RegisterInstance(defaultPalette);
        if (blockCatalog    != null) builder.RegisterInstance(blockCatalog);
        if (grinderCatalog  != null) builder.RegisterInstance(grinderCatalog);

        if (grinderFeel     == null) grinderFeel    = ScriptableObject.CreateInstance<GrinderFeelConfig>();
        if (iceFeel         == null) iceFeel        = ScriptableObject.CreateInstance<IceFeelConfig>();
        if (popupAnimation  == null) popupAnimation = ScriptableObject.CreateInstance<PopupAnimationConfig>();
        if (shatterFeel     == null) shatterFeel    = ScriptableObject.CreateInstance<ShatterFeelConfig>();

        builder.RegisterInstance(grinderFeel);
        builder.RegisterInstance(iceFeel);
        builder.RegisterInstance(popupAnimation);
        builder.RegisterInstance(shatterFeel);

        // BlockShatterEffect is a static utility in the Gameplay assembly; set it here.
        // UIToolkitPopupAnimator lives in the UI assembly and is wired by the popup views
        // via their own [Inject] hooks since Gameplay cannot reference UI types.
        BlockShatterEffect.Config = shatterFeel;

        // ---------- per-level state ----------

        builder.Register<LevelContext>(Lifetime.Singleton);
        builder.Register<BlockViewRegistry>(Lifetime.Singleton)
            .As<IBlockViewRegistry>()
            .AsSelf();

        // ---------- factories ----------

        builder.Register<IBlockModelFactory, BlockModelFactory>(Lifetime.Singleton);

        builder.Register<IBlockViewFactory>(container => new BlockViewFactory(
            defaultPalette,
            container.Resolve<CellSpace>(),
            viewParent,
            iceFeel),
            Lifetime.Singleton);

        builder.Register<IGrinderViewFactory>(container => new GrinderViewFactory(
            defaultPalette,
            container.Resolve<CellSpace>(),
            viewParent,
            grinderDepthOffset),
            Lifetime.Singleton);

        // ---------- builders ----------

        builder.Register(container => new GridBuilder(
            groundTilePrefab,
            wallPrefab,
            cornerWallPrefab,
            container.Resolve<CellSpace>(),
            grinderDepthOffset),
            Lifetime.Singleton);

        // ---------- input & drag ----------

        builder.Register<IInputService, UnityPointerInputService>(Lifetime.Singleton);
        builder.RegisterInstance(SingleAxisMovementStrategy.Instance).As<IMovementStrategy>();
        builder.Register<BlockInputLock>(Lifetime.Singleton);
        builder.RegisterEntryPoint<DragController>();

        // ---------- core loop (Phase 6) ----------
        // .AsSelf() on GameStateService and GrinderService because other
        // services resolve them by concrete type (not just via interfaces).

        builder.RegisterEntryPoint<GameStateService>().AsSelf();
        builder.RegisterEntryPoint<CountdownTimer>().AsSelf();
        builder.RegisterEntryPoint<GrinderService>().AsSelf();
        builder.RegisterEntryPoint<IceMeltService>();
        builder.RegisterEntryPoint<WinConditionEvaluator>();
        builder.RegisterEntryPoint<LoseConditionEvaluator>();

        // ---------- level loading ----------

        builder.Register<LevelLoader>(Lifetime.Singleton);
        builder.Register(container => new LevelBuilder(
            blockCatalog,
            grinderCatalog,
            defaultPalette,
            container.Resolve<IBlockModelFactory>(),
            container.Resolve<IBlockViewFactory>(),
            container.Resolve<IGrinderViewFactory>(),
            container.Resolve<BlockViewRegistry>(),
            container.Resolve<LevelContext>(),
            container.Resolve<CellSpace>(),
            cameraFitter,
            container.Resolve<GridBuilder>(),
            container.Resolve<GrinderService>(),
            viewParent),
            Lifetime.Singleton);
        builder.Register<LevelRunner>(Lifetime.Singleton);
        builder.RegisterEntryPoint<GameFlowController>();

        // ---------- progression (Phase 7) ----------

        builder.Register(c => new LevelProgressionService(levelCatalog, c.Resolve<ISaveRepository>()), Lifetime.Singleton);
        builder.Register<ILevelStartupStrategy>(c =>
        {
            var bootstrapper = FindFirstObjectByType<GameplayBootstrapper>();
            var fallback = bootstrapper != null ? bootstrapper.FallbackLevel : null;
            return new ProgressionOrFallbackStartupStrategy(c.Resolve<LevelProgressionService>(), fallback);
        }, Lifetime.Singleton);

        // ---------- feedback (Phase 7) ----------

        builder.Register(_ => new AudioService(audioLibrary), Lifetime.Singleton);
        builder.RegisterEntryPoint<AudioFeedbackRouter>();
        builder.RegisterEntryPoint<HapticsService>();
        builder.RegisterEntryPoint<BlockJuiceService>();

        // ---------- scene-owned ----------
        // Note: UI components (GameplayHudView, LevelOutcomePopupView) live in
        // the BlockFlow.UI assembly which Gameplay cannot reference. They receive
        // their [Inject] dependencies via VContainer's autoInjectGameObjects
        // list in the inspector — drag the Canvas root into that list on this
        // LifetimeScope and VContainer will walk it on build, injecting every
        // [Inject] method it finds without needing a type reference here.

        if (cameraFitter != null) builder.RegisterComponent(cameraFitter);
        builder.RegisterComponentInHierarchy<GameplayBootstrapper>();
    }
}
