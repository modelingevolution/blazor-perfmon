using Frontend.Models;
using Xunit;

namespace Backend.Tests.Unit;

public class CircularBufferTests
{
    [Fact]
    public void Constructor_SetsCapacity()
    {
        var buffer = new CircularBuffer<int>(10);

        Assert.Equal(10, buffer.Capacity);
        Assert.Equal(0, buffer.Count);
    }

    [Fact]
    public void Add_IncreasesCount()
    {
        var buffer = new CircularBuffer<int>(10);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);

        Assert.Equal(3, buffer.Count);
    }

    [Fact]
    public void Add_WrapsAroundWhenFull()
    {
        var buffer = new CircularBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Add(4); // Should overwrite 1

        Assert.Equal(3, buffer.Count);
        Assert.Equal(2, buffer[0]); // Oldest is now 2
        Assert.Equal(4, buffer[2]); // Newest is 4
    }

    [Fact]
    public void Indexer_ReturnsCorrectOrder()
    {
        var buffer = new CircularBuffer<int>(5);

        buffer.Add(10);
        buffer.Add(20);
        buffer.Add(30);

        Assert.Equal(10, buffer[0]); // Oldest
        Assert.Equal(20, buffer[1]);
        Assert.Equal(30, buffer[2]); // Newest
    }

    [Fact]
    public void GetLatest_ReturnsNewestItem()
    {
        var buffer = new CircularBuffer<int>(5);

        buffer.Add(10);
        buffer.Add(20);
        buffer.Add(30);

        Assert.Equal(30, buffer.GetLatest());
    }

    [Fact]
    public void GetLatest_ReturnsDefaultWhenEmpty()
    {
        var buffer = new CircularBuffer<int>(5);

        Assert.Equal(0, buffer.GetLatest());
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var buffer = new CircularBuffer<int>(5);

        buffer.Add(10);
        buffer.Add(20);
        buffer.Add(30);

        buffer.Clear();

        Assert.Equal(0, buffer.Count);
        Assert.Equal(0, buffer.GetLatest());
    }

    [Fact]
    public void ToArray_ReturnsItemsInCorrectOrder()
    {
        var buffer = new CircularBuffer<int>(5);

        buffer.Add(10);
        buffer.Add(20);
        buffer.Add(30);

        var array = buffer.ToArray();

        Assert.Equal(3, array.Length);
        Assert.Equal(10, array[0]);
        Assert.Equal(20, array[1]);
        Assert.Equal(30, array[2]);
    }

    [Fact]
    public void WrapAround_MaintainsCorrectOrder()
    {
        var buffer = new CircularBuffer<int>(3);

        // Fill buffer
        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);

        // Wrap around
        buffer.Add(4);
        buffer.Add(5);

        Assert.Equal(3, buffer.Count);
        Assert.Equal(3, buffer[0]); // Oldest after wrap
        Assert.Equal(4, buffer[1]);
        Assert.Equal(5, buffer[2]); // Newest
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void PerformanceTest_HandlesManyOperations(int count)
    {
        var buffer = new CircularBuffer<float>(120);

        // Add many items
        for (int i = 0; i < count; i++)
        {
            buffer.Add((float)i);
        }

        // Verify final state
        Assert.Equal(Math.Min(count, 120), buffer.Count);
        Assert.Equal((float)(count - 1), buffer.GetLatest());
    }
}
