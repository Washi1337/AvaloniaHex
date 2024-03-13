using System.Text;
using Avalonia;
using Avalonia.Media.TextFormatting;
using AvaloniaHex.Document;
using AvaloniaHex.Editing;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents a column that renders binary data using the ASCII text encoding.
/// </summary>
public class AsciiColumn : CellBasedColumn
{
    static AsciiColumn()
    {
        CursorProperty.OverrideDefaultValue<AsciiColumn>(IBeamCursor);
    }

    /// <inheritdoc />
    public override Size MinimumSize => default;

    /// <inheritdoc />
    public override int BitsPerCell => 8;

    /// <inheritdoc />
    public override int CellsPerWord => 1;

    /// <inheritdoc />
    public override double GroupPadding => 0;

    /// <inheritdoc />
    protected override bool TryWriteCell(Span<byte> buffer, BitLocation bufferStart, BitLocation writeLocation, char input)
    {
        buffer[(int) (writeLocation.ByteIndex - bufferStart.ByteIndex)] = (byte) input;
        return true;
    }

    /// <inheritdoc />
    public override string? GetText(BitRange range)
    {
        if (HexView?.Document is null)
            return null;

        byte[] data = new byte[range.ByteLength];
        HexView.Document.ReadBytes(range.Start.ByteIndex, data);

        char[] output = new char[data.Length];
        GetText(data, output);

        return new string(output);
    }

    /// <inheritdoc />
    public override TextLine? CreateTextLine(VisualBytesLine line)
    {
        if (HexView is null)
            return null;

        var properties = GetTextRunProperties();
        return TextFormatter.Current.FormatLine(
            new AsciiTextSource(line, properties),
            0,
            double.MaxValue,
            new GenericTextParagraphProperties(properties)
        );
    }

    private static void GetText(ReadOnlySpan<byte> data, Span<char> buffer)
    {
        for (int i = 0; i < data.Length; i++)
        {
            buffer[i] = data[ i] switch
            {
                >= 0x20 and < 0x7f => (char) data[ i],
                _ => '.'
            };
        }
    }

    private sealed class AsciiTextSource : ITextSource
    {
        private readonly GenericTextRunProperties _properties;
        private readonly VisualBytesLine _line;

        public AsciiTextSource(VisualBytesLine line, GenericTextRunProperties properties)
        {
            _line = line;
            _properties = properties;
        }

        /// <inheritdoc />
        public TextRun? GetTextRun(int textSourceIndex)
        {
            // Find current segment we're in.
            var currentLocation = new BitLocation(_line.Range.Start.ByteIndex + (ulong) textSourceIndex);
            var segment = _line.FindSegmentContaining(currentLocation);
            if (segment is null)
                return null;

            // Stringify the segment.
            var range = segment.Range;
            ReadOnlySpan<byte> data = _line.AsAbsoluteSpan(range);
            Span<char> buffer = stackalloc char[(int) range.ByteLength];
            GetText(data, buffer);

            // Render
            return new TextCharacters(
                new string(buffer),
                _properties.WithBrushes(
                    segment.ForegroundBrush ?? _properties.ForegroundBrush,
                    segment.BackgroundBrush ?? _properties.BackgroundBrush
                )
            );
        }
    }

}