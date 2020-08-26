using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate
{
    public class SchemaTypeResolverTests
    {
        private readonly ITypeInspector _typeInspector = new DefaultTypeInspector();

        private IExtendedType TypeOf<T>() =>
            _typeInspector.GetType(typeof(T));

        [InlineData(TypeContext.Output)]
        [InlineData(TypeContext.None)]
        [Theory]
        public void InferObjectType(TypeContext context)
        {
            // arrange
            ExtendedTypeReference typeReference = TypeReference.Create(TypeOf<Bar>(), context);

            // act
            var success = SchemaTypeResolver.TryInferSchemaType(
                _typeInspector,
                typeReference,
                out ExtendedTypeReference schemaType);

            // assert
            Assert.True(success);
            Assert.Equal(TypeContext.Output, schemaType.Context);
            Assert.Equal(typeof(ObjectType<Bar>), schemaType.Type.Source);
        }

        [InlineData(TypeContext.Output)]
        [InlineData(TypeContext.None)]
        [Theory]
        public void InferInterfaceType(TypeContext context)
        {
            // arrange
            ExtendedTypeReference typeReference = TypeReference.Create(TypeOf<IBar>(), context);

            // act
            var success = SchemaTypeResolver.TryInferSchemaType(
                _typeInspector,
                typeReference,
                out ExtendedTypeReference schemaType);

            // assert
            Assert.True(success);
            Assert.Equal(TypeContext.Output, schemaType.Context);
            Assert.Equal(typeof(InterfaceType<IBar>), schemaType.Type.Source);
        }

        [Fact]
        public void InferInputObjectType()
        {
            // arrange
            ExtendedTypeReference typeReference = TypeReference.Create(TypeOf<Bar>(), TypeContext.Input);

            // act
            var success = SchemaTypeResolver.TryInferSchemaType(
                _typeInspector,
                typeReference,
                out ExtendedTypeReference schemaType);

            // assert
            Assert.True(success);
            Assert.Equal(TypeContext.Input, schemaType.Context);
            Assert.Equal(typeof(InputObjectType<Bar>), schemaType.Type.Source);
        }

        [InlineData(TypeContext.Output)]
        [InlineData(TypeContext.Input)]
        [InlineData(TypeContext.None)]
        [Theory]
        public void InferEnumType(TypeContext context)
        {
            // arrange
            ExtendedTypeReference typeReference = TypeReference.Create(TypeOf<Foo>(), context);

            // act
            var success = SchemaTypeResolver.TryInferSchemaType(
                _typeInspector,
                typeReference,
                out ExtendedTypeReference schemaType);

            // assert
            Assert.True(success);
            Assert.Equal(TypeContext.None, schemaType.Context);
            Assert.Equal(typeof(EnumType<Foo>), schemaType.Type.Source);
        }

        public class Bar
        {
            public string Baz { get; }
        }

        public interface IBar
        {
            string Baz { get; }
        }

        public enum Foo
        {
            Bar,
            Baz
        }
    }
}
