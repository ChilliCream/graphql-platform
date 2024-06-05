using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.WebSockets;
using static StrawberryShake.CodeGeneration.CSharp.Integration.UploadScalar.UploadSchemaHelpers;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.UploadScalar;

public class UploadScalarTest : ServerTestBase
{
    public UploadScalarTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_UploadScalar_Argument()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(Configure, out var port);
        var client = CreateClient(host, port);
        using var data = CreateStream("a");

        // act
        var result = await client.TestUpload.ExecuteAsync(
            "foo",
            new Upload(data, "test-file"),
            null,
            null,
            null,
            null,
            null,
            cancellationToken: ct);

        // assert
        Assert.Equal("test-file:a", result.Data!.Upload);
    }

    [Fact]
    public async Task Execute_UploadScalarList_Argument()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(Configure, out var port);
        var client = CreateClient(host, port);
        using var dataA = CreateStream("a");
        using var dataB = CreateStream("b");

        // act
        var result = await client.TestUpload.ExecuteAsync(
            "foo",
            null,
            new Upload?[] { new Upload(dataA, "A"), new Upload(dataB, "B"), },
            null,
            null,
            null,
            null,
            cancellationToken: ct);

        // assert
        Assert.Equal("A:a,B:b", result.Data!.Upload);
    }

    [Fact]
    public async Task Execute_UploadScalarNested_Argument()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(Configure, out var port);
        var client = CreateClient(host, port);
        using var dataA = CreateStream("a");
        using var dataB = CreateStream("b");

        // act
        var result = await client.TestUpload.ExecuteAsync(
            "foo",
            null,
            null,
            new[] { new Upload?[] { new Upload(dataA, "A"), new Upload(dataB, "B"), }, },
            null,
            null,
            null,
            cancellationToken: ct);

        // assert
        Assert.Equal("A:a,B:b", result.Data!.Upload);
    }

    [Fact]
    public async Task Execute_Input_Argument()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(Configure, out var port);
        var client = CreateClient(host, port);
        using var data = CreateStream("a");

        // act
        var result = await client.TestUpload.ExecuteAsync(
            "foo",
            null,
            null,
            null,
            new TestInput()
            {
                Bar = new BarInput()
                {
                    Baz = new BazInput() { File = new Upload(data, "test-file"), },
                },
            },
            null,
            null,
            cancellationToken: ct);

        // assert
        Assert.Equal("test-file:a", result.Data!.Upload);
    }

    [Fact]
    public async Task Execute_InputList_Argument()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(Configure, out var port);
        var client = CreateClient(host, port);
        using var dataA = CreateStream("a");
        using var dataB = CreateStream("b");
        // act
        var result = await client.TestUpload.ExecuteAsync(
            "foo",
            null,
            null,
            null,
            null,
            new[]
            {
                new TestInput()
                {
                    Bar = new BarInput()
                    {
                        Baz = new BazInput() { File = new Upload(dataA, "A"), },
                    },
                },
                new TestInput()
                {
                    Bar = new BarInput()
                    {
                        Baz = new BazInput() { File = new Upload(dataB, "B"), },
                    },
                },
            },
            null,
            cancellationToken: ct);

        // assert
        Assert.Equal("A:a,B:b", result.Data!.Upload);
    }

    [Fact]
    public async Task Execute_InputNested_Argument()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(Configure, out var port);
        var client = CreateClient(host, port);
        using var dataA = CreateStream("a");
        using var dataB = CreateStream("b");

        // act
        var result = await client.TestUpload.ExecuteAsync(
            "foo",
            null,
            null,
            null,
            null,
            null,
            new[]
            {
                new[]
                {
                    new TestInput()
                    {
                        Bar = new BarInput()
                        {
                            Baz = new BazInput() { File = new Upload(dataA, "A"), },
                        },
                    },
                    new TestInput()
                    {
                        Bar = new BarInput()
                        {
                            Baz = new BazInput() { File = new Upload(dataB, "B"), },
                        },
                    },
                },
            },
            cancellationToken: ct);

        // assert
        Assert.Equal("A:a,B:b", result.Data!.Upload);
    }

    public static UploadScalarClient CreateClient(IWebHost host, int port)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddHttpClient(
            UploadScalarClient.ClientName,
            c =>
            {
                c.BaseAddress = new Uri("http://localhost:" + port + "/graphql");
                c.DefaultRequestHeaders.Add("GraphQL-Preflight", "1");
            });
        serviceCollection.AddWebSocketClient(
            UploadScalarClient.ClientName,
            c => c.Uri = new Uri("ws://localhost:" + port + "/graphql"));
        serviceCollection.AddUploadScalarClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        return services.GetRequiredService<UploadScalarClient>();
    }

    [Fact]
    public async Task Execute_ListWorksWithNull()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        using var host = TestServerHelper.CreateServer(Configure, out var port);
        var client = CreateClient(host, port);
        using var dataA = CreateStream("a");
        using var dataB = CreateStream("b");

        // act
        var result = await client.TestUpload.ExecuteAsync(
            "foo",
            null,
            new Upload?[] { new Upload(dataA, "A"), null, new Upload(dataB, "B"), },
            null,
            null,
            null,
            null,
            cancellationToken: ct);

        // assert
        Assert.Equal("A:a,null,B:b", result.Data!.Upload);
    }
}
