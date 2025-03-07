using System.Collections;
using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

internal sealed class VisualBytesLinesBuffer : IReadOnlyList<VisualBytesLine>
{
    private readonly HexView _owner;
    private readonly Stack<VisualBytesLine> _pool = new();
    private readonly List<VisualBytesLine> _activeLines = new();

    public VisualBytesLinesBuffer(HexView owner)
    {
        _owner = owner;
    }

    public int Count => _activeLines.Count;

    public VisualBytesLine this[int index] => _activeLines[index];

    public VisualBytesLine? GetVisualLineByLocation(BitLocation location)
    {
        for (int i = 0; i < _activeLines.Count; i++)
        {
            var line = _activeLines[i];
            if (line.VirtualRange.Contains(location))
                return line;

            if (line.Range.Start > location)
                return null;
        }

        return null;
    }

    public IEnumerable<VisualBytesLine> GetVisualLinesByRange(BitRange range)
    {
        for (int i = 0; i < _activeLines.Count; i++)
        {
            var line = _activeLines[i];
            if (line.VirtualRange.OverlapsWith(range))
                yield return line;

            if (line.Range.Start >= range.End)
                yield break;
        }
    }

    public VisualBytesLine GetOrCreateVisualLine(BitRange virtualRange)
    {
        VisualBytesLine? newLine = null;

        // Find existing line or create a new one, while keeping the list of visual lines ordered by range.
        for (int i = 0; i < _activeLines.Count; i++)
        {
            // Exact match on start?
            var currentLine = _activeLines[i];
            if (currentLine.VirtualRange.Start == virtualRange.Start)
            {
                // Edge-case: if our range is not exactly the same, the line's range is outdated (e.g., as a result of
                // inserting or removing a character at the end of the document).
                if (currentLine.SetRange(virtualRange))
                    currentLine.Invalidate();

                return currentLine;
            }

            // If the next line is further than the requested start, the line does not exist.
            if (currentLine.Range.Start > virtualRange.Start)
            {
                newLine = Rent(virtualRange);
                _activeLines.Insert(i, newLine);
                break;
            }
        }

        // We didn't find any line for the location, add it to the end.
        if (newLine is null)
        {
            newLine = Rent(virtualRange);
            _activeLines.Add(newLine);
        }

        return newLine;
    }

    public void RemoveOutsideOfRange(BitRange range)
    {
        for (int i = 0; i < _activeLines.Count; i++)
        {
            var line = _activeLines[i];
            if (!range.Contains(line.VirtualRange.Start))
            {
                Return(line);
                _activeLines.RemoveAt(i--);
            }
        }
    }

    public void Clear()
    {
        foreach (var instance in _activeLines)
            Return(instance);
        _activeLines.Clear();
    }

    private VisualBytesLine Rent(BitRange virtualRange)
    {
        var line = GetPooledLine();
        line.SetRange(virtualRange);
        line.Invalidate();
        return line;
    }

    private VisualBytesLine GetPooledLine()
    {
        while (_pool.TryPop(out var line))
        {
            if (line.Data.Length == _owner.ActualBytesPerLine)
                return line;
        }

        return new VisualBytesLine(_owner);
    }

    private void Return(VisualBytesLine line)
    {
        _pool.Push(line);
    }

    public IEnumerator<VisualBytesLine> GetEnumerator() => _activeLines.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}