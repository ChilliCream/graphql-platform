using HotChocolate.AspNetCore.Tests.Utilities;
using Newtonsoft.Json;

namespace HotChocolate.AspNetCore;

public class HttpMultipartMiddlewareTests : ServerTestBase
{
    public HttpMultipartMiddlewareTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task EmptyForm_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent();
        form.Headers.Add(HttpHeaderKeys.Preflight, "1");
        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Fail_Without_Preflight_Header()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent();
        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task EmptyOperations_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
        {
            { new StringContent(""), "operations" },
        };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task IncompleteOperations_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{}"), "map" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MapWithNoOperations_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "map" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MapBeforeOperations_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "map" },
                { new StringContent("{}"), "operations" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task EmptyMap_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent(""), "map" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task InvalidMap_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("data"), "map" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MissingFile_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{ \"1\": [\"variables.file\"] }"), "map" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MissingKeyInMap_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{ \"\": [\"variables.file\"] }"), "map" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task MissingObjectPathsForKey_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent("{}"), "operations" },
                { new StringContent("{ \"1\": [] }"), "map" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form);

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_File()
    {
        // arrange
        var server = CreateStarWarsServer();

        var query = @"
                query ($upload: Upload!) {
                    singleUpload(file: $upload)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null },
                },
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_Optional_File()
    {
        // arrange
        var server = CreateStarWarsServer();

        var query = @"
                query ($upload: Upload) {
                    optionalUpload(file: $upload)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null },
                },
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_Optional_File_In_InputObject()
    {
        // arrange
        var server = CreateStarWarsServer();

        var query = @"
                query ($input: InputWithOptionalFileInput!) {
                    optionalObjectUpload(input: $input)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "input", new Dictionary<string, object?> { { "file", null }, } },
                },
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.input.file\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_Optional_File_In_Inline_InputObject()
    {
        // arrange
        var server = CreateStarWarsServer();

        var query = @"
                query ($upload: Upload!) {
                    optionalObjectUpload(input: { file: $upload })
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null },
                },
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_File_In_InputObject()
    {
        // arrange
        var server = CreateStarWarsServer();

        var query = @"
                query ($input: InputWithFileInput!) {
                    objectUpload(input: $input)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "input", new Dictionary<string, object?> { { "file", null }, } },
                },
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.input.file\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_File_Inline_InputObject()
    {
        // arrange
        var server = CreateStarWarsServer();

        var query = @"
            query ($upload: Upload!) {
                objectUpload(input: { file: $upload })
            }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null },
                },
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_File_In_List()
    {
        // arrange
        var server = CreateStarWarsServer();

        const string query =
            @"query ($input: [[InputWithFileInput!]!]!) {
                listUpload(input: $input)
            }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    {
                        "input",
                        new List<object>
                        {
                            new List<object>
                            {
                                new Dictionary<string, object?> { { "file", null }, },
                            },
                        }
                    },
                },
            });

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.input.0.0.file\"] }"), "map" },
                { new StringContent("abc"), "1", "foo.bar" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Upload_Too_Large_File_Test()
    {
        // arrange
        var server = CreateStarWarsServer();

        var query = @"
                query ($upload: Upload!) {
                    singleUpload(file: $upload)
                }";

        var request = JsonConvert.SerializeObject(
            new ClientQueryRequest
            {
                Query = query,
                Variables = new Dictionary<string, object?>
                {
                    { "upload", null },
                },
            });

        var count = 1024 * 1024 * 129;
        var buffer = new byte[count];

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = 0xFF;
        }

        // act
        var form = new MultipartFormDataContent
            {
                { new StringContent(request), "operations" },
                { new StringContent("{ \"1\": [\"variables.upload\"] }"), "map" },
                { new ByteArrayContent(buffer), "1", "foo.bar" },
            };

        form.Headers.Add(HttpHeaderKeys.Preflight, "1");

        var result = await server.PostMultipartAsync(form, path: "/upload");

        // assert
        result.MatchSnapshot();
    }
}
