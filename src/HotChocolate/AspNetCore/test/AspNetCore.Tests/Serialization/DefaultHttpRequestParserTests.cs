using System.Collections;
using System.Text;
using HotChocolate.Execution.Caching;
using HotChocolate.Language;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace HotChocolate.AspNetCore.Serialization;

public sealed class DefaultHttpRequestParserTests
{
    [Fact]
    public async Task ParseRequestAsync_Valid_QueryId()
    {
        // arrange
        const string json =
            """
            {
                "id": "abc1213_5164-ABC-123"
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // act
        var parser = new DefaultHttpRequestParser(
            new DefaultDocumentCache(),
            new Sha256DocumentHashProvider(),
            256,
            ParserOptions.Default);
        var request = await parser.ParseRequestAsync(stream, CancellationToken.None);

        // assert
        Assert.Collection(
            request.Select(r => r.QueryId),
            id => Assert.Equal("abc1213_5164-ABC-123", id));
    }

    [InlineData("abc1213_5164-ABC-123/")]
    [InlineData("abc1213_5164-ABC-123\\\\")]
    [InlineData("abc1213_5164-ABC-123|")]
    [InlineData("abc1213_5164-ABC-123~")]
    [InlineData("abc1213_5164-ABC-123=")]
    [Theory]
    public async Task ParseRequestAsync_Invalid_QueryId(string id)
    {
        // arrange
        var json = $"{{ \"id\": \"{id}\"}}";

        // act
        async Task Parse()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            var parser = new DefaultHttpRequestParser(
                new DefaultDocumentCache(),
                new Sha256DocumentHashProvider(),
                256,
                ParserOptions.Default);

            await parser.ParseRequestAsync(stream, CancellationToken.None);
        }

        // assert
        var ex = await Assert.ThrowsAsync<GraphQLRequestException>(Parse);
        Assert.Equal("Invalid query id format.", ex.Message);
    }

    [Fact]
    public void ParseRequest_Valid_QueryId()
    {
        // arrange
        const string json =
            """
            {
                "id": "abc1213_5164-ABC-123"
            }
            """;

        // act
        var parser = new DefaultHttpRequestParser(
            new DefaultDocumentCache(),
            new Sha256DocumentHashProvider(),
            256,
            ParserOptions.Default);
        var request = parser.ParseRequest(json);

        // assert
        Assert.Collection(
            request.Select(r => r.QueryId),
            id => Assert.Equal("abc1213_5164-ABC-123", id));
    }

    [InlineData("abc1213_5164-ABC-123/")]
    [InlineData("abc1213_5164-\\\\ABC-123")]
    [InlineData("abc1213_5164-ABC-123|")]
    [InlineData("abc1213_5164-ABC-123~")]
    [InlineData("abc1213_5164-ABC-123=")]
    [Theory]
    public void ParseRequest_Invalid_QueryId(string id)
    {
        // arrange
        var json = $"{{ \"id\": \"{id}\"}}";

        // act
        void Parse()
        {
            var parser = new DefaultHttpRequestParser(
                new DefaultDocumentCache(),
                new Sha256DocumentHashProvider(),
                256,
                ParserOptions.Default);

            parser.ParseRequest(json);
        }

        // assert
        var ex = Assert.Throws<GraphQLRequestException>(Parse);
        Assert.Equal("Invalid query id format.", ex.Message);
    }

    [Fact]
    public void ParseRequestFromParams_Valid_QueryId()
    {
        // arrange
        var queryParams = new MockQueryParams("abc1213_5164-ABC-123");

        // act
        var parser = new DefaultHttpRequestParser(
            new DefaultDocumentCache(),
            new Sha256DocumentHashProvider(),
            256,
            ParserOptions.Default);
        var request = parser.ParseRequestFromParams(queryParams);

        // assert
        Assert.Equal("abc1213_5164-ABC-123", request.QueryId);
    }

    [InlineData("abc1213_5164-ABC-123/")]
    [InlineData("abc1213_5164-ABC-123\\")]
    [InlineData("abc1213_5164-ABC-123|")]
    [InlineData("abc1213_5164-ABC-123~")]
    [InlineData("abc1213_5164-ABC-123=")]
    [Theory]
    public void ParseRequestFromParams_Invalid_QueryId(string id)
    {
        // arrange
        var queryParams = new MockQueryParams(id);

        // act
        void Parse()
        {
            var parser = new DefaultHttpRequestParser(
                new DefaultDocumentCache(),
                new Sha256DocumentHashProvider(),
                256,
                ParserOptions.Default);
            parser.ParseRequestFromParams(queryParams);
        }

        // assert
        var ex = Assert.Throws<GraphQLRequestException>(Parse);
        Assert.Equal("Invalid query id format.", ex.Message);
    }

    public sealed class MockQueryParams(string id) : IQueryCollection
    {
        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out StringValues value)
        {
            throw new NotImplementedException();
        }

        public int Count => throw new NotImplementedException();

        public ICollection<string> Keys => throw new NotImplementedException();

        public StringValues this[string key] => key.Equals("id") ? id : null;
    }
}
