using System;
using UnityEngine;

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

    public void Build(LevelPayload payload)
    {
        if (payload == null) throw new ArgumentNullException(nameof(payload));
        if (blockCatalog == null) throw new InvalidOperationException("BlockDefinitionCatalog is not assigned on the LifetimeScope.");
        if (grinderCatalog == null) throw new InvalidOperationException("GrinderDefinitionCatalog is not assigned on the LifetimeScope.");
        if (palette == null) throw new InvalidOperationException("ColorPalette is not assigned on the LifetimeScope.");

        // Validate first so authoring mistakes surface before any spawn.
        var validation = LevelValidator.Validate(payload, palette, blockCatalog.AsDictionary());
        foreach (var issue in validation.Issues)
        {
            if (issue.IsError) Debug.LogError("[LevelBuilder] " + issue);
            else               Debug.LogWarning("[LevelBuilder] " + issue);
        }
        if (validation.HasErrors)
            throw new InvalidOperationException($"Level '{payload.Id}' failed validation; see console for details.");

        var size = new GridSize(payload.GridSize.X, payload.GridSize.Y);
        var grid = new GridModel(size);
        ApplyWalls(payload, grid);

        gridBuilder.Build(grid, levelRoot);

        blockModelFactory.Reset();
        SpawnBlocks(payload, grid);

        SpawnGrinders(payload, size);

        gridBuilder.BuildEdgeWalls(grid, payload.Grinders, levelRoot);

        levelContext.Bind(grid, levelRoot);

        if (cameraFitter != null)
            cameraFitter.Fit(size, cellSpace);
    }

    public void Teardown()
    {
        viewRegistry.Clear();
        blockViewFactory.Clear();
        grinderViewFactory.Clear();
        gridBuilder.Clear(levelRoot);
        levelContext.Clear();
    }

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

            var edge = GridEdgeExtensions.TryParse(dto.Edge, out var parsed) ? parsed : GridEdge.Top;
            var model = new GrinderModel(i + 1, edge, dto.Position, dto.Width, dto.Color);

            grinderViewFactory.Acquire(model, definition, gridSize);
            grinderService.Register(model);
        }
    }

    private static BlockAxisLock ParseAxisLock(string value)
        => System.Enum.TryParse<BlockAxisLock>(value, ignoreCase: false, out var a) ? a : BlockAxisLock.None;
}
