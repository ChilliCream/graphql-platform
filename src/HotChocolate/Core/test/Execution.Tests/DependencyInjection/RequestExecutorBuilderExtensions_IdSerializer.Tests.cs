using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Execution.DependencyInjection
{
    public class RequestExecutorBuilderExtensionsIdSerializerTests
    {
        [Fact]
        public void AddIdSerializer_Include_Schema()
        {
            // arrange
            IIdSerializer serializer =
                new ServiceCollection()
                    .TryAddIdSerializer()
                    .AddIdSerializer(true)
                    .BuildServiceProvider()
                    .GetRequiredService<IIdSerializer>();

            // act
            var serializedId = serializer.Serialize("abc", "def", "ghi");

            // assert
            IdValue id = serializer.Deserialize(serializedId);
            Assert.Equal("abc", id.SchemaName);
            Assert.Equal("def", id.TypeName);
            Assert.Equal("ghi", id.Value);
        }

        [Fact]
        public void AddIdSerializer_Exclude_Schema()
        {
            // arrange
            IIdSerializer serializer =
                new ServiceCollection()
                    .TryAddIdSerializer()
                    .AddIdSerializer(false)
                    .BuildServiceProvider()
                    .GetRequiredService<IIdSerializer>();

            // act
            var serializedId = serializer.Serialize("abc", "def", "ghi");

            // assert
            IdValue id = serializer.Deserialize(serializedId);
            Assert.False(id.SchemaName.HasValue);
            Assert.Equal("def", id.TypeName);
            Assert.Equal("ghi", id.Value);
        }

        [Fact]
        public void AddIdSerializer_Include_Schema_Services_Is_Null()
        {
            // arrange
            // act
            void Fail() => RequestExecutorBuilderExtensions
                .AddIdSerializer(default(IServiceCollection)!, true);

            // assert
            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void RequestBuilder_AddIdSerializer_Include_Schema()
        {
            // arrange
            IIdSerializer serializer =
                new ServiceCollection()
                    .AddGraphQL()
                    .AddIdSerializer(true)
                    .Services
                    .BuildServiceProvider()
                    .GetRequiredService<IIdSerializer>();

            // act
            var serializedId = serializer.Serialize("abc", "def", "ghi");

            // assert
            IdValue id = serializer.Deserialize(serializedId);
            Assert.Equal("abc", id.SchemaName);
            Assert.Equal("def", id.TypeName);
            Assert.Equal("ghi", id.Value);
        }

        [Fact]
        public void RequestBuilder_AddIdSerializer_Exclude_Schema()
        {
            // arrange
            IIdSerializer serializer =
                new ServiceCollection()
                    .AddGraphQL()
                    .AddIdSerializer(false)
                    .Services
                    .BuildServiceProvider()
                    .GetRequiredService<IIdSerializer>();

            // act
            var serializedId = serializer.Serialize("abc", "def", "ghi");

            // assert
            IdValue id = serializer.Deserialize(serializedId);
            Assert.False(id.SchemaName.HasValue);
            Assert.Equal("def", id.TypeName);
            Assert.Equal("ghi", id.Value);
        }

        [Fact]
        public void RequestBuilder_AddIdSerializer_Include_Schema_Services_Is_Null()
        {
            // arrange
            // act
            void Fail() => RequestExecutorBuilderExtensions
                .AddIdSerializer(default(IRequestExecutorBuilder)!, true);

            // assert
            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddIdSerializer_Custom_Serializer()
        {
            // arrange
            IIdSerializer serializer =
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
            void Fail() => RequestExecutorBuilderExtensions
                .AddIdSerializer<MockSerializer>(default(IServiceCollection)!);

            // assert
            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void RequestBuilder_AddIdSerializer_Custom_Serializer()
        {
            // arrange
            IIdSerializer serializer =
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
            void Fail() => RequestExecutorBuilderExtensions
                .AddIdSerializer<MockSerializer>(default(IRequestExecutorBuilder)!);

            // assert
            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddIdSerializer_Custom_Serializer_With_Factory()
        {
            // arrange
            IIdSerializer serializer =
                new ServiceCollection()
                    .TryAddIdSerializer()
                    .AddIdSerializer(s => new MockSerializer())
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
            void Fail() => RequestExecutorBuilderExtensions
                .AddIdSerializer(default(IServiceCollection)!, s => new MockSerializer());

            // assert
            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void AddIdSerializer_Custom_Serializer_With_Factory_Factory_Is_Null()
        {
            // arrange
            // act
            void Fail() => RequestExecutorBuilderExtensions
                .AddIdSerializer(
                        new ServiceCollection(),
                        default(Func<IServiceProvider, IIdSerializer>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void RequestBuilder_AddIdSerializer_Custom_Serializer_With_Factory()
        {
            // arrange
            IIdSerializer serializer =
                new ServiceCollection()
                    .AddGraphQL()
                    .AddIdSerializer(s => new MockSerializer())
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
            void Fail() => RequestExecutorBuilderExtensions
                .AddIdSerializer(default(IRequestExecutorBuilder)!, s => new MockSerializer());

            // assert
            Assert.Throws<ArgumentNullException>(Fail);
        }

        [Fact]
        public void RequestBuilder_AddIdSerializer_Custom_Serializer_With_Fact_Factory_Is_Null()
        {
            // arrange
            // act
            void Fail() => RequestExecutorBuilderExtensions
                .AddIdSerializer(
                    new DefaultRequestExecutorBuilder(new ServiceCollection(), "Foo"),
                    default(Func<IServiceProvider, IIdSerializer>)!);

            // assert
            Assert.Throws<ArgumentNullException>(Fail);
        }

        private class MockSerializer : IIdSerializer
        {
            public string Serialize<T>(NameString schemaName, NameString typeName, T id)
            {
                return "mock";
            }

            public IdValue Deserialize(string serializedId, Type resultType = null)
            {
                return new IdValue(null, null, "mock");
            }
        }
    }
}
