using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// Per-level composition root. Registers every service a single round of
/// gameplay needs: the cell-space math, the model/view factories, the grid
/// builder, and the scene-owned camera fitter. Scene-authored prefabs and
/// catalogs are exposed as inspector fields so designers can rewire them
/// without touching code.
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

    [Header("Tuning")]
    [SerializeField, Min(0.1f), Tooltip("World-space size of a single cell. 1 is a sensible default; bump for chunkier layouts.")]
    private float cellSize = 1f;

    protected override void Configure(IContainerBuilder builder)
    {
        // ---------- utilities ----------

        var cellSpace = new CellSpace(cellSize);
        builder.RegisterInstance(cellSpace);

        // ---------- data (optional so the scene still compiles pre-assignment) ----------

        if (defaultPalette  != null) builder.RegisterInstance(defaultPalette);
        if (blockCatalog    != null) builder.RegisterInstance(blockCatalog);
        if (grinderCatalog  != null) builder.RegisterInstance(grinderCatalog);

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
            viewParent),
            Lifetime.Singleton);

        // ---------- builders ----------

        builder.Register(container => new GridBuilder(
            groundTilePrefab,
            wallPrefab,
            container.Resolve<CellSpace>()),
            Lifetime.Singleton);

        // ---------- scene-owned ----------

        if (cameraFitter != null) builder.RegisterComponent(cameraFitter);
    }
}
