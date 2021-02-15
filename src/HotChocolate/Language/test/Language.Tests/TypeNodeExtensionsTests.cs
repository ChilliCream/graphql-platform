using System;
using System.Linq;
using HotChocolate.Language.Utilities;
using Xunit;

namespace HotChocolate.Language
{
    public class TypeNodeExtensionsTests
    {
        [Fact]
        public void InnerTypeFromListType()
        {
            // arrange
            var namedType = new NamedTypeNode(null, new NameNode("Foo"));
            var listType = new ListTypeNode(null, namedType);

            // act
            ITypeNode innerType = listType.InnerType();

            // assert
            Assert.Equal(namedType, innerType);
        }

        [Fact]
        public void InnerTypeFromNonNullType()
        {
            // arrange
            var namedType = new NamedTypeNode(null, new NameNode("Foo"));
            var nonNullType = new NonNullTypeNode(null, namedType);

            // act
            ITypeNode innerType = nonNullType.InnerType();

            // assert
            Assert.Equal(namedType, innerType);
        }

        [Fact]
        public void NullableType()
        {
            // arrange
            var namedType = new NamedTypeNode(null, new NameNode("Foo"));
            var nonNullType = new NonNullTypeNode(null, namedType);

            // act
            ITypeNode a = nonNullType.NullableType();
            ITypeNode b = namedType.NullableType();

            // assert
            Assert.Equal(namedType, a);
            Assert.Equal(namedType, b);
        }

        [Fact]
        public void IsListType()
        {
            // arrange
            var namedType = new NamedTypeNode(null, new NameNode("Foo"));
            var listType = new ListTypeNode(null, namedType);

            // act
            bool shouldBeFalse = namedType.IsListType();
            bool shouldBeTrue = listType.IsListType();

            // assert
            Assert.False(shouldBeFalse);
            Assert.True(shouldBeTrue);
        }

        [Fact]
        public void IsNonNullType()
        {
            // arrange
            var namedType = new NamedTypeNode(null, new NameNode("Foo"));
            var nonNullType = new NonNullTypeNode(null, namedType);

            // act
            bool shouldBeFalse = namedType.IsNonNullType();
            bool shouldBeTrue = nonNullType.IsNonNullType();

            // assert
            Assert.False(shouldBeFalse);
            Assert.True(shouldBeTrue);
        }

        [Theory]
        [InlineData("[[Foo!]!]!")]
        [InlineData("[[Foo!]!]")]
        [InlineData("[Foo!]!")]
        [InlineData("[Foo!]")]
        [InlineData("Foo!")]
        [InlineData("Foo")]
        [InlineData("[[Foo]!]!")]
        [InlineData("[[Foo]!]")]
        [InlineData("[Foo]!")]
        [InlineData("[Foo]")]
        [InlineData("[[Foo!]]!")]
        [InlineData("[[Foo!]]")]
        public void NamedType_ExtractType_ExtractSuccessfull(string fieldType)
        {
            // arrange
            ITypeNode type = GetType(fieldType);

            // act
            NamedTypeNode name = type.NamedType();

            // assert
            Assert.Equal("Foo", name.Print());
        }

        [Fact]
        public void InvalidTypeStructure()
        {
            // arrange
            ITypeNode type = GetType("[[[Foo!]!]!]!");

            // act
            Action a = () => type.NamedType();

            // assert
            Assert.Throws<NotSupportedException>(a);
        }

        public ITypeNode GetType(string type)
        {
            DocumentNode definition = Utf8GraphQLParser.Parse($"type Foo {{ field: {type}  }}");

            if (definition.Definitions.FirstOrDefault() is ObjectTypeDefinitionNode typeNode &&
                typeNode.Fields.FirstOrDefault() is { } field)
            {
                return field.Type;
            }

            throw new InvalidOperationException();
        }
    }
}
