using AvaloniaHex.Document;

namespace AvaloniaHex.Tests.Document;

public class BitRangeTest
{
    [Fact]
    public void AllowEmptyRange()
    {
        Assert.True(new BitRange(0, 0).IsEmpty);
        Assert.True(new BitRange(10, 10).IsEmpty);
        Assert.True(new BitRange(new BitLocation(0, 5), new BitLocation(0, 5)).IsEmpty);
    }

    [Fact]
    public void DoNotAllowEndBeforeStart()
    {
        Assert.Throws<ArgumentException>(() => new BitRange(10, 9));
        Assert.Throws<ArgumentException>(() => new BitRange(new BitLocation(10, 5), new BitLocation(10, 4)));
    }

    [Theory]
    [InlineData(0, 0, 10, 0, 10 * 8)]
    [InlineData(10, 0, 10, 0, 0)]
    [InlineData(10, 5, 11, 0, 3)]
    [InlineData(10, 5, 12, 0, 8+3)]
    [InlineData(10, 0, 10, 5, 5)]
    [InlineData(10, 0, 11, 5, 8+5)]
    [InlineData(10, 3, 20, 3, 5+9*8+3)]
    public void BitLength(
        ulong startByte, int startBit,
        ulong endByte, int endBit,
        ulong length)
    {
        var range = new BitRange(
            new BitLocation(startByte, startBit),
            new BitLocation(endByte, endBit)
        );

        Assert.Equal(length, range.BitLength);
    }

    [Theory]
    [InlineData(0,0, 10, 0, 5, 0, true)] // middle
    [InlineData(10,5, 100, 0, 10, 5, true)] // start is inclusive
    [InlineData(10,5, 100, 0, 10, 4, false)] // before start
    [InlineData(10,5, 100, 5, 100, 5, false)] // end is exclusive
    [InlineData(10,5, 100, 5, 100, 4, true)] // at end.
    public void ContainsLocation(
        ulong startByte, int startBit,
        ulong endByte, int endBit,
        ulong needleByte, int needleBit,
        bool expected)
    {
        var range = new BitRange(
            new BitLocation(startByte, startBit),
            new BitLocation(endByte, endBit)
        );

        Assert.Equal(expected, range.Contains(new BitLocation(needleByte, needleBit)));
    }

    [Theory]
    [InlineData(
        0, 0, 100, 0,
        50, 0, 60, 0,
        true
    )]
    [InlineData(
        0, 0, 100, 0,
        50, 0, 150, 0,
        false
    )]
    [InlineData(
        50, 0, 100, 0,
        0, 0, 75, 0,
        false
    )]
    public void ContainsRange(
        ulong startByte1, int startBit1, ulong endByte1, int endBit1,
        ulong startByte2, int startBit2, ulong endByte2, int endBit2,
        bool expected)
    {
        var range1 = new BitRange(
            new BitLocation(startByte1, startBit1),
            new BitLocation(endByte1, endBit1)
        );

        var range2 = new BitRange(
            new BitLocation(startByte2, startBit2),
            new BitLocation(endByte2, endBit2)
        );

        Assert.Equal(expected, range1.Contains(range2));
    }

    [Theory]
    [InlineData(
        0, 0, 10, 0,
        0, 0, 10,0,
        true
    )]
    [InlineData(
        0, 0, 10, 0,
        5, 0, 15,0,
        true
    )]
    [InlineData(
        0, 0, 10, 0,
        9, 0, 15,0,
        true
    )]
    [InlineData(
        0, 0, 10, 0,
        10, 0, 15,0,
        false
    )]
    [InlineData(
        5, 0, 15,0,
        0, 0, 10, 0,
        true
    )]
    [InlineData(
        9, 0, 15,0,
        0, 0, 10, 0,
        true
    )]
    [InlineData(
        10, 0, 15,0,
        0, 0, 10, 0,
        false
    )]
    [InlineData(
        5, 0, 15,0,
        7, 0, 10, 0,
        true
    )]
    public void OverlapsWith(
        ulong startByte1, int startBit1, ulong endByte1, int endBit1,
        ulong startByte2, int startBit2, ulong endByte2, int endBit2,
        bool expected)
    {
        var range1 = new BitRange(
            new BitLocation(startByte1, startBit1),
            new BitLocation(endByte1, endBit1)
        );

        var range2 = new BitRange(
            new BitLocation(startByte2, startBit2),
            new BitLocation(endByte2, endBit2)
        );

        Assert.Equal(expected, range1.OverlapsWith(range2));
        Assert.Equal(expected, range2.OverlapsWith(range1));
    }

    [Theory]
    [InlineData(
        10, 0, 100, 0,
        40, 0, 50, 0,
        40, 0, 50, 0
    )]
    [InlineData(
        10, 0, 100, 0,
        50, 0, 150, 0,
        50, 0, 100, 0
    )]
    [InlineData(
        10, 0, 100, 0,
        0, 0, 50, 0,
        10, 0, 50, 0
    )]
    public void ClampOverlapping(
        ulong startByte1, int startBit1, ulong endByte1, int endBit1,
        ulong startByte2, int startBit2, ulong endByte2, int endBit2,
        ulong startByte3, int startBit3, ulong endByte3, int endBit3)
    {
        var original = new BitRange(new BitLocation(startByte1, startBit1), new BitLocation(endByte1, endBit1));
        var restriction = new BitRange(new BitLocation(startByte2, startBit2), new BitLocation(endByte2, endBit2));
        var expected = new BitRange(new BitLocation(startByte3, startBit3), new BitLocation(endByte3, endBit3));
        Assert.Equal(expected, original.Clamp(restriction));
    }

    [Fact]
    public void ClampNonOverlapping()
    {
        var original = new BitRange(10, 100);
        var restriction = new BitRange(200, 210);
        Assert.True(original.Clamp(restriction).IsEmpty);
    }
}