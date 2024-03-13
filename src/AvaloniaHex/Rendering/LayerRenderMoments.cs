namespace AvaloniaHex.Rendering;

/// <summary>
/// Provides members describing when a layer should be rendered.
/// </summary>
[Flags]
public enum LayerRenderMoments
{
    /// <summary>
    /// Indicates the layer should only be rendered minimally.
    /// </summary>
    Minimal = 0,

    /// <summary>
    /// Indicates the layer should be rendered when a rearrange of the text is queued.
    /// </summary>
    NoResizeRearrange = 1,

    /// <summary>
    /// Indicates the layer should be rendered when a single line was invalidated.
    /// </summary>
    LineInvalidate = 2,

    /// <summary>
    /// Indicates the layer should always be rendered on every update.
    /// </summary>
    Always = NoResizeRearrange | LineInvalidate,
}