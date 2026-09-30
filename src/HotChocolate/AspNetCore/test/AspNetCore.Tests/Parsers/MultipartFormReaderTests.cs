using System.IO.Pipelines;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace HotChocolate.AspNetCore.Parsers;

public class MultipartFormReaderTests
{
    private const int MaxRequestSize = 64;
    private const string Operations = """{ "query": "{ __typename }" }""";
    private const string Map = """{ "1": ["variables.file"] }""";

    [Fact]
    public async Task ReadAsync_Should_ReturnFieldsAndFiles_When_FormHasBoth()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" },
            { new StringContent(Map), "map" },
            { CreateFile("file content"), "1", "file.txt" }
        };
        var request = await CreateRequestAsync(form);

        // act
        var result = await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var file = Assert.Single(result.Files);
        Assert.Equal(Operations, result["operations"].ToString());
        Assert.Equal(Map, result["map"].ToString());
        Assert.Equal(("1", "file.txt", "text/plain"), (file.Name, file.FileName, file.ContentType));
        Assert.Equal("file content", await ReadFileAsync(file));
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnEmptyForm_When_ContentLengthIsZero()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" }
        };
        var request = await CreateRequestAsync(form);
        request.ContentLength = 0;

        // act
        var result = await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Same(FormCollection.Empty, result);
    }

    [Fact]
    public async Task ReadAsync_Should_ReadForm_When_ContentLengthIsUnknown()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" }
        };
        var request = await CreateRequestAsync(form);
        request.ContentLength = null;

        // act
        var result = await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(Operations, result["operations"].ToString());
    }

    [Fact]
    public async Task ReadAsync_Should_KeepEveryValue_When_FieldRepeats()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent("a"), "extra" },
            { new StringContent("b"), "extra" }
        };
        var request = await CreateRequestAsync(form);

        // act
        var result = await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Collection(
            result["extra"],
            value => Assert.Equal("a", value),
            value => Assert.Equal("b", value));
    }

    [Fact]
    public async Task ReadAsync_Should_SkipSection_When_DispositionIsNotFormData()
    {
        // arrange
        using var content = CreateRawContent(
            "--b\r\n"
            + "Content-Disposition: attachment; name=\"extra\"\r\n"
            + "\r\n"
            + "x\r\n"
            + "--b\r\n"
            + "Content-Disposition: form-data; name=\"operations\"\r\n"
            + "\r\n"
            + Operations + "\r\n"
            + "--b--\r\n");
        var request = await CreateRequestAsync(content);

        // act
        var result = await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var field = Assert.Single(result);
        Assert.Equal("operations", field.Key);
        Assert.Equal(Operations, field.Value.ToString());
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnFileContent_When_FileExceedsMemoryBufferThreshold()
    {
        // arrange
        var content = new string('x', 1024);
        using var form = new MultipartFormDataContent
        {
            { CreateFile(content), "1", "file.txt" }
        };
        var request = await CreateRequestAsync(form);

        // act
        var result = await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { MemoryBufferThreshold = 16 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var file = Assert.Single(result.Files);
        Assert.Equal(content, await ReadFileAsync(file));
    }

    [Fact]
    public async Task ReadAsync_Should_ReturnFileFromBufferedBody_When_BufferBodyIsSet()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" },
            { CreateFile("file content"), "1", "file.txt" }
        };
        var request = await CreateRequestAsync(form);

        // act
        var result = await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { BufferBody = true },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(request.Body.CanSeek);
        Assert.Equal(0, request.Body.Position);
        var file = Assert.Single(result.Files);
        Assert.Equal("file content", await ReadFileAsync(file));
    }

    [Fact]
    public async Task ReadAsync_Should_ReadSections_When_SectionsAreAtTheirLimits()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations.PadRight(MaxRequestSize)), "operations" },
            { CreateFile(new string('x', MaxRequestSize)), "1", "file.txt" }
        };
        var request = await CreateRequestAsync(form);

        // act
        var result = await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { MultipartBodyLengthLimit = MaxRequestSize },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(MaxRequestSize, result["operations"].ToString().Length);
        Assert.Equal(MaxRequestSize, Assert.Single(result.Files).Length);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowMaxRequestSize_When_OperationsExceedsMaxRequestSize()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations.PadRight(MaxRequestSize + 1)), "operations" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<GraphQLRequestException>(Action);
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0010", error.Code);
        Assert.Equal("Request size exceeds maximum allowed size.", error.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowMaxRequestSize_When_RepeatedOperationsTooLarge()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" },
            { new StringContent(Operations.PadRight(MaxRequestSize + 1)), "operations" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<GraphQLRequestException>(Action);
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0010", error.Code);
        Assert.Equal("Request size exceeds maximum allowed size.", error.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowSectionTooLarge_When_FileExceedsSectionLimit()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" },
            { CreateFile(new string('x', 33)), "1", "file.txt" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { MultipartBodyLengthLimit = 32 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<GraphQLRequestException>(Action);
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0135", error.Code);
        Assert.Equal("The multipart section '1' exceeds the maximum allowed size.", error.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowSectionTooLarge_When_OperationsExceedsSectionLimit()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations.PadRight(33)), "operations" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { MultipartBodyLengthLimit = 32 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<GraphQLRequestException>(Action);
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0135", error.Code);
        Assert.Equal(
            "The multipart section 'operations' exceeds the maximum allowed size.",
            error.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowSectionTooLarge_When_BufferedFileExceedsSectionLimit()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" },
            { CreateFile(new string('x', 33)), "1", "file.txt" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { BufferBody = true, MultipartBodyLengthLimit = 32 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<GraphQLRequestException>(Action);
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0135", error.Code);
        Assert.Equal("The multipart section '1' exceeds the maximum allowed size.", error.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowSectionTooLarge_When_MapExceedsSectionLimit()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" },
            { new StringContent(Map.PadRight(33)), "map" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { MultipartBodyLengthLimit = 32 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<GraphQLRequestException>(Action);
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0135", error.Code);
        Assert.Equal(
            "The multipart section 'map' exceeds the maximum allowed size.",
            error.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowSectionTooLarge_When_SkippedSectionTooLarge()
    {
        // arrange
        using var content = CreateRawContent(
            "--b\r\n"
            + "Content-Disposition: attachment; name=\"extra\"\r\n"
            + "\r\n"
            + new string('x', 33) + "\r\n"
            + "--b--\r\n");
        var request = await CreateRequestAsync(content);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { MultipartBodyLengthLimit = 32 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<GraphQLRequestException>(Action);
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0135", error.Code);
        Assert.Equal(
            "The multipart section 'extra' exceeds the maximum allowed size.",
            error.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ThrowRequestBodyTooLarge_When_BufferedBodyTooLarge()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" },
            { CreateFile(new string('x', 1024)), "1", "file.txt" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { BufferBody = true, BufferBodyLengthLimit = 512 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<GraphQLRequestException>(Action);
        var error = Assert.Single(exception.Errors);
        Assert.Equal("HC0136", error.Code);
        Assert.Equal(
            "The request body exceeds the maximum size the server accepts.",
            error.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_Throw_When_BoundaryIsMissing()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" }
        };
        var request = await CreateRequestAsync(form);
        request.ContentType = "multipart/form-data";

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(Action);
        Assert.Equal("Missing content-type boundary.", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_Throw_When_ContentTypeIsNotMultipart()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" }
        };
        var request = await CreateRequestAsync(form);
        request.ContentType = "text/plain";

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Action);
        Assert.Equal("Incorrect Content-Type: text/plain", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_Throw_When_SectionHeadersExceedHeadersCountLimit()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { MultipartHeadersCountLimit = 1 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(Action);
        Assert.Equal("Multipart headers count limit 1 exceeded.", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_Throw_When_SectionHeaderExceedsHeadersLengthLimit()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { MultipartHeadersLengthLimit = 16 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(Action);
        Assert.Equal("Line length limit 16 exceeded.", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_Throw_When_BoundaryExceedsBoundaryLengthLimit()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { MultipartBoundaryLengthLimit = 8 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(Action);
        Assert.Equal("Multipart boundary length limit 8 exceeded.", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_Throw_When_SectionCountExceedsValueCountLimit()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" },
            { CreateFile("file content"), "1", "file.txt" }
        };
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions { ValueCountLimit = 1 },
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(Action);
        Assert.Equal("Form value count limit 1 exceeded.", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_Throw_When_ContentDispositionIsInvalid()
    {
        // arrange
        using var form = new MultipartFormDataContent();
        var request = await CreateRequestAsync(form);

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(Action);
        Assert.Equal("Form section has invalid Content-Disposition value: ", exception.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_Throw_When_AntiforgeryValidationFailed()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" }
        };
        var request = await CreateRequestAsync(form);
        request.HttpContext.Features.Set<IAntiforgeryValidationFeature>(
            new AntiforgeryValidationFeature(isValid: false));

        // act
        async Task Action() => await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Action);
        Assert.Equal(
            "This form is being accessed with a failed antiforgery validation. Validate the "
            + "`IAntiforgeryValidationFeature` on the request before reading from the form.",
            exception.Message);
    }

    [Fact]
    public async Task ReadAsync_Should_ReadForm_When_AntiforgeryValidationSucceeded()
    {
        // arrange
        using var form = new MultipartFormDataContent
        {
            { new StringContent(Operations), "operations" }
        };
        var request = await CreateRequestAsync(form);
        request.HttpContext.Features.Set<IAntiforgeryValidationFeature>(
            new AntiforgeryValidationFeature(isValid: true));

        // act
        var result = await MultipartFormReader.ReadAsync(
            request,
            new FormOptions(),
            MaxRequestSize,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(Operations, result["operations"].ToString());
    }

    private static ByteArrayContent CreateFile(string content)
    {
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        return file;
    }

    private static ByteArrayContent CreateRawContent(string body)
    {
        var content = new ByteArrayContent(Encoding.ASCII.GetBytes(body));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data; boundary=b");

        return content;
    }

    private static async Task<HttpRequest> CreateRequestAsync(HttpContent content)
    {
        var body = new MemoryStream();
        await content.CopyToAsync(body, TestContext.Current.CancellationToken);
        body.Position = 0;

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = content.Headers.ContentType?.ToString();
        context.Request.ContentLength = body.Length;
        context.Request.Body = PipeReader.Create(body).AsStream();

        return context.Request;
    }

    private static async Task<string> ReadFileAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());

        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    private sealed class AntiforgeryValidationFeature(bool isValid) : IAntiforgeryValidationFeature
    {
        public bool IsValid => isValid;

        public Exception? Error => null;
    }
}
