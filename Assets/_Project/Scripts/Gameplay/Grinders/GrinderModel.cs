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
