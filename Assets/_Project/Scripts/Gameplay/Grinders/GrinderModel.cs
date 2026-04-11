/// <summary>
/// Runtime state of a single grinder placed on the edge of the board. As with
/// <see cref="BlockModel"/>, this is deliberately pure C#: no MonoBehaviour,
/// no SO references, so the consumption logic added in Phase 6 stays
/// unit-testable. Grinders are immutable once authored — only their
/// "consumed block count" changes, and that is tracked on a higher-level
/// service rather than the model itself.
/// </summary>
public sealed class GrinderModel
{
    public int Id { get; }
    public GridEdge Edge { get; }
    public int Position { get; }
    public int Width { get; }
    public string ColorId { get; }

    public GrinderModel(int id, GridEdge edge, int position, int width, string colorId)
    {
        Id = id;
        Edge = edge;
        Position = position;
        Width = width;
        ColorId = colorId;
    }
}
