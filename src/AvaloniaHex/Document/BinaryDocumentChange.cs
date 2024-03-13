namespace AvaloniaHex.Document;

/// <summary>
/// Describes a change in a binary document.
/// </summary>
/// <param name="Type">The action that was performed.</param>
/// <param name="AffectedRange">The range within the document that was affected.</param>
public record struct BinaryDocumentChange(BinaryDocumentChangeType Type, BitRange AffectedRange);

/// <summary>
/// Provides members describing the possible actions that can be applied to a document.
/// </summary>
public enum BinaryDocumentChangeType
{
    /// <summary>
    /// Indicates the document was modified in-place.
    /// </summary>
    Modify,

    /// <summary>
    /// Indicates bytes were inserted into the document.
    /// </summary>
    Insert,

    /// <summary>
    /// Indicates the bytes were removed from the document.
    /// </summary>
    Remove,
}