using System;
using UnityEngine;

/// <summary>
/// Translates a deserialized <see cref="LevelPayload"/> into a fully live
/// level: <see cref="GridModel"/> populated with blocks and walls, views
/// spawned and registered, grinders placed, camera fitted, context bound.
/// Every Phase 3/4 service is pulled through the constructor, which makes
/// the build pipeline explicit and easy to swap out in tests or scenarios.
///
/// Responsibility split:
///
/// <list type="bullet">
///   <item><see cref="LevelLoader"/> owns JSON &rarr; payload.</item>
///   <item>This class owns payload &rarr; runtime state.</item>
///   <item><see cref="LevelRunner"/> composes the two and owns lifecycle.</item>
/// </list>
///
/// Validation runs first so authoring mistakes surface as console errors
/// before any objects are spawned. Partial spawning on validation failure
/// is avoided by bailing early.
/// </summary>
public sealed class LevelBuilder
{
    private readonly BlockDefinitionCatalog blockCatalog;
    private readonly GrinderDefinitionCatalog grinderCatalog;
    private readonly ColorPalette palette;
    private readonly IBlockModelFactory blockModelFactory;
    private readonly IBlockViewFactory blockViewFactory;
    private readonly IGrinderViewFactory grinderViewFactory;
    private readonly BlockViewRegistry viewRegistry;
    private readonly LevelContext levelContext;
    private readonly CellSpace cellSpace;
    private readonly GameplayCameraFitter cameraFitter;
    private readonly GridBuilder gridBuilder;
    private readonly GrinderService grinderService;
    private readonly Transform levelRoot;

    public LevelBuilder(
        BlockDefinitionCatalog blockCatalog,
        GrinderDefinitionCatalog grinderCatalog,
        ColorPalette palette,
        IBlockModelFactory blockModelFactory,
        IBlockViewFactory blockViewFactory,
        IGrinderViewFactory grinderViewFactory,
        BlockViewRegistry viewRegistry,
        LevelContext levelContext,
        CellSpace cellSpace,
        GameplayCameraFitter cameraFitter,
        GridBuilder gridBuilder,
        GrinderService grinderService,
        Transform levelRoot)
    {
        this.blockCatalog       = blockCatalog;
        this.grinderCatalog     = grinderCatalog;
        this.palette            = palette;
        this.blockModelFactory  = blockModelFactory;
        this.blockViewFactory   = blockViewFactory;
        this.grinderViewFactory = grinderViewFactory;
        this.viewRegistry       = viewRegistry;
        this.levelContext       = levelContext;
        this.cellSpace          = cellSpace;
        this.cameraFitter       = cameraFitter;
        this.gridBuilder        = gridBuilder;
        this.grinderService     = grinderService;
        this.levelRoot          = levelRoot;
    }

    // ---------- build ----------

    public void Build(LevelPayload payload)
    {
        if (payload == null) throw new ArgumentNullException(nameof(payload));
        if (blockCatalog == null) throw new InvalidOperationException("BlockDefinitionCatalog is not assigned on the LifetimeScope.");
        if (grinderCatalog == null) throw new InvalidOperationException("GrinderDefinitionCatalog is not assigned on the LifetimeScope.");
        if (palette == null) throw new InvalidOperationException("ColorPalette is not assigned on the LifetimeScope.");

        // 1. Semantic validation — fail fast before any allocations.
        var validation = LevelValidator.Validate(payload, palette, blockCatalog.AsDictionary());
        foreach (var issue in validation.Issues)
        {
            if (issue.IsError) Debug.LogError("[LevelBuilder] " + issue);
            else               Debug.LogWarning("[LevelBuilder] " + issue);
        }
        if (validation.HasErrors)
            throw new InvalidOperationException($"Level '{payload.Id}' failed validation; see console for details.");

        // 2. Grid model + walls.
        var size = new GridSize(payload.GridSize.X, payload.GridSize.Y);
        var grid = new GridModel(size);
        ApplyWalls(payload, grid);

        // 3. Static visual layer: ground tiles + wall instances.
        gridBuilder.Build(grid, levelRoot);

        // 4. Block spawn pass.
        blockModelFactory.Reset();
        SpawnBlocks(payload, grid);

        // 5. Grinder spawn pass.
        SpawnGrinders(payload, size);

        // 5b. Fill uncovered edge cells with walls.
        gridBuilder.BuildEdgeWalls(grid, payload.Grinders, levelRoot);

        // 6. Bind the level context so other systems (drag, UI, evaluators) can read it.
        levelContext.Bind(grid, levelRoot);

        // 7. Fit the camera to the freshly built grid.
        if (cameraFitter != null)
            cameraFitter.Fit(size, cellSpace);
    }

    // ---------- teardown ----------

    public void Teardown()
    {
        viewRegistry.Clear();
        blockViewFactory.Clear();
        grinderViewFactory.Clear();
        gridBuilder.Clear(levelRoot);
        levelContext.Clear();
    }

    // ---------- spawn helpers ----------

    private static void ApplyWalls(LevelPayload payload, GridModel grid)
    {
        if (payload.Walls == null) return;
        for (int i = 0; i < payload.Walls.Length; i++)
        {
            var w = payload.Walls[i];
            if (w == null) continue;
            grid.SetWall(new GridCoord(w.X, w.Y), true);
        }
    }

    private void SpawnBlocks(LevelPayload payload, GridModel grid)
    {
        if (payload.Blocks == null) return;

        for (int i = 0; i < payload.Blocks.Length; i++)
        {
            var dto = payload.Blocks[i];
            if (dto == null) continue;

            if (!blockCatalog.TryGet(dto.Shape, out var definition) || definition == null)
            {
                Debug.LogError($"[LevelBuilder] Block[{i}] references unknown shape '{dto.Shape}'.");
                continue;
            }

            var origin = dto.Origin != null
                ? new GridCoord(dto.Origin.X, dto.Origin.Y)
                : GridCoord.Zero;

            int quarterTurns = (dto.Rotation / 90) % 4;
            var axisLock = ParseAxisLock(dto.Modifiers?.AxisLock);
            int iceLevel = dto.Modifiers != null ? dto.Modifiers.Iced : 0;

            var request = new BlockSpawnRequest(
                definition.CellOffsets,
                origin,
                quarterTurns,
                dto.Color,
                axisLock,
                iceLevel);

            var model = blockModelFactory.Create(request);

            if (!grid.TryPlace(model, origin))
            {
                Debug.LogError($"[LevelBuilder] Block[{i}] ({dto.Shape}) failed to place at {origin}.");
                continue;
            }

            var view = blockViewFactory.Acquire(model, definition);
            if (view != null)
                viewRegistry.Register(model.Id, view);
        }
    }

    private void SpawnGrinders(LevelPayload payload, GridSize gridSize)
    {
        if (payload.Grinders == null) return;

        for (int i = 0; i < payload.Grinders.Length; i++)
        {
            var dto = payload.Grinders[i];
            if (dto == null) continue;

            if (!grinderCatalog.TryGet(dto.Width, out var definition) || definition == null)
            {
                Debug.LogError($"[LevelBuilder] Grinder[{i}] references unknown width '{dto.Width}'.");
                continue;
            }

            var edge = ParseEdge(dto.Edge);
            var model = new GrinderModel(i + 1, edge, dto.Position, dto.Width, dto.Color);

            grinderViewFactory.Acquire(model, definition, gridSize);
            grinderService.Register(model);
        }
    }

    // ---------- parsing ----------

    private static GridEdge ParseEdge(string value)
        => System.Enum.TryParse<GridEdge>(value, ignoreCase: false, out var e) ? e : GridEdge.Top;

    private static BlockAxisLock ParseAxisLock(string value)
        => System.Enum.TryParse<BlockAxisLock>(value, ignoreCase: false, out var a) ? a : BlockAxisLock.None;
}
