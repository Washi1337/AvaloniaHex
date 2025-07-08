using AvaloniaHex.Document;

namespace AvaloniaHex.Tests.Document;

public class BitRangeUnionTest
{
    [Fact]
    public void Empty()
    {
        Assert.Empty(new BitRangeUnion());
    }

    [Fact]
    public void AddSingle()
    {
        var range = new BitRange(10, 20);
        var set = new BitRangeUnion();

        set.Add(range);
        Assert.Equal(range, Assert.Single(set));
    }

    [Fact]
    public void AddDisjoint()
    {
        var range1 = new BitRange(10, 20);
        var range2 = new BitRange(40, 60);
        var set = new BitRangeUnion();

        set.Add(range1);
        Assert.Equal(new[] {range1}, set);

        set.Add(range2);
        Assert.Equal(new[] {range1, range2}, set);
    }

    [Fact]
    public void AddDisjointShouldSort()
    {
        var range1 = new BitRange(10, 20);
        var range2 = new BitRange(40, 60);
        var set = new BitRangeUnion();

        set.Add(range2);
        Assert.Equal(new[] { range2 }, set);

        set.Add(range1);
        Assert.Equal(new[] { range1, range2 }, set);
    }

    [Fact]
    public void AddOverlapping()
    {
        var range1 = new BitRange(10, 20);
        var range2 = new BitRange(15, 60);
        var set = new BitRangeUnion();

        set.Add(range1);
        Assert.Equal(new[] {range1}, set);

        set.Add(range2);
        Assert.Equal(new[] { new BitRange(10, 60) }, set);
    }

    [Fact]
    public void AddOverlappingRight()
    {
        var range1 = new BitRange(10, 20);
        var range2 = new BitRange(15, 60);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.Equal(new[] { range1 }, set);

        set.Add(range2);
        Assert.Equal(new[] { new BitRange(10, 60) }, set);
    }

    [Fact]
    public void AddOverlappingRightEdge()
    {
        var range1 = new BitRange(10, 20);
        var range2 = new BitRange(20, 60);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.Equal(new[] { range1 }, set);

        set.Add(range2);
        Assert.Equal(new[] { new BitRange(10, 60) }, set);
    }

    [Fact]
    public void AddOverlappingLeft()
    {
        var range1 = new BitRange(10, 20);
        var range2 = new BitRange(5, 15);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.Equal(new[] { range1 }, set);

        set.Add(range2);
        Assert.Equal(new[] { new BitRange(5, 20) }, set);
    }

    [Fact]
    public void AddOverlappingLeftEdge()
    {
        var range1 = new BitRange(10, 20);
        var range2 = new BitRange(5, 10);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.Equal(new[] { range1 }, set);

        set.Add(range2);
        Assert.Equal(new[] { new BitRange(5, 20) }, set);
    }

    [Fact]
    public void AddOverlappingMergingTwo()
    {
        var range1 = new BitRange(10, 20);
        var range2 = new BitRange(30, 40);

        var range3 = new BitRange(15, 35);

        var set = new BitRangeUnion
        {
            range1,
            range2
        };

        Assert.Equal(new[] { range1, range2 }, set);

        set.Add(range3);
        Assert.Equal(new[] { new BitRange(10, 40) }, set);
    }

    [Fact]
    public void AddOverlappingMergingTwoMiddle()
    {
        var range1 = new BitRange(10, 20);
        var range2 = new BitRange(30, 40);
        var range3 = new BitRange(50, 60);

        var range4 = new BitRange(35, 55);

        var set = new BitRangeUnion
        {
            range1,
            range2,
            range3
        };

        Assert.Equal(new[] { range1, range2, range3 }, set);

        set.Add(range4);
        Assert.Equal(new[] { range1, new BitRange(30, 60) }, set);
    }

    [Fact]
    public void AddOverlappingMergingThreeMiddle()
    {
        var range1 = new BitRange(10, 20);
        var range2 = new BitRange(30, 40);
        var range3 = new BitRange(50, 60);
        var range4 = new BitRange(70, 80);
        var range5 = new BitRange(90, 100);

        var range6 = new BitRange(35, 75);

        var set = new BitRangeUnion
        {
            range1,
            range2,
            range3,
            range4,
            range5
        };

        Assert.Equal(new[] { range1, range2, range3, range4, range5 }, set);

        set.Add(range6);
        Assert.Equal(new[] { range1, new BitRange(30, 80), range5 }, set);
    }

    [Fact]
    public void RemoveNonExisting()
    {
        var range1 = new BitRange(10, 20);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.False(set.Remove(new BitRange(30, 40)));
        Assert.Equal(new[] { range1 }, set);
    }

    [Fact]
    public void TruncateLeftExact()
    {
        var range1 = new BitRange(0x10, 0x20);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.True(set.Remove(new BitRange(0x15, 0x20)));
        Assert.Equal(new[] { new BitRange(0x10, 0x15) }, set);
    }

    [Fact]
    public void TruncateRight()
    {
        var range1 = new BitRange(0x10, 0x20);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.True(set.Remove(new BitRange(0x15, 0x30)));
        Assert.Equal(new[] { new BitRange(0x10, 0x15) }, set);
    }

    [Fact]
    public void TruncateRightExact()
    {
        var range1 = new BitRange(0x10, 0x20);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.True(set.Remove(new BitRange(0x15, 0x20)));
        Assert.Equal(new[] { new BitRange(0x10, 0x15) }, set);
    }

    [Fact]
    public void TruncateLeft()
    {
        var range1 = new BitRange(0x10, 0x20);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.True(set.Remove(new BitRange(0x5, 0x15)));
        Assert.Equal(new[] { new BitRange(0x15, 0x20) }, set);
    }

    [Fact]
    public void Split()
    {
        var range1 = new BitRange(0x0, 0x20);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.True(set.Remove(new BitRange(0x5, 0x15)));
        Assert.Equal(new[] { new BitRange(0, 5), new BitRange(0x15, 0x20) }, set);
    }

    [Fact]
    public void RemoveEntireRange()
    {
        var range1 = new BitRange(0x10, 0x20);
        var set = new BitRangeUnion
        {
            range1
        };

        Assert.True(set.Remove(new BitRange(0x0, 0x30)));
        Assert.Empty(set);
    }

    [Fact]
    public void RemoveMultiple()
    {
        var set = new BitRangeUnion
        {
            new(0x10, 0x20),
            new(0x30, 0x40),
            new(0x50, 0x60),
        };

        Assert.True(set.Remove(new BitRange(0x15, 0x55)));
        Assert.Equal(new[] { new BitRange(0x10, 0x15), new BitRange(0x55, 0x60) }, set);
    }

    [Fact]
    public void EnclosingRange()
    {
        var union = new BitRangeUnion();
        Assert.Equal(BitRange.Empty, union.EnclosingRange);

        union.Add(new BitRange(0, 10));
        Assert.Equal(new BitRange(0, 10), union.EnclosingRange);

        union.Add(new BitRange(10, 20));
        Assert.Equal(new BitRange(0, 20), union.EnclosingRange);

        union.Add(new BitRange(40, 50));
        Assert.Equal(new BitRange(0, 50), union.EnclosingRange);
    }

    [Fact]
    public void GetOverlappingRanges()
    {
        var union = new BitRangeUnion
        {
            new BitRange(0, 10),
            new BitRange(20, 30),
            new BitRange(40, 50),
        };

        Span<BitRange> ranges = stackalloc BitRange[union.Count];

        ranges.Clear();
        Assert.Equal(1, union.GetOverlappingRanges(new BitRange(5, 15), ranges));
        Assert.Equal(new BitRange(0, 10), ranges[0]);

        ranges.Clear();
        Assert.Equal(1, union.GetOverlappingRanges(new BitRange(25, 35), ranges));
        Assert.Equal(new BitRange(20, 30), ranges[0]);

        ranges.Clear();
        Assert.Equal(2, union.GetOverlappingRanges(new BitRange(5, 25), ranges));
        Assert.Equal([new BitRange(0, 10), new BitRange(20, 30)], ranges[..2]);

        ranges.Clear();
        Assert.Equal(2, union.GetOverlappingRanges(new BitRange(25, 45), ranges));
        Assert.Equal([new BitRange(20, 30), new BitRange(40, 50)], ranges[..2]);

        ranges.Clear();
        Assert.Equal(3, union.GetOverlappingRanges(new BitRange(0, 100), ranges));
        Assert.Equal([new BitRange(0, 10), new BitRange(20, 30), new BitRange(40, 50)], ranges[..3]);
    }

    [Fact]
    public void GetIntersectingRanges()
    {
        var union = new BitRangeUnion
        {
            new BitRange(0, 10),
            new BitRange(20, 30),
            new BitRange(40, 50),
        };

        Span<BitRange> ranges = stackalloc BitRange[union.Count];

        ranges.Clear();
        Assert.Equal(1, union.GetIntersectingRanges(new BitRange(5, 15), ranges));
        Assert.Equal(new BitRange(5, 10), ranges[0]);

        ranges.Clear();
        Assert.Equal(1, union.GetIntersectingRanges(new BitRange(25, 35), ranges));
        Assert.Equal(new BitRange(25, 30), ranges[0]);

        ranges.Clear();
        Assert.Equal(2, union.GetIntersectingRanges(new BitRange(5, 25), ranges));
        Assert.Equal([new BitRange(5, 10), new BitRange(20, 25)], ranges[..2]);

        ranges.Clear();
        Assert.Equal(2, union.GetIntersectingRanges(new BitRange(25, 45), ranges));
        Assert.Equal([new BitRange(25, 30), new BitRange(40, 45)], ranges[..2]);

        ranges.Clear();
        Assert.Equal(3, union.GetIntersectingRanges(new BitRange(0, 100), ranges));
        Assert.Equal([new BitRange(0, 10), new BitRange(20, 30), new BitRange(40, 50)], ranges[..3]);
    }
}