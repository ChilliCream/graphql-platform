using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Xunit;

namespace HotChocolate
{
    public class SchemaTypeResolverTests
    {
        [InlineData(TypeContext.Output)]
        [InlineData(TypeContext.None)]
        [Theory]
        public void InferObjectType(TypeContext context)
        {
            // arrange
            var typeReference = new ClrTypeReference(
                typeof(Bar),
                context);

            // act
            bool success = SchemaTypeResolver.TryInferSchemaType(
                typeReference,
                out IClrTypeReference schemaType);

            // assert
            Assert.True(success);
            Assert.Equal(TypeContext.Output, schemaType.Context);
            Assert.Equal(typeof(ObjectType<Bar>), schemaType.Type);
        }

        [InlineData(TypeContext.Output)]
        [InlineData(TypeContext.None)]
        [Theory]
        public void InferInterfaceType(TypeContext context)
        {
            // arrange
            var typeReference = new ClrTypeReference(
                typeof(IBar),
                context);

            // act
            bool success = SchemaTypeResolver.TryInferSchemaType(
                typeReference,
                out IClrTypeReference schemaType);

            // assert
            Assert.True(success);
            Assert.Equal(TypeContext.Output, schemaType.Context);
            Assert.Equal(typeof(InterfaceType<IBar>), schemaType.Type);
        }

        [Fact]
        public void InferInputObjectType()
        {
            // arrange
            var typeReference = new ClrTypeReference(
                typeof(Bar),
                TypeContext.Input);

            // act
            bool success = SchemaTypeResolver.TryInferSchemaType(
                typeReference,
                out IClrTypeReference schemaType);

            // assert
            Assert.True(success);
            Assert.Equal(TypeContext.Input, schemaType.Context);
            Assert.Equal(typeof(InputObjectType<Bar>), schemaType.Type);
        }

        [InlineData(TypeContext.Output)]
        [InlineData(TypeContext.Input)]
        [InlineData(TypeContext.None)]
        [Theory]
        public void InferEnumType(TypeContext context)
        {
            // arrange
            var typeReference = new ClrTypeReference(
                typeof(Foo),
                context);

            // act
            bool success = SchemaTypeResolver.TryInferSchemaType(
                typeReference,
                out IClrTypeReference schemaType);

            // assert
            Assert.True(success);
            Assert.Equal(context, schemaType.Context);
            Assert.Equal(typeof(EnumType<Foo>), schemaType.Type);
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
