using System;

// None (default/Value=0) is the sentinel for empty cells.
public readonly struct BlockId : IEquatable<BlockId>
{
    public readonly int Value;

    public BlockId(int value)
    {
        Value = value;
    }

    public static BlockId None => default;

    public bool IsValid => Value != 0;

    public bool Equals(BlockId other) => Value == other.Value;
    public override bool Equals(object obj) => obj is BlockId other && Equals(other);
    public override int GetHashCode() => Value;

    public static bool operator ==(BlockId a, BlockId b) => a.Value == b.Value;
    public static bool operator !=(BlockId a, BlockId b) => a.Value != b.Value;

    public override string ToString() => IsValid ? $"Block#{Value}" : "Block#None";
}
