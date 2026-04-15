using System;
using Newtonsoft.Json;

// POCO DTOs deserialized from level JSON. Kept separate from runtime model so
// serialization concerns don't leak into gameplay code.
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
    [JsonProperty("edge")]     public string Edge;
    [JsonProperty("position")] public int Position;
    [JsonProperty("width")]    public int Width;
    [JsonProperty("color")]    public string Color;
}

[Serializable]
public sealed class BlockDto
{
    [JsonProperty("shape")]     public string Shape;
    [JsonProperty("origin")]    public GridCoordDto Origin;
    [JsonProperty("rotation")]  public int Rotation;
    [JsonProperty("color")]     public string Color;
    [JsonProperty("modifiers")] public BlockModifiersDto Modifiers;
}

[Serializable]
public sealed class BlockModifiersDto
{
    // Remaining global grinds required to reveal this block's color. 0 = starts un-iced.
    [JsonProperty("iced")]     public int Iced;
    [JsonProperty("axisLock")] public string AxisLock;
}
