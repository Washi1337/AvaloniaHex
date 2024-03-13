using Avalonia;
using Avalonia.Media.TextFormatting;
using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents a column that renders binary data using the binary number encoding.
/// </summary>
public class BinaryColumn : CellBasedColumn
{
    static BinaryColumn()
    {
        CursorProperty.OverrideDefaultValue<BinaryColumn>(IBeamCursor);
    }

    /// <inheritdoc />
    public override Size MinimumSize => default;

    /// <inheritdoc />
    public override double GroupPadding => CellSize.Width;

    /// <inheritdoc />
    public override int BitsPerCell => 1;

    /// <inheritdoc />
    public override int CellsPerWord => 8;

    private static byte? ParseBit(char c) => c switch
    {
        '0' => 0,
        '1' => 1,
        _ => null
    };

    /// <inheritdoc />
    protected override string PrepareTextInput(string input) => input.Replace(" ", "");

    /// <inheritdoc />
    protected override bool TryWriteCell(Span<byte> buffer, BitLocation bufferStart, BitLocation writeLocation, char input)
    {
        if (ParseBit(input) is not { } bit)
            return false;

        int relativeByteIndex = (int) (writeLocation.ByteIndex - bufferStart.ByteIndex);
        buffer[relativeByteIndex] = (byte)(
            buffer[relativeByteIndex] & ~(1 << writeLocation.BitIndex) | (bit << writeLocation.BitIndex)
        );

        return true;
    }

    /// <inheritdoc />
    public override string? GetText(BitRange range)
    {
        if (HexView?.Document is null)
            return null;

        byte[] data = new byte[range.ByteLength];
        HexView.Document.ReadBytes(range.Start.ByteIndex, data);

        char[] output = new char[data.Length * 3 - 1];
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
            new BinaryTextSource(line, properties),
            0,
            double.MaxValue,
            new GenericTextParagraphProperties(properties)
        );
    }

    private static void AppendByte(Span<char> buffer, int index, byte value)
    {
        for (int i = 0; i < 8; i++)
            buffer[index + i] = (char) (((value >> (7 - i)) & 1) + '0');
    }

    private static void GetText(ReadOnlySpan<byte> data, Span<char> buffer)
    {
        int index = 0;
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0)
                buffer[index++] = ' ';
            AppendByte(buffer, index, data[i]);
            index += 8;
        }
    }

    private sealed class BinaryTextSource : ITextSource
    {
        private readonly GenericTextRunProperties _properties;
        private readonly VisualBytesLine _line;

        public BinaryTextSource(VisualBytesLine line, GenericTextRunProperties properties)
        {
            _line = line;
            _properties = properties;
        }

        /// <inheritdoc />
        public TextRun? GetTextRun(int textSourceIndex)
        {
            // Calculate current byte location from text index.
            int byteIndex = Math.DivRem(textSourceIndex, 9, out int bitIndex);
            if (byteIndex < 0 || byteIndex >= _line.Data.Length)
                return null;

            // Special case nibble index 8 (space after byte).
            if (bitIndex == 8)
            {
                if (byteIndex >= _line.Data.Length - 1)
                    return null;

                return new TextCharacters(" ", _properties);
            }

            // Find current segment we're in.
            var currentLocation = new BitLocation(_line.Range.Start.ByteIndex + (ulong) byteIndex, bitIndex);
            var segment = _line.FindSegmentContaining(currentLocation);
            if (segment is null)
                return null;

            // Stringify the segment.
            var range = segment.Range;
            ReadOnlySpan<byte> data = _line.AsAbsoluteSpan(range);
            Span<char> buffer = stackalloc char[(int) segment.Range.ByteLength * 9 - 1];
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