using System.Globalization;
using System.Text;
using HotChocolate.Buffers;

namespace HotChocolate.Language.SyntaxTree;

public class ValueNodeConcurrencyTests
{
    [Fact]
    public void Value_Should_ReturnInteger_When_ReadConcurrently()
    {
        // arrange
        static IntValueNode Create() => new(123);

        // act
        var results = ReadConcurrently(Create, node => node.Value, node => node.Value);

        // assert
        Assert.Equal(["123"], results);
    }

    [Fact]
    public void WithLocation_Should_PreserveInteger_When_ClonedDuringFormatting()
    {
        // arrange
        static IntValueNode Create() => new(123);

        // act
        var results = ReadConcurrently(
            Create,
            node => node.Value,
            node =>
            {
                var clone = node.WithLocation(null);
                var text = Encoding.UTF8.GetString(clone.AsMemorySegment().Span);
                var number = clone.ToDouble().ToString(CultureInfo.InvariantCulture);
                return $"{text}:{number}";
            });

        // assert
        Assert.Equal(["123", "123:123"], results);
    }

    [Fact]
    public void WithLocation_Should_PreserveDouble_When_ClonedDuringFormatting()
    {
        // arrange
        static FloatValueNode Create() => new(123.5);

        // act
        var results = ReadConcurrently(
            Create,
            node => node.Value,
            node => Encoding.UTF8.GetString(node.WithLocation(null).AsSpan()));

        // assert
        Assert.Equal(["123.5"], results);
    }

    [Fact]
    public void AsMemorySegment_Should_ReturnDecimal_When_ReadDuringFormatting()
    {
        // arrange
        static FloatValueNode Create() => new(123.5m);

        // act
        var results = ReadConcurrently(
            Create,
            node => node.Value,
            node => Encoding.UTF8.GetString(node.AsMemorySegment().Span));

        // assert
        Assert.Equal(["123.5"], results);
    }

    [Fact]
    public void AsSpan_Should_ReturnEnum_When_ReadConcurrently()
    {
        // arrange
        static EnumValueNode Create() => new("ELECTRONICS");

        // act
        var results = ReadConcurrently(
            Create,
            node => Encoding.UTF8.GetString(node.AsSpan()),
            node => Encoding.UTF8.GetString(node.AsMemorySegment().Span));

        // assert
        Assert.Equal(["ELECTRONICS"], results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("héllo")]
    public void AsSpan_Should_ReturnString_When_ReadConcurrently(string value)
    {
        // arrange
        StringValueNode Create() => new(value);

        // act
        var results = ReadConcurrently(
            Create,
            node => Encoding.UTF8.GetString(node.AsSpan()),
            node => Encoding.UTF8.GetString(node.AsMemorySegment().Span));

        // assert
        Assert.Equal([value], results);
    }

    [Fact]
    public void ToInt32_Should_UseMatchingCacheTagAndPayload_When_ConversionsRunConcurrently()
    {
        // arrange
        var text = new ReadOnlyMemorySegment("-123"u8.ToArray());
        IntValueNode Create() => new(text);

        // act
        var results = ReadConcurrently(
            Create,
            node =>
            {
                node.ToInt16();
                return node.ToInt32().ToString(CultureInfo.InvariantCulture);
            },
            node =>
            {
                node.ToInt32();
                return node.ToInt16().ToString(CultureInfo.InvariantCulture);
            });

        // assert
        Assert.Equal(["-123"], results);
    }

    [Fact]
    public void ToDecimal_Should_UseMatchingCacheTagAndPayload_When_ConversionsRunConcurrently()
    {
        // arrange
        var text = new ReadOnlyMemorySegment("123.5"u8.ToArray());
        FloatValueNode Create() => new(text, FloatFormat.FixedPoint);

        // act
        var results = ReadConcurrently(
            Create,
            node =>
            {
                node.ToSingle();
                return node.ToDecimal().ToString(CultureInfo.InvariantCulture);
            },
            node =>
            {
                node.ToDecimal();
                return node.ToSingle().ToString(CultureInfo.InvariantCulture);
            });

        // assert
        Assert.Equal(["123.5"], results);
    }

    private static string[] ReadConcurrently<TNode>(
        Func<TNode> create,
        Func<TNode, string> first,
        Func<TNode, string> second)
    {
        var nodes = new TNode[200_000];

        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i] = create();
        }

        using var barrier = new Barrier(2);
        var results = new HashSet<string>[2];
        var threads = new Thread[2];

        for (var t = 0; t < threads.Length; t++)
        {
            var read = t == 0 ? first : second;
            var threadResults = results[t] = new HashSet<string>(StringComparer.Ordinal);
            threads[t] = new Thread(() =>
            {
                for (var i = 0; i < nodes.Length; i++)
                {
                    if (i % 32 == 0)
                    {
                        barrier.SignalAndWait();
                    }

                    try
                    {
                        threadResults.Add(read(nodes[i]));
                    }
                    catch (Exception exception)
                    {
                        threadResults.Add($"Exception: {exception.GetType().Name}");
                    }
                }
            });
            threads[t].Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        return results
            .SelectMany(values => values)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
