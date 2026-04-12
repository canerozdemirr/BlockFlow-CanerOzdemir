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

    [SerializeField, Tooltip("Wall prefab stamped on cells flagged IsWall.")]
    private GameObject wallPrefab;

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

        // ---------- per-level state ----------

        builder.Register<LevelContext>(Lifetime.Singleton);
        builder.Register<BlockViewRegistry>(Lifetime.Singleton);

        // ---------- factories ----------

        builder.Register<IBlockModelFactory, BlockModelFactory>(Lifetime.Singleton);

        builder.Register<IBlockViewFactory>(container => new BlockViewFactory(
            defaultPalette,
            container.Resolve<CellSpace>(),
            viewParent),
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
            container.Resolve<CellSpace>()),
            Lifetime.Singleton);

        // ---------- input & drag ----------

        builder.Register<IInputService, UnityPointerInputService>(Lifetime.Singleton);
        builder.RegisterInstance(SingleAxisMovementStrategy.Instance).As<IMovementStrategy>();
        builder.RegisterEntryPoint<DragController>();

        // ---------- core loop (Phase 6) ----------
        // .AsSelf() on GameStateService and GrinderService because other
        // services resolve them by concrete type (not just via interfaces).

        builder.RegisterEntryPoint<GameStateService>().AsSelf();
        builder.RegisterEntryPoint<CountdownTimer>();
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

        // ---------- progression (Phase 7) ----------

        builder.Register(_ => new LevelProgressionService(levelCatalog), Lifetime.Singleton);

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
