using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.DependencyInjection;

public class RequestExecutorBuilderExtensionsIdSerializerTests
{
    [Fact]
    public void AddIdSerializer_Include_Schema()
    {
        // arrange
        var serializer =
            new ServiceCollection()
                .TryAddIdSerializer()
                .AddIdSerializer(true)
                .BuildServiceProvider()
                .GetRequiredService<IIdSerializer>();

        // act
        var serializedId = serializer.Serialize("abc", "def", "ghi");

        // assert
        var id = serializer.Deserialize(serializedId!);
        Assert.Equal("abc", id.SchemaName);
        Assert.Equal("def", id.TypeName);
        Assert.Equal("ghi", id.Value);
    }

    [Fact]
    public void AddIdSerializer_Exclude_Schema()
    {
        // arrange
        var serializer =
            new ServiceCollection()
                .TryAddIdSerializer()
                .AddIdSerializer(false)
                .BuildServiceProvider()
                .GetRequiredService<IIdSerializer>();

        // act
        var serializedId = serializer.Serialize("abc", "def", "ghi");

        // assert
        var id = serializer.Deserialize(serializedId!);
        Assert.Null(id.SchemaName);
        Assert.Equal("def", id.TypeName);
        Assert.Equal("ghi", id.Value);
    }

    [Fact]
    public void AddIdSerializer_Include_Schema_Services_Is_Null()
    {
        // arrange
        // act
        void Fail() => default(IServiceCollection)!.AddIdSerializer(true);

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void RequestBuilder_AddIdSerializer_Include_Schema()
    {
        // arrange
        var serializer =
            new ServiceCollection()
                .AddGraphQL()
                .AddIdSerializer(true)
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IIdSerializer>();

        // act
        var serializedId = serializer.Serialize("abc", "def", "ghi");

        // assert
        var id = serializer.Deserialize(serializedId!);
        Assert.Equal("abc", id.SchemaName);
        Assert.Equal("def", id.TypeName);
        Assert.Equal("ghi", id.Value);
    }

    [Fact]
    public void RequestBuilder_AddIdSerializer_Exclude_Schema()
    {
        // arrange
        var serializer =
            new ServiceCollection()
                .AddGraphQL()
                .AddIdSerializer(false)
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IIdSerializer>();

        // act
        var serializedId = serializer.Serialize("abc", "def", "ghi");

        // assert
        var id = serializer.Deserialize(serializedId!);
        Assert.Null(id.SchemaName);
        Assert.Equal("def", id.TypeName);
        Assert.Equal("ghi", id.Value);
    }

    [Fact]
    public void RequestBuilder_AddIdSerializer_Include_Schema_Services_Is_Null()
    {
        // arrange
        // act
        void Fail() => default(IRequestExecutorBuilder)!.AddIdSerializer(true);

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddIdSerializer_Custom_Serializer()
    {
        // arrange
        var serializer =
            new ServiceCollection()
                .TryAddIdSerializer()
                .AddIdSerializer<MockSerializer>()
                .BuildServiceProvider()
                .GetRequiredService<IIdSerializer>();

        // act
        var serializedId = serializer.Serialize("abc", "def", "ghi");

        // assert
        Assert.Equal("mock", serializedId);
    }

    [Fact]
    public void AddIdSerializer_Custom_Serializer_Services_Is_Null()
    {
        // arrange
        // act
        void Fail() => default(IServiceCollection)!.AddIdSerializer<MockSerializer>();

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void RequestBuilder_AddIdSerializer_Custom_Serializer()
    {
        // arrange
        var serializer =
            new ServiceCollection()
                .AddGraphQL()
                .AddIdSerializer<MockSerializer>()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IIdSerializer>();

        // act
        var serializedId = serializer.Serialize("abc", "def", "ghi");

        // assert
        Assert.Equal("mock", serializedId);
    }

    [Fact]
    public void RequestBuilder_AddIdSerializer_Custom_Serializer_Services_Is_Null()
    {
        // arrange
        // act
        void Fail() => default(IRequestExecutorBuilder)!.AddIdSerializer<MockSerializer>();

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddIdSerializer_Custom_Serializer_With_Factory()
    {
        // arrange
        var serializer =
            new ServiceCollection()
                .TryAddIdSerializer()
                .AddIdSerializer(_ => new MockSerializer())
                .BuildServiceProvider()
                .GetRequiredService<IIdSerializer>();

        // act
        var serializedId = serializer.Serialize("abc", "def", "ghi");

        // assert
        Assert.Equal("mock", serializedId);
    }

    [Fact]
    public void AddIdSerializer_Custom_Serializer_With_Factory_Services_Is_Null()
    {
        // arrange
        // act
        void Fail() => default(IServiceCollection)!.AddIdSerializer(_ => new MockSerializer());

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void AddIdSerializer_Custom_Serializer_With_Factory_Factory_Is_Null()
    {
        // arrange
        // act
        void Fail() => new ServiceCollection().AddIdSerializer(
            default(Func<IServiceProvider, IIdSerializer>)!);

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void RequestBuilder_AddIdSerializer_Custom_Serializer_With_Factory()
    {
        // arrange
        var serializer =
            new ServiceCollection()
                .AddGraphQL()
                .AddIdSerializer(_ => new MockSerializer())
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IIdSerializer>();

        // act
        var serializedId = serializer.Serialize("abc", "def", "ghi");

        // assert
        Assert.Equal("mock", serializedId);
    }

    [Fact]
    public void RequestBuilder_AddIdSerializer_Custom_Serializer_With_Fac_Services_Is_Null()
    {
        // arrange
        // act
        void Fail() => default(IRequestExecutorBuilder)!.AddIdSerializer(_ => new MockSerializer());

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void RequestBuilder_AddIdSerializer_Custom_Serializer_With_Fact_Factory_Is_Null()
    {
        // arrange
        // act
        void Fail() => new DefaultRequestExecutorBuilder(new ServiceCollection(), "Foo")
            .AddIdSerializer(default(Func<IServiceProvider, IIdSerializer>)!);

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    private sealed class MockSerializer : IIdSerializer
    {
        public string Serialize<T>(string schemaName, string typeName, T id)
        {
            return "mock";
        }

        public IdValue Deserialize(string serializedId)
        {
            return new IdValue(null, "abc", "mock");
        }
    }
}
