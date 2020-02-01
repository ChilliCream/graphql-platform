using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class SyntaxTypeReferenceTests
    {
        [InlineData("abc", TypeContext.Input, false, null)]
        [InlineData("abc", TypeContext.Input, null, false)]
        [InlineData("abc", TypeContext.Input, true, false)]
        [InlineData("abc", TypeContext.None, true, false)]
        [InlineData("abc", TypeContext.Output, true, false)]
        [InlineData("abc", TypeContext.Output, false, true)]
        [InlineData("abc", TypeContext.Output, null, true)]
        [InlineData("abc", TypeContext.Output, true, null)]
        [InlineData("abc", TypeContext.Output, null, null)]
        [Theory]
        public void SyntaxTypeReference_CreateInstance(
            string typeName,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            // act
            var typeReference = new SyntaxTypeReference(
                new NamedTypeNode(typeName),
                context,
                isTypeNullable,
                isElementTypeNullable);

            // assert
            Assert.Equal(typeName, typeReference.Type.ToString());
            Assert.Equal(context, typeReference.Context);
            Assert.Equal(isTypeNullable, typeReference.IsTypeNullable);
            Assert.Equal(isElementTypeNullable,
                typeReference.IsElementTypeNullable);
        }

        [InlineData("def", TypeContext.Input, false, null)]
        [InlineData("abc", TypeContext.Input, null, false)]
        [InlineData("abc", TypeContext.Input, true, false)]
        [InlineData("abc", TypeContext.None, true, false)]
        [InlineData("abc", TypeContext.Output, true, false)]
        [InlineData("abc", TypeContext.Output, false, true)]
        [InlineData("abc", TypeContext.Output, null, true)]
        [InlineData("abc", TypeContext.Output, true, null)]
        [InlineData("abc", TypeContext.Output, null, null)]
        [Theory]
        public void SyntaxTypeReference_Equals_True(
            string typeName,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode(typeName),
                context,
                isTypeNullable,
                isElementTypeNullable);

            var y = new SyntaxTypeReference(
                new NamedTypeNode(typeName),
                context,
                isTypeNullable,
                isElementTypeNullable);

            // act
            bool result = x.Equals(y);

            // assert
            Assert.True(result);
        }

        [InlineData("def", TypeContext.Input, false, null)]
        [InlineData("abc", TypeContext.Input, null, false)]
        [InlineData("abc", TypeContext.Input, true, false)]
        [InlineData("abc", TypeContext.None, true, false)]
        [InlineData("abc", TypeContext.Output, true, false)]
        [InlineData("abc", TypeContext.Output, false, true)]
        [InlineData("abc", TypeContext.Output, null, true)]
        [InlineData("abc", TypeContext.Output, true, null)]
        [InlineData("abc", TypeContext.Output, null, null)]
        [Theory]
        public void ISyntaxTypeReference_Equals_True(
            string typeName,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode(typeName),
                context,
                isTypeNullable,
                isElementTypeNullable);

            ISyntaxTypeReference y = new SyntaxTypeReference(
                new NamedTypeNode(typeName),
                context,
                isTypeNullable,
                isElementTypeNullable);

            // act
            bool result = x.Equals(y);

            // assert
            Assert.True(result);
        }

        [InlineData("def", TypeContext.Input, false, null)]
        [InlineData("abc", TypeContext.Input, null, false)]
        [InlineData("abc", TypeContext.Input, true, false)]
        [InlineData("abc", TypeContext.None, true, false)]
        [InlineData("abc", TypeContext.Output, true, false)]
        [InlineData("abc", TypeContext.Output, false, true)]
        [InlineData("abc", TypeContext.Output, null, true)]
        [InlineData("abc", TypeContext.Output, true, null)]
        [InlineData("abc", TypeContext.Output, null, null)]
        [Theory]
        public void Object_Equals_True(
            string typeName,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode(typeName),
                context,
                isTypeNullable,
                isElementTypeNullable);

            object y = new SyntaxTypeReference(
                new NamedTypeNode(typeName),
                context,
                isTypeNullable,
                isElementTypeNullable);

            // act
            bool result = x.Equals(y);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void SyntaxTypeReference_Equals_False()
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode("abc"),
                TypeContext.Input,
                true,
                false);

            var y = new SyntaxTypeReference(
                new NamedTypeNode("cde"),
                TypeContext.Input,
                true,
                false);

            // act
            bool result = x.Equals(y);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void ISyntaxTypeReference_Equals_False()
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode("abc"),
                TypeContext.Input,
                true,
                false);

            ISyntaxTypeReference y = new SyntaxTypeReference(
                new NamedTypeNode("cde"),
                TypeContext.Input,
                true,
                false);

            // act
            bool result = x.Equals(y);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Object_Equals_False()
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode("abc"),
                TypeContext.Input,
                true,
                false);

            object y = new SyntaxTypeReference(
                new NamedTypeNode("cde"),
                TypeContext.Input,
                true,
                false);

            // act
            bool result = x.Equals(y);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void SyntaxTypeReference_RefEquals_True()
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode("abc"),
                TypeContext.Input,
                true,
                false);

            // act
            bool result = x.Equals(x);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void ISyntaxTypeReference_RefEquals_True()
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode("abc"),
                TypeContext.Input,
                true,
                false);

            // act
            bool result = x.Equals((ISyntaxTypeReference)x);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Object_RefEquals_True()
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode("abc"),
                TypeContext.Input,
                true,
                false);

            // act
            bool result = x.Equals((object)x);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void SyntaxTypeReference_NullEquals_False()
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode("abc"),
                TypeContext.Input,
                true,
                false);

            // act
            bool result = x.Equals((SyntaxTypeReference)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void ISyntaxTypeReference_NullEquals_False()
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode("abc"),
                TypeContext.Input,
                true,
                false);

            // act
            bool result = x.Equals((ISyntaxTypeReference)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Object_NullEquals_False()
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode("abc"),
                TypeContext.Input,
                true,
                false);

            // act
            bool result = x.Equals((object)null);

            // assert
            Assert.False(result);
        }

        [InlineData("abc", TypeContext.Input, false, null)]
        [InlineData("abc", TypeContext.Input, null, false)]
        [InlineData("abc", TypeContext.Output, true, false)]
        [InlineData("abc", TypeContext.None, true, false)]
        [InlineData("def", TypeContext.Input, true, false)]
        [Theory]
        public void SyntaxTypeReference_GetHashCode_NotEquals(
           string typeName,
           TypeContext context,
           bool? isTypeNullable,
           bool? isElementTypeNullable)
        {
            // arrange
            var x = new SyntaxTypeReference(
                new NamedTypeNode("abc"),
                TypeContext.Input,
                true,
                false);

            var y = new SyntaxTypeReference(
                new NamedTypeNode(typeName),
                context,
                isTypeNullable,
                isElementTypeNullable);

            // act
            int xhash = x.GetHashCode();
            int yhash = y.GetHashCode();

            // assert
            Assert.NotEqual(xhash, yhash);
        }


        [Fact]
        public void ClrTypeReference_ToString()
        {
            // arrange
            var typeReference = new SyntaxTypeReference(
               new NonNullTypeNode(new NamedTypeNode("abc")),
               TypeContext.Input,
               true,
               false);

            // act
            string result = typeReference.ToString();

            // assert
            Assert.Equal("Input: abc!", result);
        }
    }
}
