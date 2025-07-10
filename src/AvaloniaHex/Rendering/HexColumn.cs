using Avalonia;
using Avalonia.Media.TextFormatting;
using AvaloniaHex.Document;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents a column that renders binary data using hexadecimal number encoding.
/// </summary>
public class HexColumn : CellBasedColumn
{
    static HexColumn()
    {
        IsUppercaseProperty.Changed.AddClassHandler<HexColumn, bool>(OnIsUpperCaseChanged);
        UseDynamicHeaderProperty.Changed.AddClassHandler<BinaryColumn, bool>(OnUseDynamicHeaderChanged);
        CursorProperty.OverrideDefaultValue<HexColumn>(IBeamCursor);
        HeaderProperty.OverrideDefaultValue<HexColumn>("Hex");
    }

    /// <summary>
    /// Dependency property for <see cref="UseDynamicHeader"/>
    /// </summary>
    public static readonly StyledProperty<bool> UseDynamicHeaderProperty =
        AvaloniaProperty.Register<HexColumn, bool>(nameof(UseDynamicHeader), true);

    /// <summary>
    /// Gets or sets a value indicating whether the header of this column should be dynamically
    /// </summary>
    public bool UseDynamicHeader
    {
        get => GetValue(IsHeaderVisibleProperty);
        set => SetValue(IsHeaderVisibleProperty, value);
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
        GetText(data, range, output);

        return new string(output);
    }

    /// <inheritdoc />
    public override TextLine? CreateHeaderLine()
    {
        if (!UseDynamicHeader)
            return base.CreateHeaderLine();

        if (HexView is null)
            return null;

        // Generate header text.
        int count = HexView.ActualBytesPerLine;
        char[] buffer = new char[count * 3 - 1];
        for (int i = 0; i < count; i++)
        {
            buffer[i * 3] = GetHexDigit((byte) ((i >> 4) & 0xF), IsUppercase);
            buffer[i * 3 + 1] = GetHexDigit((byte) (i & 0xF), IsUppercase);
            if (i < count - 1)
                buffer[i * 3 + 2] = ' ';
        }

        // Render.
        var properties = GetHeaderTextRunProperties();
        return TextFormatter.Current.FormatLine(
            new SimpleTextSource(new string(buffer), properties),
            0,
            double.MaxValue,
            new GenericTextParagraphProperties(properties)
        );
    }

    /// <inheritdoc />
    public override TextLine? CreateTextLine(VisualBytesLine line)
    {
        if (HexView is null)
            return null;

        var properties = GetTextRunProperties();
        return TextFormatter.Current.FormatLine(
            new HexTextSource(this, line, properties),
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

    private void GetText(ReadOnlySpan<byte> data, BitRange dataRange, Span<char> buffer)
    {
        bool uppercase = IsUppercase;
        char invalidCellChar = InvalidCellChar;

        if (HexView?.Document?.ValidRanges is not { } valid)
        {
            buffer.Fill(invalidCellChar);
            return;
        }

        int index = 0;
        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0)
                buffer[index++] = ' ';

            var location1 = new BitLocation(dataRange.Start.ByteIndex + (ulong) i, 0);
            var location2 = new BitLocation(dataRange.Start.ByteIndex + (ulong) i, 4);
            var location3 = new BitLocation(dataRange.Start.ByteIndex + (ulong) i + 1, 0);

            byte value = data[i];

            buffer[index] = valid.IsSuperSetOf(new BitRange(location2, location3))
                ? GetHexDigit((byte) ((value >> 4) & 0xF), uppercase)
                : invalidCellChar;

            buffer[index + 1] = valid.IsSuperSetOf(new BitRange(location1, location2))
                ? GetHexDigit((byte) (value & 0xF), uppercase)
                : invalidCellChar;

            index += 2;
        }
    }

    private static void OnIsUpperCaseChanged(HexColumn arg1, AvaloniaPropertyChangedEventArgs<bool> arg2)
    {
        if (arg1.HexView is null)
            return;

        arg1.HexView.InvalidateVisualLines();
        arg1.HexView.InvalidateHeaders();
    }

    private static void OnUseDynamicHeaderChanged(BinaryColumn arg1, AvaloniaPropertyChangedEventArgs<bool> arg2)
    {
        arg1.HexView?.InvalidateHeaders();
    }

    private sealed class HexTextSource : ITextSource
    {
        private readonly HexColumn _column;
        private readonly GenericTextRunProperties _properties;
        private readonly VisualBytesLine _line;

        public HexTextSource(HexColumn column, VisualBytesLine line, GenericTextRunProperties properties)
        {
            _column = column;
            _line = line;
            _properties = properties;
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
            _column.GetText(data,  range, buffer);

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