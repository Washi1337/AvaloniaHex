using Avalonia;
using Avalonia.Media.TextFormatting;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents the column rendering the line offsets.
/// </summary>
public class OffsetColumn : Column
{
    private Size _minimumSize;

    static OffsetColumn()
    {
        IsUppercaseProperty.Changed.AddClassHandler<OffsetColumn, bool>(OnIsUpperCaseChanged);
        IsHeaderVisibleProperty.OverrideDefaultValue<OffsetColumn>(false);
        HeaderProperty.OverrideDefaultValue<OffsetColumn>("Offset");
    }

    /// <inheritdoc />
    public override Size MinimumSize => _minimumSize;

    /// <summary>
    /// Defines the <see cref="IsUppercase"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsUppercaseProperty =
        AvaloniaProperty.Register<OffsetColumn, bool>(nameof(IsUppercase), true);

    /// <summary>
    /// Gets or sets a value indicating whether the hexadecimal digits should be rendered in uppercase or not.
    /// </summary>
    public bool IsUppercase
    {
        get => GetValue(IsUppercaseProperty);
        set => SetValue(IsUppercaseProperty, value);
    }

    private static void OnIsUpperCaseChanged(OffsetColumn arg1, AvaloniaPropertyChangedEventArgs<bool> arg2)
    {
        arg1.HexView?.InvalidateVisualLines();
    }

    /// <inheritdoc />
    public override void Measure()
    {
        if (HexView is null)
        {
            _minimumSize = default;
        }
        else
        {
            var dummy = CreateTextLine("00000000:")!;
            _minimumSize = new Size(dummy.Width, dummy.Height);
        }
    }

    /// <inheritdoc />
    public override TextLine? CreateTextLine(VisualBytesLine line)
    {
        if (HexView is null)
            throw new InvalidOperationException();

        return CreateTextLine(FormatOffset(line.Range.Start.ByteIndex));
    }

    /// <summary>
    /// Formats the provided offset to a string to be displayed in the column.
    /// </summary>
    /// <param name="offset">The offset to format.</param>
    /// <returns>The formatted offset.</returns>
    protected virtual string FormatOffset(ulong offset) => IsUppercase
        ? $"{offset:X8}:"
        : $"{offset:x8}:";

    private TextLine? CreateTextLine(string text)
    {
        if (HexView is null)
            return null;

        var properties = GetTextRunProperties();
        return TextFormatter.Current.FormatLine(
            new SimpleTextSource(text, properties),
            0,
            double.MaxValue,
            new GenericTextParagraphProperties(properties)
        )!;
    }
}