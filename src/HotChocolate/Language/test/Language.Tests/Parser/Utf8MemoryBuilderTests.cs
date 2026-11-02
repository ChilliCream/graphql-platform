using System.Text;

namespace HotChocolate.Language;

public class Utf8MemoryBuilderTests
{
    [Fact]
    public void NextIndex_Should_Throw_When_Sealed()
    {
        // arrange
        var builder = new Utf8MemoryBuilder();
        builder.Seal();

        // act
        void Act()
        {
            _ = builder.NextIndex;
        }

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void NextIndex_Should_Throw_When_Abandoned()
    {
        // arrange
        var builder = new Utf8MemoryBuilder();
        builder.Abandon();

        // act
        void Act()
        {
            _ = builder.NextIndex;
        }

        // assert
        Assert.Throws<InvalidOperationException>(Act);
    }

    [Fact]
    public void Seal_Should_PreserveWrittenContent_When_BufferContainsData()
    {
        // arrange
        var builder = new Utf8MemoryBuilder();
        var segment = builder.Write("100"u8);

        // act
        builder.Seal();

        // assert
        Snapshot.Create()
            .Add(Encoding.UTF8.GetString(builder.WrittenSpan), "WrittenSpan")
            .Add(Encoding.UTF8.GetString(builder.WrittenMemory.Span), "WrittenMemory")
            .Add(Encoding.UTF8.GetString(segment.Span), "Segment")
            .MatchMarkdownSnapshot();
    }
}
