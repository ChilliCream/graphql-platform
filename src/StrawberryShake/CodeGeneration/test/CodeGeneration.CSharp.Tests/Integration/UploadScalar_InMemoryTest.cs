using static StrawberryShake.CodeGeneration.CSharp.Integration.UploadScalar.UploadSchemaHelpers;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.UploadScalar_InMemory;

public class UploadScalarInMemoryTest : ServerTestBase
{
    public UploadScalarInMemoryTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_UploadScalar_Argument()
    {
        // arrange
        var client = CreateClient();
        using var data = CreateStream("a");

        // act
        var result = await client.TestUpload.ExecuteAsync(
            "foo",
            new Upload(data, "test-file"),
            null,
            null,
            null,
            null,
            null);

        // assert
        Assert.Equal("test-file:a", result.Data!.Upload);
    }

    [Fact]
    public async Task Execute_UploadScalarList_Argument()
    {
        // arrange
        var client = CreateClient();
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
            null);

        // assert
        Assert.Equal("A:a,B:b", result.Data!.Upload);
    }

    [Fact]
    public async Task Execute_UploadScalarNested_Argument()
    {
        // arrange
        var client = CreateClient();
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
            null);

        // assert
        Assert.Equal("A:a,B:b", result.Data!.Upload);
    }

    [Fact]
    public async Task Execute_Input_Argument()
    {
        // arrange
        var client = CreateClient();
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
            null);

        // assert
        Assert.Equal("test-file:a", result.Data!.Upload);
    }

    [Fact]
    public async Task Execute_InputList_Argument()
    {
        // arrange
        var client = CreateClient();
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
            null);

        // assert
        Assert.Equal("A:a,B:b", result.Data!.Upload);
    }

    [Fact]
    public async Task Execute_InputNested_Argument()
    {
        // arrange
        var client = CreateClient();
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
            });

        // assert
        Assert.Equal("A:a,B:b", result.Data!.Upload);
    }

    public static UploadScalar_InMemoryClient CreateClient()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddUploadScalar_InMemoryClient().ConfigureInMemoryClient();
        var builder = serviceCollection.AddGraphQLServer().AddQueryType();
        Configure(builder);
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        return services.GetRequiredService<UploadScalar_InMemoryClient>();
    }
}
