using System.Text;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class FileUploadTests : IDisposable
{
    private TestServer? _server;

    [Fact]
    public async Task Upload_Single_File()
    {
        // arrange
        _server = await CreateServerAsync();
        using var client = GraphQLHttpClient.Create(_server.CreateClient());

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

        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true
        };

        // act
        using var result = await client.SendAsync(request);

        // assert
        var body = await result.ReadAsResultAsync();
        body.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Upload_Single_File_In_Input_Object()
    {
        // arrange
        _server = await CreateServerAsync();
        using var client = GraphQLHttpClient.Create(_server.CreateClient());

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

        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true
        };

        // act
        using var result = await client.SendAsync(request);

        // assert
        var body = await result.ReadAsResultAsync();
        body.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Upload_Single_File_In_Input_Object_Inline()
    {
        // arrange
        _server = await CreateServerAsync();
        using var client = GraphQLHttpClient.Create(_server.CreateClient());

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

        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true
        };

        // act
        using var result = await client.SendAsync(request);

        // assert
        var body = await result.ReadAsResultAsync();
        body.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Upload_List_Of_Files()
    {
        // arrange
        _server = await CreateServerAsync();
        using var client = GraphQLHttpClient.Create(_server.CreateClient());

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

        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true
        };

        // act
        using var result = await client.SendAsync(request);

        // assert
        var body = await result.ReadAsResultAsync();
        body.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Upload_List_Of_Files_In_Input_Object()
    {
        // arrange
        _server = await CreateServerAsync();
        using var client = GraphQLHttpClient.Create(_server.CreateClient());

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

        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true
        };

        // act
        using var result = await client.SendAsync(request);

        // assert
        var body = await result.ReadAsResultAsync();
        body.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Upload_List_Of_Files_In_Input_Object_Inline()
    {
        // arrange
        _server = await CreateServerAsync();
        using var client = GraphQLHttpClient.Create(_server.CreateClient());

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

        var request = new GraphQLHttpRequest(operation, new Uri("http://localhost/graphql"))
        {
            Method = GraphQLHttpMethod.Post,
            EnableFileUploads = true
        };

        // act
        using var result = await client.SendAsync(request);

        // assert
        var body = await result.ReadAsResultAsync();
        body.MatchMarkdownSnapshot();
    }

    private static async Task<TestServer> CreateServerAsync()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddGraphQL("uploads")
            .AddQueryType<UploadSchema.Query>()
            .AddUploadType()
            .AddSourceSchemaDefaults();

        builder.Services.AddGraphQLGatewayServer()
            .AddInMemorySchema("uploads");

        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();

        return app.GetTestServer();
    }

    public void Dispose()
    {
        _server?.Dispose();
    }

    private static class UploadSchema
    {
        public class Query
        {
            public async Task<FileUploadResult> SingleUpload(IFile file)
                => await ReadFileAsync(file);

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

            private static async Task<FileUploadResult> ReadFileAsync(IFile file)
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
