using System.Net;
using System.Text;
using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class FileUploadTests : FusionTestBase
{
    [Fact]
    public async Task Upload_Single_File()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var stream = new MemoryStream("abc"u8.ToArray());

        var operation = new OperationRequest(
            """
            query ($file: Upload!) {
              singleUpload(file: $file) {
                fileName
                contentType
                content
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["file"] = new FileReference(() => stream, "test.txt", "text/plain")
            });

        RawRequest? rawRequest = null;
        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost:5000/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true,
            OnMessageCreated = (_, request, _) => rawRequest = GetRawRequest(request)
        };

        // act
        var result = await client.SendAsync(request);

        // assert
        await MatchSnapshotAsync(gateway, operation, result, rawRequest: rawRequest);
    }

    [Fact]
    public async Task Upload_Single_File_In_Input_Object()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var stream = new MemoryStream("abc"u8.ToArray());

        var operation = new OperationRequest(
            """
            query ($input: FileInput!) {
              singleUploadWithInput(input: $input) {
                fileName
                contentType
                content
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["input"] = new Dictionary<string, object?>
                {
                    ["file"] = new FileReference(() => stream, "test.txt", "text/plain")
                }
            });

        RawRequest? rawRequest = null;
        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost:5000/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true,
            OnMessageCreated = (_, request, _) => rawRequest = GetRawRequest(request)
        };

        // act
        var result = await client.SendAsync(request);

        // assert
        await MatchSnapshotAsync(gateway, operation, result, rawRequest: rawRequest);
    }

    [Fact]
    public async Task Upload_Single_File_In_Input_Object_Inline()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var stream = new MemoryStream("abc"u8.ToArray());

        var operation = new OperationRequest(
            """
            query ($file: Upload!) {
              singleUploadWithInput(input: { file: $file }) {
                fileName
                contentType
                content
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["file"] = new FileReference(() => stream, "test.txt", "text/plain")
            });

        RawRequest? rawRequest = null;
        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost:5000/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true,
            OnMessageCreated = (_, request, _) => rawRequest = GetRawRequest(request)
        };

        // act
        var result = await client.SendAsync(request);

        // assert
        await MatchSnapshotAsync(gateway, operation, result, rawRequest: rawRequest);
    }

    [Fact]
    public async Task Upload_List_Of_Files()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var stream1 = new MemoryStream("abc"u8.ToArray());
        var stream2 = new MemoryStream("def"u8.ToArray());

        var operation = new OperationRequest(
            """
            query ($files: [Upload!]!) {
              multiUpload(files: $files) {
                fileName
                contentType
                content
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["files"] = new List<object>
                {
                    new FileReference(() => stream1, "test.txt", "text/plain"),
                    new FileReference(() => stream2, "test2.pdf", "application/pdf")
                }
            });

        RawRequest? rawRequest = null;
        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost:5000/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true,
            OnMessageCreated = (_, request, _) => rawRequest = GetRawRequest(request)
        };

        // act
        var result = await client.SendAsync(request);

        // assert
        await MatchSnapshotAsync(gateway, operation, result, rawRequest: rawRequest);
    }

    [Fact]
    public async Task Upload_List_Of_Files_In_Input_Object()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var stream1 = new MemoryStream("abc"u8.ToArray());
        var stream2 = new MemoryStream("abc"u8.ToArray());

        var operation = new OperationRequest(
            """
            query ($input: FilesInput!) {
              multiUploadWithInput(input: $input) {
                fileName
                contentType
                content
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["input"] = new Dictionary<string, object?>
                {
                    ["files"] = new List<object>
                    {
                        new FileReference(() => stream1, "test.txt", "text/plain"),
                        new FileReference(() => stream2, "test2.pdf", "application/pdf")
                    }
                }
            });

        RawRequest? rawRequest = null;
        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost:5000/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true,
            OnMessageCreated = (_, request, _) => rawRequest = GetRawRequest(request)
        };

        // act
        var result = await client.SendAsync(request);

        // assert
        await MatchSnapshotAsync(gateway, operation, result, rawRequest: rawRequest);
    }

    [Fact]
    public async Task Upload_List_Of_Files_In_Input_Object_Inline()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var stream1 = new MemoryStream("abc"u8.ToArray());
        var stream2 = new MemoryStream("def"u8.ToArray());

        var operation = new OperationRequest(
            """
            query ($files: [Upload!]!) {
              multiUploadWithInput(input: { files: $files }) {
                fileName
                contentType
                content
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["files"] = new List<object>
                {
                    new FileReference(() => stream1, "test.txt", "text/plain"),
                    new FileReference(() => stream2, "test2.pdf", "application/pdf")
                }
            });

        RawRequest? rawRequest = null;
        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost:5000/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true,
            OnMessageCreated = (_, request, _) => rawRequest = GetRawRequest(request)
        };

        // act
        var result = await client.SendAsync(request);

        // assert
        await MatchSnapshotAsync(gateway, operation, result, rawRequest: rawRequest);
    }

    [Fact]
    public async Task Upload_Nullable_File_Not_Provided()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var operation = new OperationRequest(
            """
            query ($file: Upload) {
              nullableUpload(file: $file) {
                fileName
                contentType
                content
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["file"] = null
            });

        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost:5000/graphql"))
        {
            Method = GraphQLHttpMethod.Post
        };

        // act
        var result = await client.SendAsync(request);

        // assert
        await MatchSnapshotAsync(gateway, operation, result);
    }

    [Fact]
    public async Task Upload_Nullable_File()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var stream = new MemoryStream("abc"u8.ToArray());

        var operation = new OperationRequest(
            """
            query ($file: Upload) {
              nullableUpload(file: $file) {
                fileName
                contentType
                content
              }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["file"] = new FileReference(() => stream, "test.txt", "text/plain")
            });

        RawRequest? rawRequest = null;
        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost:5000/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true,
            OnMessageCreated = (_, request, _) => rawRequest = GetRawRequest(request)
        };

        // act
        var result = await client.SendAsync(request);

        // assert
        await MatchSnapshotAsync(gateway, operation, result, rawRequest: rawRequest);
    }

    [Fact]
    public async Task Upload_File_In_Input_List_With_NonNull_Placeholder_Should_Bind_File()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: b => b.ModifyServerOptions(
                o => o.EnforceNullVariableValuesForMultipartFileUpload = false));

        const string operations =
            """
            {"query":"query ($input: FilesInput!) { multiUploadWithInput(input: $input) { fileName contentType content } }","variables":{"input":{"files":[{"path":"./test.txt","relativePath":"./test.txt","preview":"blob:http://localhost/abc"}]}}}
            """;

        const string map =
            """
            {"variables.input.files.0":["variables.input.files.0"]}
            """;

        using var form = new MultipartFormDataContent
        {
            { new StringContent(operations), "operations" },
            { new StringContent(map), "map" },
            { new ByteArrayContent("abc"u8.ToArray()), "variables.input.files.0", "test.txt" }
        };

        form.Headers.Add("GraphQL-Preflight", "1");

        using var client = gateway.CreateClient();

        // act
        using var response = await client.PostAsync("graphql", form);
        var content = await response.Content.ReadAsStringAsync();

        // assert
        // The file must be bound from the multipart part instead of being rejected because the
        // placeholder object is not a valid file value.
        Assert.DoesNotContain("The value is not a valid file.", content);
        Assert.Contains("test.txt", content);
        Assert.Contains("abc", content);
    }

    [Fact]
    public async Task Upload_File_In_Input_List_With_NonNull_Placeholder_Should_Throw_When_Enforced()
    {
        // arrange
        // Same non-conformant request as above, but with the default strict spec behavior the
        // non-null placeholder at the file location must be rejected with an explicit error.
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        const string operations =
            """
            {"query":"query ($input: FilesInput!) { multiUploadWithInput(input: $input) { fileName contentType content } }","variables":{"input":{"files":[{"path":"./test.txt","relativePath":"./test.txt","preview":"blob:http://localhost/abc"}]}}}
            """;

        const string map =
            """
            {"variables.input.files.0":["variables.input.files.0"]}
            """;

        using var form = new MultipartFormDataContent
        {
            { new StringContent(operations), "operations" },
            { new StringContent(map), "map" },
            { new ByteArrayContent("abc"u8.ToArray()), "variables.input.files.0", "test.txt" }
        };

        form.Headers.Add("GraphQL-Preflight", "1");

        using var client = gateway.CreateClient();

        // act
        using var response = await client.PostAsync("graphql", form);
        var content = await response.Content.ReadAsStringAsync();

        // assert
        // The request is rejected because a non-null value sits at the file's variable location:
        // the file is not bound, the operation does not execute, and an explicit error is returned.
        using var document = JsonDocument.Parse(content);
        var error = document.RootElement.GetProperty("errors")[0];

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "The file mapped to 'variables.input.files.0' must be referenced by a null value "
            + "at its variable location, but a non-null value was provided.",
            error.GetProperty("message").GetString());
        Assert.Equal("HC0078", error.GetProperty("extensions").GetProperty("code").GetString());
        Assert.DoesNotContain("multiUploadWithInput", content);
    }

    [Fact]
    public async Task Upload_File_In_Input_Object_With_NonNull_Placeholder_Should_Bind_File()
    {
        // arrange
        // A file mapped to an object property (not a list element) with a non-null placeholder
        // exercises the object-property rewrite branch in lenient mode.
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1)
            ],
            configureGatewayBuilder: b => b.ModifyServerOptions(
                o => o.EnforceNullVariableValuesForMultipartFileUpload = false));

        const string operations =
            """
            {"query":"query ($input: FileInput!) { singleUploadWithInput(input: $input) { fileName contentType content } }","variables":{"input":{"file":{"path":"./test.txt","relativePath":"./test.txt","preview":"blob:http://localhost/abc"}}}}
            """;

        const string map =
            """
            {"variables.input.file":["variables.input.file"]}
            """;

        using var form = new MultipartFormDataContent
        {
            { new StringContent(operations), "operations" },
            { new StringContent(map), "map" },
            { new ByteArrayContent("abc"u8.ToArray()), "variables.input.file", "test.txt" }
        };

        form.Headers.Add("GraphQL-Preflight", "1");

        using var client = gateway.CreateClient();

        // act
        using var response = await client.PostAsync("graphql", form);
        var content = await response.Content.ReadAsStringAsync();

        // assert
        // The file must be bound from the multipart part instead of being rejected because the
        // placeholder object is not a valid file value.
        Assert.DoesNotContain("The value is not a valid file.", content);
        Assert.Contains("test.txt", content);
        Assert.Contains("abc", content);
    }

    [Fact]
    public async Task Upload_File_In_Input_Object_With_NonNull_Placeholder_Should_Throw_When_Enforced()
    {
        // arrange
        // Same object-property request, but with the default strict spec behavior the non-null
        // placeholder at the file location must be rejected with an explicit error.
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<SourceSchema1.Query>().AddUploadType());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        const string operations =
            """
            {"query":"query ($input: FileInput!) { singleUploadWithInput(input: $input) { fileName contentType content } }","variables":{"input":{"file":{"path":"./test.txt","relativePath":"./test.txt","preview":"blob:http://localhost/abc"}}}}
            """;

        const string map =
            """
            {"variables.input.file":["variables.input.file"]}
            """;

        using var form = new MultipartFormDataContent
        {
            { new StringContent(operations), "operations" },
            { new StringContent(map), "map" },
            { new ByteArrayContent("abc"u8.ToArray()), "variables.input.file", "test.txt" }
        };

        form.Headers.Add("GraphQL-Preflight", "1");

        using var client = gateway.CreateClient();

        // act
        using var response = await client.PostAsync("graphql", form);
        var content = await response.Content.ReadAsStringAsync();

        // assert
        // The request is rejected because a non-null value sits at the file's variable location:
        // the file is not bound, the operation does not execute, and an explicit error is returned.
        using var document = JsonDocument.Parse(content);
        var error = document.RootElement.GetProperty("errors")[0];

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "The file mapped to 'variables.input.file' must be referenced by a null value "
            + "at its variable location, but a non-null value was provided.",
            error.GetProperty("message").GetString());
        Assert.Equal("HC0078", error.GetProperty("extensions").GetProperty("code").GetString());
        Assert.DoesNotContain("singleUploadWithInput", content);
    }

    private static RawRequest GetRawRequest(HttpRequestMessage requestMessage)
    {
        if (requestMessage.Content is not { } content)
        {
            throw new InvalidOperationException("Expected content to not be null.");
        }

        if (requestMessage.Content.Headers.ContentType is not { } contentType)
        {
            throw new InvalidOperationException("Expected Content-Type header to not be null.");
        }

        var bodyStream = new MemoryStream();
        var originalStream = content.ReadAsStream();

        originalStream.CopyTo(bodyStream);
        bodyStream.Position = 0;

        if (originalStream.CanSeek)
        {
            originalStream.Position = 0;
        }

        return new RawRequest { Body = bodyStream, ContentType = contentType };
    }

    public static class SourceSchema1
    {
        public class Query
        {
            public async Task<FileUploadResult> SingleUpload(IFile file) => await ReadFileAsync(file);

            public async Task<FileUploadResult?> NullableUpload(IFile? file)
            {
                if (file is null)
                {
                    return null;
                }

                return await ReadFileAsync(file);
            }

            public async Task<FileUploadResult> SingleUploadWithInput(FileInput input)
                => await ReadFileAsync(input.File);

            public async IAsyncEnumerable<FileUploadResult> MultiUpload(IFile[] files)
            {
                foreach (var file in files)
                {
                    yield return await ReadFileAsync(file);
                }
            }

            public async IAsyncEnumerable<FileUploadResult> MultiUploadWithInput(FilesInput input)
            {
                foreach (var file in input.Files)
                {
                    yield return await ReadFileAsync(file);
                }
            }

            private async Task<FileUploadResult> ReadFileAsync(IFile file)
            {
                await using var stream = file.OpenReadStream();
                using var sr = new StreamReader(stream, Encoding.UTF8);
                var content = await sr.ReadToEndAsync();

                return new FileUploadResult(file.Name, file.ContentType, content);
            }

            public class FileInput
            {
                public required IFile File { get; init; }
            }

            public class FilesInput
            {
                public required IFile[] Files { get; init; }
            }
        }

        public record FileUploadResult(string FileName, string? ContentType, string Content);
    }
}
