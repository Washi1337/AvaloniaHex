using AvaloniaHex.Document;

namespace AvaloniaHex.Tests.Document;

public class BitLocationTest
{
    [Fact]
    public void InvalidBitIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BitLocation(0, 8));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BitLocation(0, -3));
    }
    
    [Theory]
    [InlineData(0, 0, 3, 0, 3)]
    [InlineData(0, 0, 8+8+8+3, 3, 3)]
    [InlineData(0, 2, 3, 0, 5)]
    [InlineData(0, 7, 1, 1, 0)]
    [InlineData(0, 7, 3, 1, 2)]
    [InlineData(0, 7, 8+8+8+3, 4, 2)]
    public void AddBits(ulong startByteIndex, int startBitIndex, ulong bitCount, ulong endByteIndex, int endBitIndex)
    {
        var location = new BitLocation(startByteIndex, startBitIndex);
        var newLocation = location.AddBits(bitCount);
        Assert.Equal(new BitLocation(endByteIndex, endBitIndex), newLocation);
    }
}