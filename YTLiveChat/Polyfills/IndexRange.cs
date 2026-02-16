#if NETSTANDARD2_0

#pragma warning disable IDE0130 // Does not match folder structure
#pragma warning disable IDE0290 // Use primary constructor
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace System;

public readonly struct Index : IEquatable<Index>
{
    private readonly int _value;

    public int Value => _value < 0 ? ~_value : _value;
    public bool IsFromEnd => _value < 0;

    public Index(int value, bool fromEnd = false)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Index must not be negative.");
        _value = fromEnd ? ~value : value;
    }

    public static implicit operator Index(int value) => FromStart(value);

    public int GetOffset(int length)
    {
        int offset = _value;
        if (IsFromEnd) offset += length + 1;
        return offset;
    }

    public static Index FromStart(int value) => new(value);
    public static Index FromEnd(int value) => new(value, true);

    public override bool Equals(object value) => value is Index index && _value == index._value;
    public bool Equals(Index other) => _value == other._value;
    public override int GetHashCode() => _value;
    public override string ToString() => IsFromEnd ? "^" + Value.ToString() : Value.ToString();
}

public readonly struct Range : IEquatable<Range>
{
    public Index Start { get; }
    public Index End { get; }

    public Range(Index start, Index end)
    {
        Start = start;
        End = end;
    }

    public override bool Equals(object value) => value is Range r && Start.Equals(r.Start) && End.Equals(r.End);
    public bool Equals(Range other) => Start.Equals(other.Start) && End.Equals(other.End);
    public override int GetHashCode() => (Start.GetHashCode() * 31) + End.GetHashCode();
    public override string ToString() => Start + ".." + End;

    public static Range StartAt(Index start) => new(start, new Index(0, true)); // 0 from end
    public static Range EndAt(Index end) => new(new Index(0, false), end); // 0 from start
    public static Range All => new(new Index(0, false), new Index(0, true));

    // This method is required by the compiler for the [..] syntax
    public (int Offset, int Length) GetOffsetAndLength(int length)
    {
        int start = Start.GetOffset(length);
        int end = End.GetOffset(length);

        return (uint)end > (uint)length || (uint)start > (uint)end
            ? throw new ArgumentOutOfRangeException(nameof(length))
            : ((int Offset, int Length))(start, end - start);
    }
}


#pragma warning restore IDE0130 // Does not match folder structure
#pragma warning restore IDE0290 // Use primary constructor
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

#endif
