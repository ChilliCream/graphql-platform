using static StrawberryShake.CodeGeneration.CSharp.Integration.UploadScalar.UploadSchemaHelpers;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.UploadScalar_InMemory;

public class UploadScalarInMemoryTest : ServerTestBase
{
    public UploadScalarInMemoryTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Theory]
    [InlineData(null, "test-file:a")]
    [InlineData("application/pdf", "[test-file:a|test-file|application/pdf]")]
    public async Task Execute_UploadScalar_Argument(string? contentType, string expectedResponse)
    {
        // arrange
        var client = CreateClient();
        using var data = CreateStream("a");

        // act
        var result = await client.TestUpload.ExecuteAsync(
            "foo",
            new Upload(data, "test-file", contentType),
            null,
            null,
            null,
            null,
            null,
            contentType != null);

        // assert
        Assert.Equal(expectedResponse, result.Data!.Upload);
    }

    [Theory]
    [InlineData(null, "A:a,B:b")]
    [InlineData("application/pdf", "[A:a|A|application/pdf],[B:b|B|application/pdf]")]
    public async Task Execute_UploadScalarList_Argument(string? contentType, string expectedResponse)
    {
        // arrange
        var client = CreateClient();
        using var dataA = CreateStream("a");
        using var dataB = CreateStream("b");

        // act
        var result = await client.TestUpload.ExecuteAsync(
            "foo",
            null,
            new Upload?[] { new Upload(dataA, "A", contentType), new Upload(dataB, "B", contentType) },
            null,
            null,
            null,
            null,
            contentType != null);

        // assert
        Assert.Equal(expectedResponse, result.Data!.Upload);
    }

    [Theory]
    [InlineData(null, "A:a,B:b")]
    [InlineData("application/pdf", "[A:a|A|application/pdf],[B:b|B|application/pdf]")]
    public async Task Execute_UploadScalarNested_Argument(string? contentType, string expectedResponse)
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
            new[] { new Upload?[] { new Upload(dataA, "A", contentType), new Upload(dataB, "B", contentType) } },
            null,
            null,
            null,
            contentType != null);

        // assert
        Assert.Equal(expectedResponse, result.Data!.Upload);
    }

    [Theory]
    [InlineData(null, "test-file:a")]
    [InlineData("application/pdf", "[test-file:a|test-file|application/pdf]")]
    public async Task Execute_Input_Argument(string? contentType, string expectedResponse)
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
                    Baz = new BazInput() { File = new Upload(data, "test-file", contentType) }
                }
            },
            null,
            null,
            contentType != null);

        // assert
        Assert.Equal(expectedResponse, result.Data!.Upload);
    }

    [Theory]
    [InlineData(null, "A:a,B:b")]
    [InlineData("application/pdf", "[A:a|A|application/pdf],[B:b|B|application/pdf]")]
    public async Task Execute_InputList_Argument(string? contentType, string expectedResponse)
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
                        Baz = new BazInput() { File = new Upload(dataA, "A", contentType) }
                    }
                },
                new TestInput()
                {
                    Bar = new BarInput()
                    {
                        Baz = new BazInput() { File = new Upload(dataB, "B", contentType) }
                    }
                }
            },
            null,
            contentType != null);

        // assert
        Assert.Equal(expectedResponse, result.Data!.Upload);
    }

    [Theory]
    [InlineData(null, "A:a,B:b")]
    [InlineData("application/pdf", "[A:a|A|application/pdf],[B:b|B|application/pdf]")]
    public async Task Execute_InputNested_Argument(string? contentType, string expectedResponse)
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
                            Baz = new BazInput() { File = new Upload(dataA, "A", contentType) }
                        }
                    },
                    new TestInput()
                    {
                        Bar = new BarInput()
                        {
                            Baz = new BazInput() { File = new Upload(dataB, "B", contentType) }
                        }
                    }
                }
            },
            contentType != null);

        // assert
        Assert.Equal(expectedResponse, result.Data!.Upload);
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
