using System.Buffers;
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
