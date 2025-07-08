using System.Collections;
using System.Collections.Specialized;

namespace AvaloniaHex.Document;

/// <summary>
/// Represents a read-only disjoint union of binary ranges in a document.
/// </summary>
public class ReadOnlyBitRangeUnion : IReadOnlyBitRangeUnion
{
    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// The empty union.
    /// </summary>
    public static readonly ReadOnlyBitRangeUnion Empty = new(new BitRangeUnion());

    private readonly BitRangeUnion _union;

    /// <summary>
    /// Wraps an existing disjoint binary range union into a <see cref="ReadOnlyBitRangeUnion"/>.
    /// </summary>
    /// <param name="union">The union to wrap.</param>
    public ReadOnlyBitRangeUnion(BitRangeUnion union)
    {
        _union = union;
        _union.CollectionChanged += UnionOnCollectionChanged;
    }

    private void UnionOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CollectionChanged?.Invoke(this, e);
    }

    /// <inheritdoc />
    public int Count => _union.Count;

    /// <inheritdoc />
    public BitRange EnclosingRange => _union.EnclosingRange;

    /// <inheritdoc />
    public bool IsFragmented => _union.IsFragmented;

    /// <inheritdoc />
    public bool Contains(BitLocation location) => _union.Contains(location);

    /// <inheritdoc />
    public bool IsSuperSetOf(BitRange range) => _union.IsSuperSetOf(range);

    /// <inheritdoc />
    public bool IntersectsWith(BitRange range) => _union.IntersectsWith(range);

    /// <inheritdoc />
    public int GetOverlappingRanges(BitRange range, Span<BitRange> output) => _union.GetOverlappingRanges(range, output);

    /// <inheritdoc />
    public int GetIntersectingRanges(BitRange range, Span<BitRange> output) => _union.GetIntersectingRanges(range, output);

    /// <inheritdoc />
    public BitRangeUnion.Enumerator GetEnumerator() => _union.GetEnumerator();

    IEnumerator<BitRange> IEnumerable<BitRange>.GetEnumerator() => _union.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) _union).GetEnumerator();
}