using System;
using Newtonsoft.Json;

/// <summary>
/// Pure POCO representation of a level exactly as it exists in JSON. These DTO
/// classes are the boundary between on-disk data and the rest of the codebase:
/// they are deserialized by Newtonsoft and then handed to the level validator
/// and runtime builder, which translate them into real domain types.
///
/// Keeping the DTOs separate from the runtime model keeps serialization
/// concerns (nullable fields, string enums, loose typing) out of the
/// gameplay code and protects us from schema churn leaking into the simulation.
/// </summary>
[Serializable]
public sealed class LevelPayload
{
    [JsonProperty("id")]        public string Id;
    [JsonProperty("gridSize")]  public GridSizeDto GridSize;
    [JsonProperty("timeLimit")] public float TimeLimit;
    [JsonProperty("walls")]     public GridCoordDto[] Walls;
    [JsonProperty("grinders")]  public GrinderDto[] Grinders;
    [JsonProperty("blocks")]    public BlockDto[] Blocks;
}

[Serializable]
public sealed class GridSizeDto
{
    [JsonProperty("x")] public int X;
    [JsonProperty("y")] public int Y;
}

[Serializable]
public sealed class GridCoordDto
{
    [JsonProperty("x")] public int X;
    [JsonProperty("y")] public int Y;
}

[Serializable]
public sealed class GrinderDto
{
    /// <summary>"Top" | "Bottom" | "Left" | "Right" — parsed to <see cref="GridEdge"/> in the builder.</summary>
    [JsonProperty("edge")]     public string Edge;

    /// <summary>Offset along the edge, 0-indexed.</summary>
    [JsonProperty("position")] public int Position;

    /// <summary>Maximum accepted block edge width; 1, 2, or 3.</summary>
    [JsonProperty("width")]    public int Width;

    /// <summary>Color id referenced in the level's palette.</summary>
    [JsonProperty("color")]    public string Color;
}

[Serializable]
public sealed class BlockDto
{
    /// <summary>Shape id matching a <see cref="BlockDefinition.ShapeId"/>.</summary>
    [JsonProperty("shape")]     public string Shape;

    /// <summary>Origin cell of the block on the grid.</summary>
    [JsonProperty("origin")]    public GridCoordDto Origin;

    /// <summary>Rotation in degrees, quantized to 0 / 90 / 180 / 270.</summary>
    [JsonProperty("rotation")]  public int Rotation;

    /// <summary>Color id referenced in the level's palette.</summary>
    [JsonProperty("color")]     public string Color;

    [JsonProperty("modifiers")] public BlockModifiersDto Modifiers;
}

[Serializable]
public sealed class BlockModifiersDto
{
    /// <summary>
    /// Remaining global grinds required to reveal this block's color.
    /// 0 means the block starts un-iced.
    /// </summary>
    [JsonProperty("iced")]     public int Iced;

    /// <summary>"None" | "Horizontal" | "Vertical" — parsed to <see cref="BlockAxisLock"/> in the builder.</summary>
    [JsonProperty("axisLock")] public string AxisLock;
}
