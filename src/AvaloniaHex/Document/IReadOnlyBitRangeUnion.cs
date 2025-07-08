using System.Collections.Specialized;

namespace AvaloniaHex.Document;

/// <summary>
/// Provides read-only access to a disjoint union of binary ranges in a document.
/// </summary>
public interface IReadOnlyBitRangeUnion : IReadOnlyCollection<BitRange>, INotifyCollectionChanged
{
    /// <summary>
    /// Gets the minimum range that encloses all sub ranges included in the union.
    /// </summary>
    BitRange EnclosingRange { get; }

    /// <summary>
    /// Gets a value indicating whether the union consists of multiple disjoint ranges.
    /// </summary>
    bool IsFragmented { get; }

    /// <summary>
    /// Determines whether the provided location is within the included ranges.
    /// </summary>
    /// <param name="location">The location.</param>
    /// <returns><c>true</c> if the location is included, <c>false</c> otherwise.</returns>
    bool Contains(BitLocation location);

    /// <summary>
    /// Determines whether the provided range is within the included ranges.
    /// </summary>
    /// <param name="range">The range to test.</param>
    /// <returns><c>true</c> if the union is a super-set of the provided range, <c>false</c> otherwise.</returns>
    bool IsSuperSetOf(BitRange range);

    /// <summary>
    /// Determines whether the provided range intersects with any of the ranges in the union.
    /// </summary>
    /// <param name="range">The range to test.</param>
    /// <returns><c>true</c> if the union intersects with the provided range, <c>false</c> otherwise.</returns>
    bool IntersectsWith(BitRange range);

    /// <summary>
    /// Collects the disjoint ranges in the union that overlap with the provided range.
    /// </summary>
    /// <param name="range">The range to overlap with.</param>
    /// <param name="output">The output buffer to store the overlapping disjoint ranges in.</param>
    /// <returns>The number of found disjoint ranges.</returns>
    int GetOverlappingRanges(BitRange range, Span<BitRange> output);

    /// <summary>
    /// Collects the intersection of all disjoint ranges that overlap with the provided range.
    /// </summary>
    /// <param name="range">The range to intersect with.</param>
    /// <param name="output">The output buffer to store the intersecting disjoint ranges in.</param>
    /// <returns>The number of found disjoint ranges.</returns>
    int GetIntersectingRanges(BitRange range, Span<BitRange> output);

    /// <summary>
    /// Gets an enumerator that enumerates all the ranges in the union.
    /// </summary>
    new BitRangeUnion.Enumerator GetEnumerator();
}