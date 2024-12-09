using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace AvaloniaHex.Document;

/// <summary>
/// Represents a disjoint union of binary ranges.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
public class BitRangeUnion : IReadOnlyBitRangeUnion, ICollection<BitRange>
{
    /// <inheritdoc />
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    private readonly ObservableCollection<BitRange> _ranges = new();

    /// <summary>
    /// Creates a new empty union.
    /// </summary>
    public BitRangeUnion()
    {
        _ranges.CollectionChanged += (sender, args) => CollectionChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Initializes a new union of bit ranges.
    /// </summary>
    /// <param name="ranges">The ranges to unify.</param>
    public BitRangeUnion(IEnumerable<BitRange> ranges)
        : this()
    {
        foreach (var range in ranges)
            Add(range);
    }

    /// <inheritdoc />
    public BitRange EnclosingRange => _ranges.Count == 0 ? BitRange.Empty : new(_ranges[0].Start, _ranges[^1].End);

    /// <inheritdoc />
    public bool IsFragmented => _ranges.Count > 1;

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
    public int Count => _ranges.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    private (SearchResult Result, int Index) FindFirstOverlappingRange(BitRange range)
    {
        range = new BitRange(range.Start, range.End.NextOrMax());
        for (int i = 0; i < _ranges.Count; i++)
        {
            if (_ranges[i].ExtendTo(_ranges[i].End.NextOrMax()).OverlapsWith(range))
            {
                if (_ranges[i].Start >= range.Start)
                    return (SearchResult.PresentAfterIndex, i);
                return (SearchResult.PresentBeforeIndex, i);
            }

            if (_ranges[i].Start > range.End)
            {
                return (SearchResult.NotPresentAtIndex, i);
            }
        }

        return (SearchResult.NotPresentAtIndex, _ranges.Count);
    }

    private void MergeRanges(int startIndex)
    {
        for (int i = startIndex; i < _ranges.Count - 1; i++)
        {
            if (!_ranges[i].ExtendTo(_ranges[i].End.Next()).OverlapsWith(_ranges[i + 1]))
                return;

            _ranges[i] = _ranges[i]
                .ExtendTo(_ranges[i + 1].Start)
                .ExtendTo(_ranges[i + 1].End);

            _ranges.RemoveAt(i + 1);
            i--;
        }
    }

    /// <inheritdoc />
    public void Add(BitRange item)
    {
        (var result, int index) = FindFirstOverlappingRange(item);

        switch (result)
        {
            case SearchResult.PresentBeforeIndex:
                _ranges.Insert(index + 1, item);
                break;

            case SearchResult.PresentAfterIndex:
            case SearchResult.NotPresentAtIndex:
                _ranges.Insert(index, item);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        MergeRanges(index);
    }

    /// <inheritdoc />
    public void Clear() => _ranges.Clear();

    /// <inheritdoc />
    public bool Contains(BitRange item) => _ranges.Contains(item);

    /// <inheritdoc />
    public bool Contains(BitLocation location) => IsSuperSetOf(new BitRange(location, location.NextOrMax()));

    /// <inheritdoc />
    public bool IsSuperSetOf(BitRange range)
    {
        (var result, int index) = FindFirstOverlappingRange(range);
        if (result == SearchResult.NotPresentAtIndex)
            return false;

        return _ranges[index].Contains(range);
    }

    /// <inheritdoc />
    public bool IntersectsWith(BitRange range)
    {
        (var result, int index) = FindFirstOverlappingRange(range);
        if (result == SearchResult.NotPresentAtIndex)
            return false;

        return _ranges[index].OverlapsWith(range);
    }

    /// <inheritdoc />
    public void CopyTo(BitRange[] array, int arrayIndex) => _ranges.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public bool Remove(BitRange item)
    {
        (var result, int index) = FindFirstOverlappingRange(item);

        if (result == SearchResult.NotPresentAtIndex)
            return false;

        for (int i = index; i < _ranges.Count; i++)
        {
            // Is this an overlapping range?
            if (!_ranges[i].OverlapsWith(item))
                break;

            if (_ranges[i].Contains(new BitRange(item.Start, item.End.NextOrMax())))
            {
                // The range contains the entire range-to-remove, split up the range.
                var (a, rest) = _ranges[i].Split(item.Start);
                var (b, c) = rest.Split(item.End);

                if (a.IsEmpty)
                    _ranges.RemoveAt(i--);
                else
                    _ranges[i] = a;

                if (!c.IsEmpty)
                    _ranges.Insert(i + 1, c);
                break;
            }

            if (item.Contains(_ranges[i]))
            {
                // The range-to-remove contains the entire current range.
                _ranges.RemoveAt(i--);
            }
            else if (item.Start < _ranges[i].Start)
            {
                // We are truncating the current range from the left.
                _ranges[i] = _ranges[i].Clamp(new BitRange(item.End, BitLocation.Maximum));
            }
            else if (item.End >= _ranges[i].End)
            {
                // We are truncating the current range from the right.
                _ranges[i] = _ranges[i].Clamp(new BitRange(BitLocation.Minimum, item.Start));
            }
        }

        return true;
    }

    /// <inheritdoc />
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<BitRange> IEnumerable<BitRange>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Wraps the union into a <see cref="ReadOnlyBitRangeUnion"/>.
    /// </summary>
    /// <returns>The resulting read-only union.</returns>
    public ReadOnlyBitRangeUnion AsReadOnly() => new(this);

    private enum SearchResult
    {
        PresentBeforeIndex,
        PresentAfterIndex,
        NotPresentAtIndex,
    }

    /// <summary>
    /// An implementation of an enumerator that enumerates all disjoint ranges within a bit range union.
    /// </summary>
    public struct Enumerator : IEnumerator<BitRange>
    {
        private readonly BitRangeUnion _union;
        private int _index;

        /// <summary>
        /// Creates a new disjoint bit range union enumerator.
        /// </summary>
        /// <param name="union">The disjoint union to enumerate.</param>
        public Enumerator(BitRangeUnion union) : this()
        {
            _union = union;
            _index = -1;
        }

        /// <inheritdoc />
        public BitRange Current => _index < _union._ranges.Count
            ? _union._ranges[_index]
            : default;

        /// <inheritdoc />
        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            _index++;
            return _index < _union._ranges.Count;
        }

        /// <inheritdoc />
        void IEnumerator.Reset()
        {
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}