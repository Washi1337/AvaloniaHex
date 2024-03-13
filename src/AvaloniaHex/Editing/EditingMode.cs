namespace AvaloniaHex.Editing;

/// <summary>
/// Provides members describing all possible editing modes a caret in a hex editor can be.
/// </summary>
public enum EditingMode
{
    /// <summary>
    /// Indicates the cursor is overwriting the existing bytes.
    /// </summary>
    Overwrite,

    /// <summary>
    /// Indicates the cursor is inserting new bytes.
    /// </summary>
    Insert
}