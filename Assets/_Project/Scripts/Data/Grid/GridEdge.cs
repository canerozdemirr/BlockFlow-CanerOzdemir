/// <summary>
/// The four outer edges of a puzzle grid. Used when placing grinders: the
/// level data only has to specify which edge a grinder sits on and the index
/// along that edge, independent of grid dimensions.
/// </summary>
public enum GridEdge
{
    Top,
    Bottom,
    Left,
    Right
}
