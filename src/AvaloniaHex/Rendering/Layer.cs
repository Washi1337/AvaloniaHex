using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaHex.Editing;

namespace AvaloniaHex.Rendering;

/// <summary>
/// Represents a single layer in the hex view rendering.
/// </summary>
public abstract class Layer : Control
{
    /// <summary>
    /// Gets a value indicating when the layer should be rendered.
    /// </summary>
    public virtual LayerRenderMoments UpdateMoments => LayerRenderMoments.Always;

    /// <summary>
    /// Gets the parent hex view the layer is added to.
    /// </summary>
    public HexView? HexView
    {
        get;
        internal set;
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        base.Render(context);
    }
}