using System.Buffers;
using System.Text;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace HotChocolate.AspNetCore.Formatters;

public sealed class DefaultHttpResponseFormatterTests
{
    [Theory]
    [InlineData(HttpTransportVersion.Latest, HttpTransportVersion.Draft20250508)]
    [InlineData(HttpTransportVersion.Legacy, HttpTransportVersion.Legacy)]
    [InlineData(HttpTransportVersion.Draft20230127, HttpTransportVersion.Draft20250508)]
    [InlineData(HttpTransportVersion.Draft20250508, HttpTransportVersion.Draft20250508)]
    [InlineData(HttpTransportVersion.Draft20260903, HttpTransportVersion.Draft20260903)]
    public void Constructor_Should_ResolveTransportVersion_When_VersionIsRecognized(
        HttpTransportVersion configured,
        HttpTransportVersion expected)
    {
        // arrange
        var options = new HttpResponseFormatterOptions { HttpTransportVersion = configured };

        // act
        var formatter = new DefaultHttpResponseFormatter(options);

        // assert
        Assert.Equal(expected, formatter.TransportVersion);
    }

    [Fact]
    public void Constructor_Should_Throw_When_VersionIsUnrecognized()
    {
        // arrange
        var options = new HttpResponseFormatterOptions
        {
            HttpTransportVersion = (HttpTransportVersion)99
        };

        // act
        void Act() => _ = new DefaultHttpResponseFormatter(options);

        // assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(Act);
        Assert.Equal("options", exception.ParamName);
        Assert.Equal((HttpTransportVersion)99, exception.ActualValue);
        Assert.Equal(
            "The specified HTTP transport version `99` is not supported. (Parameter 'options')"
            + Environment.NewLine
            + "Actual value was 99.",
            exception.Message);
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("TEXT/HTML")]
    [InlineData("text/html;q=0.5")]
    [InlineData("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")]
    [InlineData("text/html, application/json;q=0.5")]
    [InlineData("text/html, */*;q=0.9")]
    [InlineData("application/json;q=0.5", "text/html")]
    public void PrefersHtml_Should_ReturnTrue_When_HtmlIsRatedAboveEveryGraphQLMediaType(
        params string[] accept)
    {
        // arrange
        var acceptMediaTypes = ParseAccept(accept);

        // act
        var prefersHtml = DefaultHttpResponseFormatter.PrefersHtml(acceptMediaTypes);

        // assert
        Assert.True(prefersHtml);
    }

    [Theory]
    [InlineData]
    [InlineData("*/*")]
    [InlineData("text/*")]
    [InlineData("text/html, application/json")]
    [InlineData("text/html, application/*")]
    [InlineData("text/html, multipart/mixed")]
    [InlineData("text/html;q=0, */*")]
    [InlineData("text/html;q=0")]
    [InlineData("text/event-stream")]
    [InlineData("application/graphql-response+json, application/json;q=0.9")]
    [InlineData("application/graphql-response+json, text/html;q=0.1")]
    public void PrefersHtml_Should_ReturnFalse_When_AGraphQLMediaTypeIsRatedAtLeastAsHigh(
        params string[] accept)
    {
        // arrange
        var acceptMediaTypes = ParseAccept(accept);

        // act
        var prefersHtml = DefaultHttpResponseFormatter.PrefersHtml(acceptMediaTypes);

        // assert
        Assert.False(prefersHtml);
    }

    [Fact]
    public async Task FormatAsync_Should_SetContentLengthToBodyLength_When_ResultIsSingleJson()
    {
        // arrange
        var formatter = new DefaultHttpResponseFormatter();
        var context = new DefaultHttpContext();
        await using var body = new MemoryStream();
        context.Response.Body = body;
        await using var result = new OperationResult(
            ImmutableOrderedDictionary<string, object?>.Empty.Add("greeting", "hello"));

        // act
        await formatter.FormatAsync(
            context.Response,
            result,
            [],
            proposedStatusCode: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(body.Length, context.Response.ContentLength);
        Encoding.UTF8.GetString(body.ToArray()).MatchInlineSnapshot(
            """
            {"extensions":{"greeting":"hello"}}
            """);
    }

    [Fact]
    public async Task FormatAsync_Should_WriteCompleteBody_When_BodyIsLargerThanOneCopy()
    {
        // arrange
        var formatter = new DefaultHttpResponseFormatter();
        var context = new DefaultHttpContext();
        await using var body = new MemoryStream();
        context.Response.Body = body;
        await using var result = new OperationResult(
            ImmutableOrderedDictionary<string, object?>.Empty.Add("items", CreateItems(200_000)));
        var expected = new ArrayBufferWriter<byte>();
        JsonResultFormatter.Default.Format(result, expected);

        // act
        await formatter.FormatAsync(
            context.Response,
            result,
            [],
            proposedStatusCode: null,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expected.WrittenCount, context.Response.ContentLength);
        Assert.Equal(expected.WrittenSpan.ToArray(), body.ToArray());
    }

    [Fact]
    public async Task FormatAsync_Should_AbortRequestWithoutWritingBody_When_FormatterThrows()
    {
        // arrange
        var formatter = new DefaultHttpResponseFormatter();
        var lifetime = new RecordingRequestLifetimeFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpRequestLifetimeFeature>(lifetime);
        await using var body = new MemoryStream();
        context.Response.Body = body;
        await using var result = new OperationResult(
            ImmutableOrderedDictionary<string, object?>.Empty
                .Add("items", CreateItems(10_000))
                .Add("broken", new UnformattableValue()));

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await formatter.FormatAsync(
                context.Response,
                result,
                [],
                proposedStatusCode: null,
                TestContext.Current.CancellationToken));

        // assert
        Assert.Equal("boom", exception.Message);
        Assert.True(lifetime.IsAborted);
        Assert.Equal(0, body.Length);
        Assert.Null(context.Response.ContentLength);
    }

    private static string[] CreateItems(int count)
    {
        var items = new string[count];

        for (var i = 0; i < count; i++)
        {
            items[i] = $"item-{i}";
        }

        return items;
    }

    private static AcceptMediaType[] ParseAccept(string[] accept)
    {
        var context = new DefaultHttpContext();

        if (accept.Length > 0)
        {
            context.Request.Headers.Accept = accept;
        }

        var acceptHeader = HeaderUtilities.GetAcceptHeader(context.Request);
        Assert.False(acceptHeader.HasError);

        return acceptHeader.AcceptMediaTypes;
    }

    private sealed class RecordingRequestLifetimeFeature : IHttpRequestLifetimeFeature
    {
        public bool IsAborted { get; private set; }

        public CancellationToken RequestAborted { get; set; }

        public void Abort() => IsAborted = true;
    }

    private sealed class UnformattableValue
    {
        public override string ToString() => throw new InvalidOperationException("boom");
    }
}
