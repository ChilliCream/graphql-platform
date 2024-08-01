using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace StrawberryShake.Serialization;

public class SerializerResolverTests
{
    [Fact]
    public void Constructor_AllArgs_NotThrow()
    {
        // arrange
        var serializers = Enumerable.Empty<ISerializer>();

        // act
        var exception = Record.Exception(() => new SerializerResolver(serializers));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_NoSerializer_ThrowException()
    {
        // arrange
        IEnumerable<ISerializer> serializers = default!;

        // act
        var exception = Record.Exception(() => new SerializerResolver(serializers));

        // assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void ServiceProvider_SerializerRegistered_NotThrow()
    {
        // arrange
        var serviceProvider = new ServiceCollection()
            .AddSingleton<ISerializerResolver, SerializerResolver>()
            .AddSingleton<ISerializer, StringSerializer>()
            .BuildServiceProvider();

        // act
        var exception =
            Record.Exception(() => serviceProvider.GetService<ISerializerResolver>());

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_SerializerRegistered_RegisterSerializers()
    {
        // arrange
        var serializerMock = new Mock<ISerializer>(MockBehavior.Strict);
        var serializers =
            Enumerable.Empty<ISerializer>()
                .Append(serializerMock.Object);
        serializerMock.Setup(x => x.TypeName).Returns("Foo");

        // act
        new SerializerResolver(serializers);

        // assert
        serializerMock.VerifyAll();
    }

    [Fact]
    public void Constructor_InputObjectFormatterRegistered_Initialize()
    {
        // arrange
        var serializerMock = new Mock<IInputObjectFormatter>(MockBehavior.Strict);
        ISerializerResolver? callback = null;
        var serializers =
            Enumerable.Empty<ISerializer>()
                .Append(serializerMock.Object);
        serializerMock.Setup(x => x.TypeName).Returns("Foo");
        serializerMock
            .Setup(x => x.Initialize(It.IsAny<ISerializerResolver>()))
            .Callback((ISerializerResolver resolver) => callback = resolver);

        // act
        var resolver = new SerializerResolver(serializers);

        // assert
        serializerMock.VerifyAll();
        Assert.Equal(resolver, callback);
    }

    [Fact]
    public void Constructor_CustomIntSerializerRegistered_PreferOverBuiltInt()
    {
        // arrange
        ISerializer[] serializers =
        [
            new CustomIntSerializer(),
                new IntSerializer(),
        ];
        var resolver = new SerializerResolver(serializers);

        // act
        var resolvedFormatter = resolver.GetInputValueFormatter("Int");

        // assert
        Assert.IsType<CustomIntSerializer>(resolvedFormatter);
    }

    [Fact]
    public void GetLeaveValueParser_SerializerRegistered_ReturnSerializer()
    {
        // arrange
        ISerializer[] serializers =
        [
            new IntSerializer(),
        ];
        var resolver = new SerializerResolver(serializers);

        // act
        var resolvedParser =
            resolver.GetLeafValueParser<int, int>("Int");

        // assert
        Assert.IsType<IntSerializer>(resolvedParser);
    }

    [Fact]
    public void GetLeaveValueParser_SerializerRegisteredDifferentName_ThrowException()
    {
        // arrange
        ISerializer[] serializers =
        [
            new IntSerializer(),
        ];
        var resolver = new SerializerResolver(serializers);

        // act
        var ex = Record.Exception(() => resolver.GetLeafValueParser<int, int>("Foo"));

        // assert
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public void GetLeaveValueParser_SerializerRegisteredDifferentType_ThrowException()
    {
        // arrange
        ISerializer[] serializers =
        [
            new IntSerializer(),
        ];
        var resolver = new SerializerResolver(serializers);

        // act
        var ex = Record.Exception(() => resolver.GetLeafValueParser<int, double>("Int"));

        // assert
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public void GetLeaveValueParser_TypeNull_ThrowException()
    {
        // arrange
        ISerializer[] serializers =
        [
            new IntSerializer(),
        ];
        var resolver = new SerializerResolver(serializers);

        // act
        var ex = Record.Exception(() => resolver.GetLeafValueParser<int, double>(null!));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void GetInputValueFormatter_FormatterRegistered_ReturnFormatter()
    {
        // arrange
        ISerializer[] serializers =
        [
            new CustomInputValueFormatter(),
        ];

        var resolver = new SerializerResolver(serializers);

        // act
        var resolvedFormatter = resolver.GetInputValueFormatter("Foo");

        // assert
        Assert.IsType<CustomInputValueFormatter>(resolvedFormatter);
    }

    [Fact]
    public void GetInputValueFormatter_FormatterRegisteredDifferentName_ThrowException()
    {
        // arrange
        ISerializer[] serializers =
        [
            new CustomInputValueFormatter(),
        ];
        var resolver = new SerializerResolver(serializers);

        // act
        var ex = Record.Exception(() => resolver.GetInputValueFormatter("Bar"));

        // assert
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public void GetInputValueFormatter_FormatterRegisteredDifferentType_ThrowException()
    {
        // arrange
        var serializerMock = new Mock<ISerializer>();
        serializerMock.Setup(x => x.TypeName).Returns("Int");
        ISerializer[] serializers =
        [
            serializerMock.Object,
        ];

        var resolver = new SerializerResolver(serializers);

        // act
        var ex = Record.Exception(() => resolver.GetInputValueFormatter("Int"));

        // assert
        Assert.IsType<ArgumentException>(ex);
    }

    [Fact]
    public void GetInputValueFormatter_TypeNull_ThrowException()
    {
        // arrange
        ISerializer[] serializers =
        [
            new IntSerializer(),
        ];
        var resolver = new SerializerResolver(serializers);

        // act
        var ex = Record.Exception(() => resolver.GetInputValueFormatter(null!));

        // assert
        Assert.IsType<ArgumentNullException>(ex);
    }

    [Fact]
    public void DependencyInjection_CustomIntSerializerRegistered_ReturnSerializer()
    {
        // arrange
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<SerializerResolver>();
        serviceCollection.AddSerializer<CustomIntSerializer>();

        var resolver =
            serviceCollection.BuildServiceProvider().GetRequiredService<SerializerResolver>();

        // act
        var resolvedFormatter = resolver.GetInputValueFormatter("Int");

        // assert
        Assert.IsType<CustomIntSerializer>(resolvedFormatter);
    }

    [Fact]
    public void DependencyInjection_CustomIntSerializerRegistered_ReturnInstanceOfSerializer()
    {
        // arrange
        CustomIntSerializer serializer = new();
        IServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<SerializerResolver>();
        serviceCollection.AddSerializer(serializer);

        var resolver =
            serviceCollection.BuildServiceProvider().GetRequiredService<SerializerResolver>();

        // act
        var resolvedFormatter = resolver.GetInputValueFormatter("Int");

        // assert
        Assert.Same(serializer, resolvedFormatter);
    }

    private sealed class CustomIntSerializer : ScalarSerializer<int>
    {
        public CustomIntSerializer() : base("Int")
        {
        }
    }

    private sealed class CustomInputValueFormatter : IInputValueFormatter
    {
        public string TypeName => "Foo";

        public object? Format(object? runtimeValue)
        {
            throw new NotImplementedException();
        }
    }
}
