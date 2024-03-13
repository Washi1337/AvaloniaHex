namespace AvaloniaHex.Document;

/// <summary>
/// Represents a bit offset within a binary document.
/// </summary>
public readonly struct BitLocation : IEquatable<BitLocation>, IComparable<BitLocation>
{
    /// <summary>
    /// The minimum value for a bit location.
    /// </summary>
    public static readonly BitLocation Minimum = new(0, 0);

    /// <summary>
    /// The maximum value for a bit location.
    /// </summary>
    public static readonly BitLocation Maximum = new(ulong.MaxValue, 7);

    /// <summary>
    /// Creates a new bit location.
    /// </summary>
    /// <param name="byteIndex">The byte offset within the binary document.</param>
    public BitLocation(ulong byteIndex)
        : this(byteIndex, 0)
    {
    }

    /// <summary>
    /// Creates a new bit location.
    /// </summary>
    /// <param name="byteIndex">The byte offset within the binary document.</param>
    /// <param name="bitIndex">The bit index within the referenced byte.</param>
    public BitLocation(ulong byteIndex, int bitIndex)
    {
        if (bitIndex is < 0 or >= 8)
            throw new ArgumentOutOfRangeException(nameof(bitIndex));

        ByteIndex = byteIndex;
        BitIndex = bitIndex;
    }

    /// <summary>
    /// Gets the byte offset within the binary document.
    /// </summary>
    public ulong ByteIndex
    {
        get;
    }

    /// <summary>
    /// Gets the bit index within the referenced byte.
    /// </summary>
    public int BitIndex
    {
        get;
    }

    /// <summary>
    /// Adds a number of bits to the location.
    /// </summary>
    /// <param name="bits">The bit count.</param>
    /// <returns>The new location.</returns>
    public BitLocation AddBits(ulong bits)
    {
        ulong remaining = (ulong)(7 - BitIndex);
        if (remaining >= bits)
            return new BitLocation(ByteIndex, BitIndex + (int)bits);

        bits -= remaining + 1;
        (ulong byteCount, ulong bitCount) = Math.DivRem(bits, 8);

        return new BitLocation(ByteIndex + byteCount + 1, (int)bitCount);
    }

    /// <summary>
    /// Gets the location of the previous bit in the binary document.
    /// </summary>
    /// <returns>The previous location.</returns>
    /// <exception cref="ArgumentException">
    /// Occurs when the bit location references the first bit in a binary document.
    /// </exception>
    public BitLocation Previous()
    {
        if (BitIndex == 0)
        {
            if (ByteIndex == 0)
                throw new ArgumentException("Cannot get the previous bit location before the zero offset.");

            return new BitLocation(ByteIndex - 1, 7);
        }

        return new BitLocation(ByteIndex, BitIndex - 1);
    }

    /// <summary>
    /// Gets the location of the previous bit in the binary document, or the first bit location in a binary document.
    /// </summary>
    /// <returns>The previous location.</returns>
    public BitLocation PreviousOrZero()
    {
        if (BitIndex == 0)
            return ByteIndex == 0 ? this : new BitLocation(ByteIndex - 1, 7);

        return new BitLocation(ByteIndex, BitIndex - 1);
    }

    /// <summary>
    /// Gets the location of the next bit in the binary document.
    /// </summary>
    /// <returns>The next location.</returns>
    /// <exception cref="ArgumentException">
    /// Occurs when the bit location references the last possible bit in a binary document.
    /// </exception>
    public BitLocation Next()
    {
        if (BitIndex == 7)
        {
            if (ByteIndex == ulong.MaxValue)
                throw new ArgumentException("Cannot get the next bit location after the maximum offset.");

            return new BitLocation(ByteIndex + 1, 0);
        }

        return new BitLocation(ByteIndex, BitIndex + 1);
    }

    /// <summary>
    /// Gets the location of the next bit in the binary document, or the maximum bit location possible in a binary document.
    /// </summary>
    /// <returns>The next location.</returns>
    public BitLocation NextOrMax()
    {
        if (BitIndex == 7)
            return ByteIndex == ulong.MaxValue ? this : new BitLocation(ByteIndex + 1, 0);

        return new BitLocation(ByteIndex, BitIndex + 1);
    }

    /// <summary>
    /// Chooses the lowest bit location between the current and provided locations.
    /// </summary>
    /// <param name="other">The other location.</param>
    /// <returns>The lowest bit location.</returns>
    public BitLocation Min(BitLocation other) => this > other ? other : this;

    /// <summary>
    /// Chooses the highest bit location between the current and provided locations.
    /// </summary>
    /// <param name="other">The other location.</param>
    /// <returns>The highest bit location.</returns>
    public BitLocation Max(BitLocation other) => this < other ? other : this;

    /// <summary>
    /// Restricts the current location to the provided range.
    /// </summary>
    /// <param name="range">The range.</param>
    /// <returns>The restricted location.</returns>
    public BitLocation Clamp(BitRange range) => Max(range.Start).Min(range.End);

    /// <summary>
    /// Aligns the location down to the lower byte offset.
    /// </summary>
    /// <returns>The aligned location.</returns>
    public BitLocation AlignDown() => new(ByteIndex, 0);

    /// <summary>
    /// Aligns the location down to the next byte offset.
    /// </summary>
    /// <returns>The aligned location.</returns>
    public BitLocation AlignUp() => BitIndex > 0 ? new BitLocation(ByteIndex + 1, 0) : this;

    /// <inheritdoc />
    public int CompareTo(BitLocation other)
    {
        int byteIndexComparison = ByteIndex.CompareTo(other.ByteIndex);
        if (byteIndexComparison != 0)
            return byteIndexComparison;

        return BitIndex.CompareTo(other.BitIndex);
    }

    /// <inheritdoc />
    public bool Equals(BitLocation other)
    {
        return ByteIndex == other.ByteIndex && BitIndex == other.BitIndex;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is BitLocation other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return (ByteIndex.GetHashCode() * 397) ^ BitIndex;
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"{ByteIndex:X}:{BitIndex}";

    /// <summary>
    /// Determines whether two locations are equal.
    /// </summary>
    /// <param name="a">The first location.</param>
    /// <param name="b">The second location.</param>
    /// <returns><c>true</c> if the locations are equal, <c>false</c> otherwise.</returns>
    public static bool operator ==(BitLocation a, BitLocation b) => a.Equals(b);

    /// <summary>
    /// Determines whether two locations are not equal.
    /// </summary>
    /// <param name="a">The first location.</param>
    /// <param name="b">The second location.</param>
    /// <returns><c>true</c> if the locations are not equal, <c>false</c> otherwise.</returns>
    public static bool operator !=(BitLocation a, BitLocation b) => !(a == b);

    /// <summary>
    /// Determines whether one location is lower than the other.
    /// </summary>
    /// <param name="a">The first location.</param>
    /// <param name="b">The second location.</param>
    /// <returns><c>true</c> if the first location is lower, <c>false</c> otherwise.</returns>
    public static bool operator <(BitLocation a, BitLocation b) => a.CompareTo(b) < 0;

    /// <summary>
    /// Determines whether one location is lower than or equal to the other.
    /// </summary>
    /// <param name="a">The first location.</param>
    /// <param name="b">The second location.</param>
    /// <returns><c>true</c> if the first location is lower or equal, <c>false</c> otherwise.</returns>
    public static bool operator <=(BitLocation a, BitLocation b) => a.CompareTo(b) <= 0;

    /// <summary>
    /// Determines whether one location is higher than the other.
    /// </summary>
    /// <param name="a">The first location.</param>
    /// <param name="b">The second location.</param>
    /// <returns><c>true</c> if the first location is higher, <c>false</c> otherwise.</returns>
    public static bool operator >(BitLocation a, BitLocation b) => a.CompareTo(b) > 0;

    /// <summary>
    /// Determines whether one location is higher than or equal to the other.
    /// </summary>
    /// <param name="a">The first location.</param>
    /// <param name="b">The second location.</param>
    /// <returns><c>true</c> if the first location is higher or equal, <c>false</c> otherwise.</returns>
    public static bool operator >=(BitLocation a, BitLocation b) => a.CompareTo(b) >= 0;
}