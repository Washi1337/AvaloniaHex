namespace AvaloniaHex.Document;

/// <summary>
/// Describes a change of documents in a hex view or editor.
/// </summary>
public class DocumentChangedEventArgs : EventArgs
{
    /// <summary>
    /// Constructs a new document change event.
    /// </summary>
    /// <param name="old">The old document.</param>
    /// <param name="new">The new document.</param>
    public DocumentChangedEventArgs(IBinaryDocument? old, IBinaryDocument? @new)
    {
        Old = old;
        New = @new;
    }

    /// <summary>
    /// Gets the original document.
    /// </summary>
    public IBinaryDocument? Old { get; }

    /// <summary>
    /// Gets the new document.
    /// </summary>
    public IBinaryDocument? New { get; }
}