using System;
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

        [Fact]
        public void NamedTypeFromNonNullList()
        {
            // arrange
            var namedType = new NamedTypeNode(null, new NameNode("Foo"));
            var listType = new ListTypeNode(null,
                new NonNullTypeNode(null, namedType));
            var nonNullType = new NonNullTypeNode(null, listType);

            // act
            NamedTypeNode retrievedNamedType = nonNullType.NamedType();

            // assert
            Assert.Equal(namedType, retrievedNamedType);
        }

        [Fact]
        public void InvalidTypeStructure()
        {
            // arrange
            var namedType = new NamedTypeNode(null, new NameNode("Foo"));
            var listType = new ListTypeNode(null, new ListTypeNode(null,
                new NonNullTypeNode(null, namedType)));
            var nonNullType = new NonNullTypeNode(null, listType);

            // act
            Action a = () => nonNullType.NamedType();

            // assert
            Assert.Throws<NotSupportedException>(a);
        }
    }
}
