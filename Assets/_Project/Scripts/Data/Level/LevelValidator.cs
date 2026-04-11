using System.Collections.Generic;
using System.Text;

/// <summary>
/// Semantic validator for <see cref="LevelPayload"/>. Runs at level load
/// time and in tests to catch authoring mistakes before the runtime builder
/// ever tries to spawn anything. Distinct from deserialization (which only
/// checks JSON correctness); this layer knows about palettes, definitions,
/// grid bounds and the project's win rules.
///
/// Lives in the Data assembly so the runtime LevelBuilder can invoke it;
/// the validator itself has no editor-only dependencies.
///
/// The validator never throws: it returns a <see cref="Result"/> collecting
/// every issue found so tooling can show them in bulk.
/// </summary>
public static class LevelValidator
{
    public sealed class Issue
    {
        public readonly string Message;
        public readonly bool IsError;

        public Issue(string message, bool isError)
        {
            Message = message;
            IsError = isError;
        }

        public override string ToString() => (IsError ? "[ERROR] " : "[WARN] ") + Message;
    }

    public sealed class Result
    {
        public readonly List<Issue> Issues = new List<Issue>();

        public bool HasErrors
        {
            get
            {
                for (int i = 0; i < Issues.Count; i++)
                    if (Issues[i].IsError) return true;
                return false;
            }
        }

        public string ToMultiLineString()
        {
            if (Issues.Count == 0) return "OK";
            var sb = new StringBuilder();
            for (int i = 0; i < Issues.Count; i++)
            {
                if (i > 0) sb.AppendLine();
                sb.Append(Issues[i].ToString());
            }
            return sb.ToString();
        }

        internal void Error(string message) => Issues.Add(new Issue(message, true));
        internal void Warn(string message)  => Issues.Add(new Issue(message, false));
    }

    /// <summary>
    /// Validates the payload against the given palette and a shape-id -&gt;
    /// <see cref="BlockDefinition"/> lookup. The caller is responsible for
    /// providing a complete catalog; unknown shapes become errors.
    /// </summary>
    public static Result Validate(
        LevelPayload payload,
        ColorPalette palette,
        IReadOnlyDictionary<string, BlockDefinition> definitionsByShapeId)
    {
        var result = new Result();

        if (payload == null)
        {
            result.Error("Level payload is null.");
            return result;
        }

        ValidateGridSize(payload, result);
        ValidateTimer(payload, result);

        var occupancy = new HashSet<(int x, int y)>();
        ValidateBlocks(payload, palette, definitionsByShapeId, occupancy, result);
        ValidateWalls(payload, occupancy, result);
        ValidateGrinders(payload, palette, result);
        ValidateColorCoverage(payload, result);

        return result;
    }

    // ----- individual rules -----

    private static void ValidateGridSize(LevelPayload payload, Result result)
    {
        if (payload.GridSize == null || payload.GridSize.X <= 0 || payload.GridSize.Y <= 0)
        {
            result.Error("Invalid grid size.");
            return;
        }

        // Case study requires 4x4..6x6, anything outside is suspicious but not fatal.
        if (payload.GridSize.X < 4 || payload.GridSize.Y < 4 ||
            payload.GridSize.X > 6 || payload.GridSize.Y > 6)
        {
            result.Warn($"Grid size {payload.GridSize.X}x{payload.GridSize.Y} is outside the expected 4x4..6x6 range.");
        }
    }

    private static void ValidateTimer(LevelPayload payload, Result result)
    {
        if (payload.TimeLimit <= 0f)
            result.Error("Time limit must be greater than 0.");
    }

    private static void ValidateBlocks(
        LevelPayload payload,
        ColorPalette palette,
        IReadOnlyDictionary<string, BlockDefinition> definitionsByShapeId,
        HashSet<(int x, int y)> occupancy,
        Result result)
    {
        if (payload.Blocks == null) return;

        for (int i = 0; i < payload.Blocks.Length; i++)
        {
            var b = payload.Blocks[i];
            if (b == null)
            {
                result.Error($"Block[{i}] is null.");
                continue;
            }

            if (string.IsNullOrEmpty(b.Shape) ||
                definitionsByShapeId == null ||
                !definitionsByShapeId.TryGetValue(b.Shape, out var def) ||
                def == null)
            {
                result.Error($"Block[{i}] references unknown shape '{b.Shape}'.");
                continue;
            }

            if (palette == null || !palette.TryGet(b.Color, out _))
                result.Error($"Block[{i}] references unknown color '{b.Color}'.");

            if (b.Origin == null)
            {
                result.Error($"Block[{i}] origin is null.");
                continue;
            }

            var offsets = def.CellOffsets;
            if (offsets == null || offsets.Length == 0)
            {
                result.Error($"Block[{i}] shape '{b.Shape}' has no cells defined.");
                continue;
            }

            for (int c = 0; c < offsets.Length; c++)
            {
                // Rotation handling lives in the builder; Phase 1 only validates
                // the raw authored layout, so rotation is intentionally ignored here.
                int worldX = b.Origin.X + offsets[c].x;
                int worldY = b.Origin.Y + offsets[c].y;

                if (payload.GridSize != null &&
                    (worldX < 0 || worldY < 0 ||
                     worldX >= payload.GridSize.X || worldY >= payload.GridSize.Y))
                {
                    result.Error($"Block[{i}] cell ({worldX},{worldY}) is outside the grid.");
                }

                if (!occupancy.Add((worldX, worldY)))
                    result.Error($"Block[{i}] overlaps another block at ({worldX},{worldY}).");
            }
        }
    }

    private static void ValidateWalls(LevelPayload payload, HashSet<(int x, int y)> occupancy, Result result)
    {
        if (payload.Walls == null) return;

        for (int i = 0; i < payload.Walls.Length; i++)
        {
            var w = payload.Walls[i];
            if (w == null) continue;
            if (occupancy.Contains((w.X, w.Y)))
                result.Error($"Wall[{i}] at ({w.X},{w.Y}) overlaps a block.");
        }
    }

    private static void ValidateGrinders(LevelPayload payload, ColorPalette palette, Result result)
    {
        if (payload.Grinders == null) return;

        for (int i = 0; i < payload.Grinders.Length; i++)
        {
            var g = payload.Grinders[i];
            if (g == null)
            {
                result.Error($"Grinder[{i}] is null.");
                continue;
            }

            if (g.Width < 1 || g.Width > 3)
                result.Error($"Grinder[{i}] width {g.Width} is out of the supported 1..3 range.");

            if (palette == null || !palette.TryGet(g.Color, out _))
                result.Error($"Grinder[{i}] references unknown color '{g.Color}'.");

            if (!TryParseEdge(g.Edge, out _))
                result.Error($"Grinder[{i}] references unknown edge '{g.Edge}'.");
        }
    }

    private static void ValidateColorCoverage(LevelPayload payload, Result result)
    {
        // Every block color must have at least one grinder that can accept it.
        // This is a solvability smoke-check, not a proper solver.
        if (payload.Blocks == null || payload.Blocks.Length == 0) return;

        var grinderColors = new HashSet<string>();
        if (payload.Grinders != null)
        {
            for (int i = 0; i < payload.Grinders.Length; i++)
            {
                var g = payload.Grinders[i];
                if (g != null && !string.IsNullOrEmpty(g.Color))
                    grinderColors.Add(g.Color);
            }
        }

        var seen = new HashSet<string>();
        for (int i = 0; i < payload.Blocks.Length; i++)
        {
            var b = payload.Blocks[i];
            if (b == null || string.IsNullOrEmpty(b.Color)) continue;
            if (!seen.Add(b.Color)) continue;
            if (!grinderColors.Contains(b.Color))
                result.Error($"Color '{b.Color}' has blocks but no matching grinder.");
        }
    }

    private static bool TryParseEdge(string edge, out GridEdge result)
    {
        switch (edge)
        {
            case "Top":    result = GridEdge.Top;    return true;
            case "Bottom": result = GridEdge.Bottom; return true;
            case "Left":   result = GridEdge.Left;   return true;
            case "Right":  result = GridEdge.Right;  return true;
            default:       result = GridEdge.Top;    return false;
        }
    }
}
