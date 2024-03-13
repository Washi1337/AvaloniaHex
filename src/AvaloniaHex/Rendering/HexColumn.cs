using Avalonia;
using Avalonia.Input;
using Avalonia.Media.TextFormatting;
using AvaloniaHex.Document;
using AvaloniaHex.Editing;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents a column that renders binary data using hexadecimal number encoding.
/// </summary>
public class HexColumn : CellBasedColumn
{
    static HexColumn()
    {
        IsUppercaseProperty.Changed.AddClassHandler<HexColumn, bool>(OnIsUpperCaseChanged);
        CursorProperty.OverrideDefaultValue<HexColumn>(IBeamCursor);
    }

    /// <inheritdoc />
    public override Size MinimumSize => default;

    /// <inheritdoc />
    public override double GroupPadding => CellSize.Width;

    /// <inheritdoc />
    public override int BitsPerCell => 4;

    /// <inheritdoc />
    public override int CellsPerWord => 2;

    /// <summary>
    /// Defines the <see cref="IsUppercase"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsUppercaseProperty =
        AvaloniaProperty.Register<HexColumn, bool>(nameof(IsUppercase), true);

    /// <summary>
    /// Gets or sets a value indicating whether the hexadecimal digits should be rendered in uppercase or not.
    /// </summary>
    public bool IsUppercase
    {
        get => GetValue(IsUppercaseProperty);
        set => SetValue(IsUppercaseProperty, value);
    }

    private static void OnIsUpperCaseChanged(HexColumn arg1, AvaloniaPropertyChangedEventArgs<bool> arg2)
    {
        arg1.HexView?.InvalidateVisualLines();
    }

    /// <inheritdoc />
    protected override string PrepareTextInput(string input) => input.Replace(" ", "");

    private static byte? ParseNibble(char c) => c switch
    {
        >= '0' and <= '9' => (byte?) (c - '0'),
        >= 'a' and <= 'f' => (byte?) (c - 'a' + 10),
        >= 'A' and <= 'F' => (byte?) (c - 'A' + 10),
        _ => null
    };

    /// <inheritdoc />
    protected override bool TryWriteCell(Span<byte> buffer, BitLocation bufferStart, BitLocation writeLocation, char input)
    {
        if (ParseNibble(input) is not { } nibble)
            return false;

        int relativeIndex = (int) (writeLocation.ByteIndex - bufferStart.ByteIndex);
        buffer[relativeIndex] = writeLocation.BitIndex == 4
            ? (byte) ((buffer[relativeIndex] & 0xF) | (nibble << 4))
            : (byte) ((buffer[relativeIndex] & 0xF0) | nibble);
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
        GetText(data, output, IsUppercase);

        return new string(output);
    }

    /// <inheritdoc />
    public override TextLine? CreateTextLine(VisualBytesLine line)
    {
        if (HexView is null)
            return null;

        var properties = GetTextRunProperties();
        return TextFormatter.Current.FormatLine(
            new HexTextSource(line, properties, IsUppercase),
            0,
            double.MaxValue,
            new GenericTextParagraphProperties(properties)
        );
    }

    private static char GetHexDigit(byte nibble, bool uppercase) => nibble switch
    {
        < 10 => (char) (nibble + '0'),
        < 16 => (char) (nibble - 10 + (uppercase ? 'A' : 'a')),
        _ => throw new ArgumentOutOfRangeException(nameof(nibble))
    };

    private static void AppendByte(Span<char> buffer, int index, byte value, bool uppercase)
    {
        buffer[index] = GetHexDigit((byte) ((value >> 4) & 0xF), uppercase);
        buffer[index + 1] = GetHexDigit((byte) (value & 0xF), uppercase);
    }

    private static void GetText(ReadOnlySpan<byte> data, Span<char> buffer, bool uppercase)
    {
        int index = 0;
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0)
                buffer[index++] = ' ';
            AppendByte(buffer, index, data[i], uppercase);
            index += 2;
        }
    }

    private sealed class HexTextSource : ITextSource
    {
        private readonly GenericTextRunProperties _properties;
        private readonly bool _isUppercase;
        private readonly VisualBytesLine _line;

        public HexTextSource(VisualBytesLine line, GenericTextRunProperties properties, bool isUppercase)
        {
            _line = line;
            _properties = properties;
            _isUppercase = isUppercase;
        }

        /// <inheritdoc />
        public TextRun? GetTextRun(int textSourceIndex)
        {
            // Calculate current byte location from text index.
            int byteIndex = Math.DivRem(textSourceIndex, 3, out int nibbleIndex);
            if (byteIndex < 0 || byteIndex >= _line.Data.Length)
                return null;

            // Special case nibble index 2 (space after byte).
            if (nibbleIndex == 2)
            {
                if (byteIndex >= _line.Data.Length - 1)
                    return null;

                return new TextCharacters(" ", _properties);
            }

            // Find current segment we're in.
            var currentLocation = new BitLocation(_line.Range.Start.ByteIndex + (ulong) byteIndex, nibbleIndex * 4);
            var segment = _line.FindSegmentContaining(currentLocation);
            if (segment is null)
                return null;

            // Stringify the segment.
            var range = segment.Range;
            ReadOnlySpan<byte> data = _line.AsAbsoluteSpan(range);
            Span<char> buffer = stackalloc char[(int) segment.Range.ByteLength * 3 - 1];
            GetText(data, buffer, _isUppercase);

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