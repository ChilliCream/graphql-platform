using System;
using StrawberryShake.Serializers;
using Xunit;

namespace StrawberryShake
{
    public class ValueSerializerResolverTests
    {
        [Fact]
        public void Resolve_Serializer()
        {
            // arrange
            var resolver = new ValueSerializerResolver(ValueSerializers.All);

            // act
            IValueSerializer serializer = resolver.GetValueSerializer("String");

            // assert
            Assert.NotNull(serializer);
            Assert.Equal("String", serializer.Name);
        }

        [Fact]
        public void Resolve_Serializer_Not_Found()
        {
            // arrange
            var resolver = new ValueSerializerResolver(ValueSerializers.All);

            // act
            Action action = () => resolver.GetValueSerializer("Foo");

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void Resolve_TypeName_Is_Null()
        {
            // arrange
            var resolver = new ValueSerializerResolver(ValueSerializers.All);

            // act
            Action action = () => resolver.GetValueSerializer(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Constructor_Resolvers_Are_null()
        {
            // arrange
            // act
            Action action = () => new ValueSerializerResolver(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Initialize_Input_Serializers()
        {
            // arrange
            var serializer = new InputSerializer();

            // act
            var resolver = new ValueSerializerResolver(
                new IValueSerializer[] { serializer });

            // assert
            Assert.True(serializer.IsInitialized);
        }

        public class InputSerializer
            : IInputSerializer
        {
            public string Name => "Foo";

            public ValueKind Kind => throw new NotImplementedException();

            public Type ClrType => throw new NotImplementedException();

            public Type SerializationType => throw new NotImplementedException();

            public bool IsInitialized { get; private set; }

            public void Initialize(IValueSerializerResolver serializerResolver)
            {
                IsInitialized = true;
            }

            public object Deserialize(object serialized)
            {
                throw new NotImplementedException();
            }

            public object Serialize(object value)
            {
                throw new NotImplementedException();
            }
        }
    }
}
