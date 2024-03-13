using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace AvaloniaHex.Rendering;

internal static class TextRunExtensions
{
    public static GenericTextRunProperties WithForeground(this GenericTextRunProperties self, IBrush? foreground)
    {
        if (Equals(self.ForegroundBrush, foreground))
            return self;
        
        return new GenericTextRunProperties(
            self.Typeface,
            self.FontRenderingEmSize,
            self.TextDecorations,
            foreground,
            self.BackgroundBrush,
            self.BaselineAlignment,
            self.CultureInfo
        );
    }
    
    public static GenericTextRunProperties WithBackground(this GenericTextRunProperties self, IBrush? background)
    {
        if (Equals(self.BackgroundBrush, background))
            return self;
        
        return new GenericTextRunProperties(
            self.Typeface,
            self.FontRenderingEmSize,
            self.TextDecorations,
            self.ForegroundBrush,
            background,
            self.BaselineAlignment,
            self.CultureInfo
        );
    }
    
    public static GenericTextRunProperties WithBrushes(this GenericTextRunProperties self, IBrush? foreground, IBrush? background)
    {
        if (Equals(self.ForegroundBrush, foreground) && Equals(self.BackgroundBrush, background))
            return self;
        
        return new GenericTextRunProperties(
            self.Typeface,
            self.FontRenderingEmSize,
            self.TextDecorations,
            foreground,
            background,
            self.BaselineAlignment,
            self.CultureInfo
        );
    }
}