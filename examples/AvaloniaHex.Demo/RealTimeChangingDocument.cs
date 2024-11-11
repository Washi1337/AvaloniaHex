using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using AvaloniaHex.Document;

namespace AvaloniaHex.Demo;

/// <summary>
/// Provides an example implementation of a binary document that demonstrates notifying document changes to a hex view.
/// </summary>
public class RealTimeChangingDocument : MemoryBinaryDocument
{
    private readonly Random _random = new();

    public RealTimeChangingDocument(int size, TimeSpan refreshInterval)
        : base(new byte[size])
    {
        var timer = new DispatcherTimer(refreshInterval, DispatcherPriority.Background, RefreshTimerOnTick);
        timer.Start();
    }

    /// <summary>
    /// Gets the collection of bit ranges that are changing continuously.
    /// </summary>
    public IList<BitRange> DynamicRanges { get; } = new List<BitRange>();

    private void RefreshTimerOnTick(object? sender, EventArgs e)
    {
        int maxLength = (int) DynamicRanges.Max(x => x.ByteLength);
        Span<byte> buffer = stackalloc byte[maxLength];

        for (int i = 0; i < DynamicRanges.Count; i++)
        {
            var range = DynamicRanges[i];

            // Generate some new random memory for this range.
            var span = buffer[..(int) range.ByteLength];
            _random.NextBytes(span);
            span.CopyTo(Memory.Span[(int) range.Start.ByteIndex..]);

            // Notify changes.
            OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Modify, range));
        }
    }
}