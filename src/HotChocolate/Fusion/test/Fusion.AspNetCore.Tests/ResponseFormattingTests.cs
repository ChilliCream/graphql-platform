using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class ResponseFormattingTests : FusionTestBase
{
    [Fact]
    public async Task Format_Should_WriteSameBytesAsEscapingFormatter_When_ResultIsComposedFromLookups()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<K6ExecutionTests.AccountsQuery>(d => d.Name("Query")));

        using var inventoryApi = CreateSourceSchema(
            "inventory-api",
            builder => builder.AddQueryType<K6ExecutionTests.InventoryQuery>(d => d.Name("Query")));

        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<K6ExecutionTests.ProductsQuery>(d => d.Name("Query")));

        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<K6ExecutionTests.ReviewsQuery>(d => d.Name("Query")));

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("accounts-api", accountsApi),
                ("inventory-api", inventoryApi),
                ("products-api", productsApi),
                ("reviews-api", reviewsApi)
            ]);

        // act
        var (expected, actual) = await FormatWithBothFormattersAsync(
            gateway,
            """
            query TestQuery {
              users {
                id
                username
                name
                reviews {
                  id
                  body
                  product {
                    inStock
                    name
                    price
                    shippingEstimate
                    upc
                    weight
                    reviews { id body author { id username name } }
                  }
                }
              }
              topProducts(first: 5) {
                __typename
                upc
                productName: name
                shippingEstimate
                reviews { id author { reviews { body } } }
              }
            }
            """);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Format_Should_WriteSameBytesAsEscapingFormatter_When_ResultHasSourceValues()
    {
        // arrange
        using var server = CreateSourceSchema(
            "a",
            b => b.AddQueryType<Query>().AddJsonTypeConverter());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("a", server)
            ]);

        // act
        var (expected, actual) = await FormatWithBothFormattersAsync(
            gateway,
            """
            {
              __typename
              escaped
              nothing
              flag
              count
              ratio
              numbers
              long
              any
              anyList
              again: escaped
            }
            """);

        // assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task FormatAsync_Should_WriteDataIntoPipeMemory_When_ResultIsMinimized()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<K6ExecutionTests.AccountsQuery>(d => d.Name("Query")));

        using var inventoryApi = CreateSourceSchema(
            "inventory-api",
            builder => builder.AddQueryType<K6ExecutionTests.InventoryQuery>(d => d.Name("Query")));

        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<K6ExecutionTests.ProductsQuery>(d => d.Name("Query")));

        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<K6ExecutionTests.ReviewsQuery>(d => d.Name("Query")));

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("accounts-api", accountsApi),
                ("inventory-api", inventoryApi),
                ("products-api", productsApi),
                ("reviews-api", reviewsApi)
            ]);

        var executor = await gateway.Services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await using var result = await executor.ExecuteAsync(
            """
            query TestQuery {
              users {
                id
                username
                name
                reviews {
                  id
                  body
                  product { inStock name price shippingEstimate upc weight reviews { id body } }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);
        var operationResult = Assert.IsType<OperationResult>(result);

        var expected = new ArrayBufferWriter<byte>();
        JsonResultFormatter.Default.Format(operationResult, expected);

        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));
        var writer = new SizeHintRecordingPipeWriter(pipe.Writer);

        // act
        await JsonResultFormatter.Default.FormatAsync(
            operationResult,
            writer,
            TestContext.Current.CancellationToken);
        await writer.CompleteAsync();

        // assert
        var read = await pipe.Reader.ReadAsync(TestContext.Current.CancellationToken);
        var actual = Encoding.UTF8.GetString(read.Buffer);
        pipe.Reader.AdvanceTo(read.Buffer.End);
        Assert.Equal(Encoding.UTF8.GetString(expected.WrittenSpan), actual);

        // only the enclosing {"data": and } go through the formatter's buffer
        Assert.Equal(expected.WrittenCount - 9, writer.BytesWrittenToLargeSpans);
    }

    [Fact]
    public async Task FormatAsync_Should_WriteLargeResultInFewPipeSegments_When_ResultIsMinimized()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<K6ExecutionTests.AccountsQuery>(d => d.Name("Query")));

        using var inventoryApi = CreateSourceSchema(
            "inventory-api",
            builder => builder.AddQueryType<K6ExecutionTests.InventoryQuery>(d => d.Name("Query")));

        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<K6ExecutionTests.ProductsQuery>(d => d.Name("Query")));

        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<K6ExecutionTests.ReviewsQuery>(d => d.Name("Query")));

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("accounts-api", accountsApi),
                ("inventory-api", inventoryApi),
                ("products-api", productsApi),
                ("reviews-api", reviewsApi)
            ]);

        var executor = await gateway.Services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await using var result = await executor.ExecuteAsync(
            """
            query TestQuery {
              a: users { ...UserReviews }
              b: users { ...UserReviews }
              c: users { ...UserReviews }
              d: users { ...UserReviews }
            }

            fragment UserReviews on User {
              id
              username
              name
              reviews {
                id
                body
                product {
                  inStock
                  name
                  price
                  shippingEstimate
                  upc
                  weight
                  reviews {
                    id
                    body
                    author {
                      id
                      username
                      name
                      reviews {
                        id
                        body
                        product { inStock name price shippingEstimate upc weight }
                      }
                    }
                  }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);
        var operationResult = Assert.IsType<OperationResult>(result);

        var expected = new ArrayBufferWriter<byte>();
        JsonResultFormatter.Default.Format(operationResult, expected);

        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 0, resumeWriterThreshold: 0));

        // act
        await JsonResultFormatter.Default.FormatAsync(
            operationResult,
            pipe.Writer,
            TestContext.Current.CancellationToken);
        await pipe.Writer.CompleteAsync();

        // assert
        var read = await pipe.Reader.ReadAsync(TestContext.Current.CancellationToken);
        var actual = Encoding.UTF8.GetString(read.Buffer);
        var segments = CountSegments(read.Buffer);
        pipe.Reader.AdvanceTo(read.Buffer.End);
        Assert.Equal(Encoding.UTF8.GetString(expected.WrittenSpan), actual);
        Assert.Equal(77_502, expected.WrittenCount);
        Assert.Equal(7, segments);
    }

    [Theory]
    [InlineData("multipart")]
    [InlineData("event-stream")]
    public async Task FormatAsync_Should_ForwardEmitterSizeHints_When_SingleResultIsWrittenAsMessage(
        string format)
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<K6ExecutionTests.AccountsQuery>(d => d.Name("Query")));

        using var inventoryApi = CreateSourceSchema(
            "inventory-api",
            builder => builder.AddQueryType<K6ExecutionTests.InventoryQuery>(d => d.Name("Query")));

        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<K6ExecutionTests.ProductsQuery>(d => d.Name("Query")));

        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<K6ExecutionTests.ReviewsQuery>(d => d.Name("Query")));

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("accounts-api", accountsApi),
                ("inventory-api", inventoryApi),
                ("products-api", productsApi),
                ("reviews-api", reviewsApi)
            ]);

        var executor = await gateway.Services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await using var result = await executor.ExecuteAsync(
            """
            query TestQuery {
              a: users { ...UserReviews }
              b: users { ...UserReviews }
            }

            fragment UserReviews on User {
              id
              username
              name
              reviews {
                id
                body
                product {
                  inStock
                  name
                  price
                  shippingEstimate
                  upc
                  weight
                  reviews { id body author { id username name } }
                }
              }
            }
            """,
            TestContext.Current.CancellationToken);
        var operationResult = Assert.IsType<OperationResult>(result);

        var expected = new ArrayBufferWriter<byte>();
        JsonResultFormatter.Default.Format(operationResult, expected);

        IExecutionResultFormatter formatter = format switch
        {
            "multipart" => new MultiPartResultFormatter(),
            _ => new EventStreamResultFormatter(new JsonResultFormatterOptions())
        };
        var writer = new SizeHintRecordingPipeWriter(PipeWriter.Create(Stream.Null));

        // act
        await formatter.FormatAsync(
            operationResult,
            writer,
            ExecutionResultFormatFlags.None,
            TestContext.Current.CancellationToken);
        await writer.CompleteAsync();

        // assert
        Assert.Equal(expected.WrittenCount - 9, writer.BytesWrittenToLargeSpans);
        Assert.Equal(2048, writer.MaxSizeHint);
    }

    private static async Task<(string Expected, string Actual)> FormatWithBothFormattersAsync(
        Gateway gateway,
        string query)
    {
        var executor = await gateway.Services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        await using var result = await executor.ExecuteAsync(
            query,
            TestContext.Current.CancellationToken);
        var operationResult = Assert.IsType<OperationResult>(result);

        // a custom encoder makes the composite result take the escaping JsonWriter path
        var escapingFormatter = new JsonResultFormatter(
            new JsonResultFormatterOptions { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) });

        var expected = new ArrayBufferWriter<byte>();
        escapingFormatter.Format(operationResult, expected);

        var actual = new ArrayBufferWriter<byte>();
        JsonResultFormatter.Default.Format(operationResult, actual);

        return (Encoding.UTF8.GetString(expected.WrittenSpan), Encoding.UTF8.GetString(actual.WrittenSpan));
    }

    private static int CountSegments(ReadOnlySequence<byte> buffer)
    {
        var segments = 0;
        var position = buffer.Start;

        while (buffer.TryGet(ref position, out _))
        {
            segments++;
        }

        return segments;
    }

    /// <summary>
    /// A pipe writer that counts the bytes written into spans requested with a size hint of
    /// at least 2 KB and records the largest size hint it receives.
    /// </summary>
    private sealed class SizeHintRecordingPipeWriter(PipeWriter inner) : PipeWriter
    {
        private bool _largeSpan;

        public long BytesWrittenToLargeSpans { get; private set; }

        public int MaxSizeHint { get; private set; }

        public override void Advance(int bytes)
        {
            if (_largeSpan)
            {
                BytesWrittenToLargeSpans += bytes;
            }

            inner.Advance(bytes);
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            _largeSpan = sizeHint >= 2048;
            MaxSizeHint = Math.Max(MaxSizeHint, sizeHint);
            return inner.GetMemory(sizeHint);
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            _largeSpan = sizeHint >= 2048;
            MaxSizeHint = Math.Max(MaxSizeHint, sizeHint);
            return inner.GetSpan(sizeHint);
        }

        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            => inner.FlushAsync(cancellationToken);

        public override void CancelPendingFlush() => inner.CancelPendingFlush();

        public override void Complete(Exception? exception = null) => inner.Complete(exception);
    }

    public class Query
    {
        public string GetEscaped() => "quote \" backslash \\ tab \t unicode é中 <tag> &amp;";

        public string? GetNothing() => null;

        public bool GetFlag() => true;

        public int GetCount() => -42;

        public double GetRatio() => 3.5e-7;

        public List<int?> GetNumbers() => [1, null, 3];

        public string GetLong() => new('x', 20_000);

        [GraphQLType<AnyType>]
        public Dictionary<string, object?> GetAny()
            => new()
            {
                { "name", "test \"quoted\"" },
                { "nested", new Dictionary<string, object?> { { "number", 123 }, { "empty", new List<object?>() } } },
                { "list", new List<object?> { "a", 1, false, null, new Dictionary<string, object?>() } }
            };

        [GraphQLType<AnyType>]
        public List<object?> GetAnyList() => ["string", 123, true, 45.67, null];
    }
}
