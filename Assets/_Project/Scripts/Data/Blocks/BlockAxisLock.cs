/// <summary>
/// Movement constraint applied to a single block instance. Set per-block in
/// the level JSON; the runtime movement strategy reads this value to decide
/// whether a drag delta is legal.
/// </summary>
public enum BlockAxisLock
{
    None,
    Horizontal,
    Vertical
}
