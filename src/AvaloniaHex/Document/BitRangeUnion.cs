using System.Collections;
using System.Diagnostics;

namespace AvaloniaHex.Document;

/// <summary>
/// Represents a disjoint union of binary ranges.
/// </summary>
[DebuggerDisplay("Count = {Count}")]
public class BitRangeUnion : ICollection<BitRange>
{
    private readonly List<BitRange> _ranges = new();

    /// <inheritdoc />
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

    /// <summary>
    /// Determines whether the provided location is within the included ranges..
    /// </summary>
    /// <param name="location">The location.</param>
    /// <returns><c>true</c> if the location is included, <c>false</c> otherwise.</returns>
    public bool Contains(BitLocation location) => Contains(new BitRange(location, location.NextOrMax()));

    /// <inheritdoc />
    public bool Contains(BitRange item)
    {
        (var result, int index) = FindFirstOverlappingRange(item);
        if (result == SearchResult.NotPresentAtIndex)
            return false;

        return _ranges[index].OverlapsWith(item);
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

            if (_ranges[i].Contains(item))
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
            else if (item.End > _ranges[i].End)
            {
                // We are truncating the current range from the right.
                _ranges[i] = _ranges[i].Clamp(new BitRange(BitLocation.Minimum, item.Start));
            }
        }

        return true;
    }

    /// <summary>
    /// Gets an enumerator that enumerates all the ranges in the union.
    /// </summary>
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<BitRange> IEnumerable<BitRange>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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