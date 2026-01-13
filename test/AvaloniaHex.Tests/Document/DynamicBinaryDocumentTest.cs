using AvaloniaHex.Document;

namespace AvaloniaHex.Tests.Document;

public class DynamicBinaryDocumentTest
{
    [Fact]
    public void ReadUnmodified()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        Assert.Equal([0, 1, 2, 3], document.ReadBytes(0, 4));
        Assert.Equal([3, 4, 5, 6, 7], document.ReadBytes(3, 5));
    }

    [Fact]
    public void ReadPastEndShouldThrow()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        Assert.Throws<EndOfStreamException>(() => document.ReadBytes(document.Length, 1));
    }

    [Fact]
    public void InsertBytesBeginShouldReadBackInsertedThenOriginal()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        document.InsertBytes(0, [11, 22, 33]);
        Assert.Equal([11, 22, 33, 0, 1, 2, 3], document.ReadBytes(0, 7));
    }

    [Fact]
    public void InsertBytesMiddleShouldReadBackOriginalThenInsertedThenOriginal()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        document.InsertBytes(2, [11, 22, 33]);
        Assert.Equal([0, 1, 11, 22, 33, 2, 3], document.ReadBytes(0, 7));
    }

    [Fact]
    public void InsertBytesEndShouldReadBackOriginalThenInserted()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        document.InsertBytes(document.Length, [11, 22, 33]);
        Assert.Equal([4, 5, 6, 7, 11, 22, 33], document.ReadBytes(4, 7));
    }

    [Fact]
    public void InsertBytesEmptyShouldReadBackInserted()
    {
        var document = new DynamicBinaryDocument();

        document.InsertBytes(document.Length, [11, 22, 33]);
        Assert.Equal([11, 22, 33], document.ReadBytes(0, 3));
    }

    [Fact]
    public void InsertBytesMultipleInDifferentRange()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        document.InsertBytes(2, [11, 22, 33, 44, 55]);
        Assert.Equal([0, 1, 11, 22, 33, 44, 55, 2, 3, 4, 5, 6, 7], document.ReadBytes(0, (int) document.Length));
        document.InsertBytes(9, [111, 222]);
        Assert.Equal([0, 1, 11, 22, 33, 44, 55, 2, 3, 111, 222, 4, 5, 6, 7], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void InsertBytesMultipleInTouchingRange()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        document.InsertBytes(2, [11, 22, 33, 44, 55]);
        Assert.Equal([0, 1, 11, 22, 33, 44, 55, 2, 3, 4, 5, 6, 7], document.ReadBytes(0, (int) document.Length));
        document.InsertBytes(7, [111, 222]);
        Assert.Equal([0, 1, 11, 22, 33, 44, 55, 111, 222, 2, 3, 4, 5, 6, 7], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void InsertBytesMultipleInSameRange()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        document.InsertBytes(2, [11, 22, 33, 44, 55]);
        Assert.Equal([0, 1, 11, 22, 33, 44, 55, 2, 3, 4, 5, 6, 7], document.ReadBytes(0, (int) document.Length));
        document.InsertBytes(4, [111, 222]);
        Assert.Equal([0, 1, 11, 22, 111, 222, 33, 44, 55, 2, 3, 4, 5, 6, 7], document.ReadBytes(0, (int) document.Length));
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] {1, 2, 3})]
    public void InsertBytesPastEndShouldThrow(byte[] initialData)
    {
        var document = new DynamicBinaryDocument(initialData);

        Assert.Throws<ArgumentOutOfRangeException>(() => document.InsertBytes(document.Length + 1, [11, 22, 33]));
    }

    [Fact]
    public void RemoveBytesBeginShouldReadBackTail()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        document.RemoveBytes(0, 3);
        Assert.Equal([3, 4, 5, 6, 7], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void RemoveBytesEndShouldReadBackHead()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        document.RemoveBytes(5, 3);
        Assert.Equal([0, 1, 2, 3, 4], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void RemoveBytesMiddleShouldReadBackHeadAndTail()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7]);

        document.RemoveBytes(2, 3);
        Assert.Equal([0, 1, 5, 6, 7], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void RemoveBytesMultiple()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        document.RemoveBytes(2, 3);
        Assert.Equal([0, 1, 5, 6, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
        document.RemoveBytes(5, 2);
        Assert.Equal([0, 1, 5, 6, 7, 10], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void InsertAndRemoveBytesDifferentRanges()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        document.InsertBytes(5, [11, 22, 33]);
        Assert.Equal([0, 1, 2, 3, 4, 11, 22, 33, 5, 6, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
        document.RemoveBytes(2, 3);
        Assert.Equal([0, 1, 11, 22, 33, 5, 6, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void RemoveAndInsertBytesDifferentRanges()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        document.RemoveBytes(2, 3);
        Assert.Equal([0, 1, 5, 6, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
        document.InsertBytes(5, [11, 22, 33]);
        Assert.Equal([0, 1, 5, 6, 7, 11, 22, 33, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void InsertAndRemoveBytesOverlappingFromStart()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        document.InsertBytes(5, [11, 22, 33]);
        Assert.Equal([0, 1, 2, 3, 4, 11, 22, 33, 5, 6, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
        document.RemoveBytes(3, 3);
        Assert.Equal([0, 1, 2, 22, 33, 5, 6, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void InsertAndRemoveBytesOverlappingFromEnd()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        document.InsertBytes(5, [11, 22, 33]);
        Assert.Equal([0, 1, 2, 3, 4, 11, 22, 33, 5, 6, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
        document.RemoveBytes(7, 3);
        Assert.Equal([0, 1, 2, 3, 4, 11, 22, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void InsertAndClearDocument()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], document.ReadBytes(0, (int) document.Length));
        document.RemoveBytes(0, document.Length);
        Assert.Equal([], document.ReadBytes(0, (int) document.Length));
        document.InsertBytes(0, [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7, 8, 9], document.ReadBytes(0, (int) document.Length));
        document.RemoveBytes(0, document.Length);
        Assert.Equal([], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void FlushAfterModificationsShouldReadBackSameDocument()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);

        document.InsertBytes(5, [11, 22, 33]);
        Assert.Equal([0, 1, 2, 3, 4, 11, 22, 33, 5, 6, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
        document.RemoveBytes(3, 3);
        Assert.Equal([0, 1, 2, 22, 33, 5, 6, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
        document.Flush();
        Assert.Equal([0, 1, 2, 22, 33, 5, 6, 7, 8, 9, 10], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void WriteBytesBeginShouldReadBackOverwrittenAndTail()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

        document.WriteBytes(0, [11, 22, 33]);
        Assert.Equal([11, 22, 33, 3, 4, 5, 6, 7, 8, 9], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void WriteBytesEndShouldReadBackHeadAndOverwritten()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

        document.WriteBytes(7, [11, 22, 33]);
        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 11, 22, 33], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void WriteBytesMiddleShouldReadBackHeadAndOverwrittenAndTail()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

        document.WriteBytes(3, [11, 22, 33]);
        Assert.Equal([0, 1, 2, 11, 22, 33, 6, 7, 8, 9], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void OverwriteInsertedBytesMiddleShouldReadBackHeadAndOverwrittenAndTail()
    {
        var document = new DynamicBinaryDocument([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]);

        document.InsertBytes(3, [11, 22, 33]);
        Assert.Equal([0, 1, 2, 11, 22, 33, 3, 4, 5, 6, 7, 8, 9], document.ReadBytes(0, (int) document.Length));
        document.WriteBytes(3, [111, 222]);
        Assert.Equal([0, 1, 2, 111, 222, 33, 3, 4, 5, 6, 7, 8, 9], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void TypingSimulationInOrder()
    {
        var document = new DynamicBinaryDocument([]);

        document.InsertBytes(0, [0x10]);
        Assert.Equal([0x10], document.ReadBytes(0, (int) document.Length));
        document.WriteBytes(0, [0x12]);
        Assert.Equal([0x12], document.ReadBytes(0, (int) document.Length));
        document.InsertBytes(1, [0x30]);
        Assert.Equal([0x12, 0x30], document.ReadBytes(0, (int) document.Length));
        document.WriteBytes(1, [0x34]);
        Assert.Equal([0x12, 0x34], document.ReadBytes(0, (int) document.Length));
        document.InsertBytes(2, [0x50]);
        Assert.Equal([0x12, 0x34, 0x50], document.ReadBytes(0, (int) document.Length));
        document.WriteBytes(2, [0x56]);
        Assert.Equal([0x12, 0x34, 0x56], document.ReadBytes(0, (int) document.Length));
    }

    [Fact]
    public void TypingSimulationOutOfOrder()
    {
        var document = new DynamicBinaryDocument([]);

        document.InsertBytes(0, [0x30]);
        Assert.Equal([0x30], document.ReadBytes(0, (int) document.Length));
        document.WriteBytes(0, [0x34]);
        Assert.Equal([0x34], document.ReadBytes(0, (int) document.Length));
        document.InsertBytes(1, [0x50]);
        Assert.Equal([0x34, 0x50], document.ReadBytes(0, (int) document.Length));
        document.WriteBytes(1, [0x56]);
        Assert.Equal([0x34, 0x56], document.ReadBytes(0, (int) document.Length));
        document.InsertBytes(0, [0x10]);
        Assert.Equal([0x10, 0x34, 0x56], document.ReadBytes(0, (int) document.Length));
        document.WriteBytes(0, [0x12]);
        Assert.Equal([0x12, 0x34, 0x56], document.ReadBytes(0, (int) document.Length));
    }
}
