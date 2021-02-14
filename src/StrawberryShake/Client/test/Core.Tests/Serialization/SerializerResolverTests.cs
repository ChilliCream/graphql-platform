using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace StrawberryShake.Serialization
{
    public class SerializerResolverTests
    {
        [Fact]
        public void Constructor_AllArgs_NotThrow()
        {
            // arrange
            IEnumerable<ISerializer> serializers = Enumerable.Empty<ISerializer>();

            // act
            Exception? exception = Record.Exception(() => new SerializerResolver(serializers));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_NoSerializer_ThrowException()
        {
            // arrange
            IEnumerable<ISerializer> serializers = default!;

            // act
            Exception? exception = Record.Exception(() => new SerializerResolver(serializers));

            // assert
            Assert.IsType<ArgumentNullException>(exception);
        }

        [Fact]
        public void ServiceProvider_SerializerRegistered_NotThrow()
        {
            // arrange
            ServiceProvider? serviceProvider = new ServiceCollection()
                .AddSingleton<ISerializerResolver, SerializerResolver>()
                .AddSingleton<ISerializer, StringSerializer>()
                .BuildServiceProvider();

            // act
            Exception? exception =
                Record.Exception(() => serviceProvider.GetService<ISerializerResolver>());

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public void Constructor_SerializerRegistered_RegisterSerializers()
        {
            // arrange
            var serializerMock = new Mock<ISerializer>(MockBehavior.Strict);
            IEnumerable<ISerializer> serializers =
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
            IEnumerable<ISerializer> serializers =
                Enumerable.Empty<ISerializer>()
                    .Append(serializerMock.Object);
            serializerMock.Setup(x => x.TypeName).Returns("Foo");
            serializerMock
                .Setup(x => x.Initialize(It.IsAny<ISerializerResolver>()))
                .Callback((ISerializerResolver resolver) => callback = resolver);

            // act
            var resolver =new SerializerResolver(serializers);

            // assert
            serializerMock.VerifyAll();
            Assert.Equal(resolver, callback);
        }

        [Fact]
        public void Constructor_CustomIntSerializerRegistered_PreferOverBuiltInt()
        {
            // arrange
            ISerializer[] serializers =
            {
                new IntSerializer(),
                new CustomIntSerializer()
            };
            var resolver =new SerializerResolver(serializers);

            // act
            IInputValueFormatter resolvedFormatter = resolver.GetInputValueFormatter("Int");

            // assert
            Assert.IsType<CustomIntSerializer>(resolvedFormatter);
        }

        [Fact]
        public void GetLeaveValueParser_SerializerRegistered_ReturnSerializer()
        {
            // arrange
            ISerializer[] serializers =
            {
                new IntSerializer()
            };
            var resolver =new SerializerResolver(serializers);

            // act
            ILeafValueParser<int, int>? resolvedParser = resolver.GetLeafValueParser<int, int>("Int");

            // assert
            Assert.IsType<IntSerializer>(resolvedParser);
        }

        [Fact]
        public void GetLeaveValueParser_SerializerRegisteredDifferentName_ThrowException()
        {
            // arrange
            ISerializer[] serializers =
            {
                new IntSerializer()
            };
            var resolver =new SerializerResolver(serializers);

            // act
            Exception? ex = Record.Exception(() => resolver.GetLeafValueParser<int, int>("Foo"));

            // assert
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public void GetLeaveValueParser_SerializerRegisteredDifferentType_ThrowException()
        {
            // arrange
            ISerializer[] serializers =
            {
                new IntSerializer()
            };
            var resolver =new SerializerResolver(serializers);

            // act
            Exception? ex = Record.Exception(() => resolver.GetLeafValueParser<int, double>("Int"));

            // assert
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public void GetLeaveValueParser_TypeNull_ThrowException()
        {
            // arrange
            ISerializer[] serializers =
            {
                new IntSerializer()
            };
            var resolver =new SerializerResolver(serializers);

            // act
            Exception? ex = Record.Exception(() => resolver.GetLeafValueParser<int, double>(null!));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }


        [Fact]
        public void GetInputValueFormatter_FormatterRegistered_ReturnFormatter()
        {
            // arrange
            ISerializer[] serializers =
            {
                new CustomInputValueFormatter()
            };

            var resolver = new SerializerResolver(serializers);

            // act
            IInputValueFormatter resolvedFormatter = resolver.GetInputValueFormatter("Foo");

            // assert
            Assert.IsType<CustomInputValueFormatter>(resolvedFormatter);
        }

        [Fact]
        public void GetInputValueFormatter_FormatterRegisteredDifferentName_ThrowException()
        {
            // arrange
            ISerializer[] serializers =
            {
                new CustomInputValueFormatter()
            };
            var resolver =new SerializerResolver(serializers);

            // act
            Exception? ex = Record.Exception(() => resolver.GetInputValueFormatter("Bar"));

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
            {
                serializerMock.Object
            };

            var resolver =new SerializerResolver(serializers);

            // act
            Exception? ex = Record.Exception(() => resolver.GetInputValueFormatter("Int"));

            // assert
            Assert.IsType<ArgumentException>(ex);
        }

        [Fact]
        public void GetInputValueFormatter_TypeNull_ThrowException()
        {
            // arrange
            ISerializer[] serializers =
            {
                new IntSerializer()
            };
            var resolver =new SerializerResolver(serializers);

            // act
            Exception? ex = Record.Exception(() => resolver.GetInputValueFormatter(null!));

            // assert
            Assert.IsType<ArgumentNullException>(ex);
        }

        private class CustomIntSerializer : ScalarSerializer<int>
        {
            public CustomIntSerializer() : base("Int")
            {
            }
        }

        private class CustomInputValueFormatter : IInputValueFormatter
        {
            public string TypeName => "Foo";

            public object? Format(object? runtimeValue)
            {
                throw new NotImplementedException();
            }
        }
    }
}
