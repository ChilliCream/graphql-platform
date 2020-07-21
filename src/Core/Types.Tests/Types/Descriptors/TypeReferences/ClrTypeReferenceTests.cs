using System;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class ClrTypeReferenceTests
    {
        [InlineData(typeof(string[]), TypeContext.Input, false, null)]
        [InlineData(typeof(string[]), TypeContext.Input, null, false)]
        [InlineData(typeof(string), TypeContext.Input, true, false)]
        [InlineData(typeof(string), TypeContext.None, true, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(string), TypeContext.Output, false, true)]
        [InlineData(typeof(string), TypeContext.Output, null, true)]
        [InlineData(typeof(string), TypeContext.Output, true, null)]
        [InlineData(typeof(string), TypeContext.Output, null, null)]
        [Theory]
        public void ClrTypeReference_CreateTypeReference(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            // act
            var typeReference = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            // assert
            Assert.Equal(clrType, typeReference.Type);
            Assert.Equal(context, typeReference.Context);
            Assert.Equal(isTypeNullable, typeReference.IsTypeNullable);
            Assert.Equal(isElementTypeNullable,
                typeReference.IsElementTypeNullable);
        }

        [InlineData(typeof(int?[]), false, true, typeof(NonNullType<NativeType<int?[]>>))]
        [InlineData(typeof(int?[]), true, null, typeof(int?[]))]
        [InlineData(typeof(int?[]), false, false, typeof(NonNullType<NativeType<int[]>>))]
        [InlineData(typeof(int), false, true, typeof(int))]
        [InlineData(typeof(int), true, true, typeof(int?))]
        [InlineData(typeof(int), false, false, typeof(int))]
        [InlineData(typeof(int[]), false, true, typeof(NonNullType<NativeType<int?[]>>))]
        [InlineData(typeof(int[]), true, false, typeof(int[]))]
        [Theory]
        public void ClrTypeReference_RewriteType(
            Type clrType,
            bool? isTypeNullable,
            bool? isElementTypeNullable,
            Type rewrittenClrType)
        {
            // arrange
            var typeReference = new ClrTypeReference(
                clrType,
                TypeContext.None,
                isTypeNullable,
                isElementTypeNullable);

            // act
            IClrTypeReference compiled = typeReference.Compile();

            // assert
            Assert.Equal(rewrittenClrType, compiled.Type);
            Assert.Null(compiled.IsTypeNullable);
            Assert.Null(compiled.IsElementTypeNullable);
        }


        [InlineData(typeof(string[]), TypeContext.Input, false, null)]
        [InlineData(typeof(string[]), TypeContext.Input, null, false)]
        [InlineData(typeof(string), TypeContext.Input, true, false)]
        [InlineData(typeof(string), TypeContext.None, true, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(string), TypeContext.Output, false, true)]
        [InlineData(typeof(string), TypeContext.Output, null, true)]
        [InlineData(typeof(string), TypeContext.Output, true, null)]
        [InlineData(typeof(string), TypeContext.Output, null, null)]
        [Theory]
        public void ClrTypeReference_Equals_True(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            var y = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            // act
            bool result = x.Equals(y);

            // assert
            Assert.True(result);
        }

        [InlineData(typeof(string[]), TypeContext.Input, false, null)]
        [InlineData(typeof(string[]), TypeContext.Input, null, false)]
        [InlineData(typeof(string), TypeContext.Input, true, false)]
        [InlineData(typeof(string), TypeContext.None, true, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(string), TypeContext.Output, false, true)]
        [InlineData(typeof(string), TypeContext.Output, null, true)]
        [InlineData(typeof(string), TypeContext.Output, true, null)]
        [InlineData(typeof(string), TypeContext.Output, null, null)]
        [Theory]
        public void ClrTypeReference_CtxNone_Equals_True(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            var y = new ClrTypeReference(
                clrType,
                TypeContext.None,
                isTypeNullable,
                isElementTypeNullable);

            // act
            bool result = x.Equals(y);

            // assert
            Assert.True(result);
        }

        [InlineData(typeof(string[]), TypeContext.Input, false, null)]
        [InlineData(typeof(string[]), TypeContext.Input, null, false)]
        [InlineData(typeof(string), TypeContext.Input, true, false)]
        [InlineData(typeof(string), TypeContext.None, true, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(string), TypeContext.Output, false, true)]
        [InlineData(typeof(string), TypeContext.Output, null, true)]
        [InlineData(typeof(string), TypeContext.Output, true, null)]
        [InlineData(typeof(string), TypeContext.Output, null, null)]
        [Theory]
        public void IClrTypeReference_Equals_True(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            IClrTypeReference y = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            // act

            bool result = x.Equals(y);

            // assert
            Assert.True(result);
        }

        [InlineData(typeof(string[]), TypeContext.Input, false, null)]
        [InlineData(typeof(string[]), TypeContext.Input, null, false)]
        [InlineData(typeof(string), TypeContext.Input, true, false)]
        [InlineData(typeof(string), TypeContext.None, true, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(string), TypeContext.Output, false, true)]
        [InlineData(typeof(string), TypeContext.Output, null, true)]
        [InlineData(typeof(string), TypeContext.Output, true, null)]
        [InlineData(typeof(string), TypeContext.Output, null, null)]
        [Theory]
        public void IClrTypeReference_CtxNone_Equals_True(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            IClrTypeReference y = new ClrTypeReference(
                clrType,
                TypeContext.None,
                isTypeNullable,
                isElementTypeNullable);

            // act

            bool result = x.Equals(y);

            // assert
            Assert.True(result);
        }

        [InlineData(typeof(string[]), TypeContext.Input, false, null)]
        [InlineData(typeof(string[]), TypeContext.Input, null, false)]
        [InlineData(typeof(string), TypeContext.Input, true, false)]
        [InlineData(typeof(string), TypeContext.None, true, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(string), TypeContext.Output, false, true)]
        [InlineData(typeof(string), TypeContext.Output, null, true)]
        [InlineData(typeof(string), TypeContext.Output, true, null)]
        [InlineData(typeof(string), TypeContext.Output, null, null)]
        [Theory]
        public void Object_Equals_True(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            IClrTypeReference y = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            // act
            bool result = x.Equals(y);

            // assert
            Assert.True(result);
        }

        [InlineData(typeof(string[]), TypeContext.Input, false, null)]
        [InlineData(typeof(string[]), TypeContext.Input, null, false)]
        [InlineData(typeof(string), TypeContext.Input, true, false)]
        [InlineData(typeof(string), TypeContext.None, true, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(string), TypeContext.Output, false, true)]
        [InlineData(typeof(string), TypeContext.Output, null, true)]
        [InlineData(typeof(string), TypeContext.Output, true, null)]
        [InlineData(typeof(string), TypeContext.Output, null, null)]
        [Theory]
        public void Object_CtxNone_Equals_True(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            IClrTypeReference y = new ClrTypeReference(
                clrType,
                TypeContext.None,
                isTypeNullable,
                isElementTypeNullable);

            // act
            bool result = x.Equals(y);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void ClrTypeReference_EqualsToNull_False()
        {
            // arrange
            var x = new ClrTypeReference(
                typeof(string),
                TypeContext.None,
                true,
                false);

            // act
            bool result = x.Equals((ClrTypeReference)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void IClrTypeReference_EqualsToNull_False()
        {
            // arrange
            var x = new ClrTypeReference(
                typeof(string),
                TypeContext.None,
                true,
                false);

            // act
            bool result = x.Equals((IClrTypeReference)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Object_EqualsToNull_False()
        {
            // arrange
            var x = new ClrTypeReference(
                typeof(string),
                TypeContext.None,
                true,
                false);

            // act
            bool result = x.Equals((object)null);

            // assert
            Assert.False(result);
        }

        [InlineData(typeof(string), TypeContext.None, true, null)]
        [InlineData(typeof(string), TypeContext.None, null, false)]
        [InlineData(typeof(string), TypeContext.None, true, true)]
        [InlineData(typeof(string), TypeContext.None, false, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(int), TypeContext.None, true, false)]
        [InlineData(typeof(object), TypeContext.None, true, false)]
        [Theory]
        public void ClrTypeReference_EqualsToDifferent_False(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new ClrTypeReference(
                typeof(string),
                TypeContext.Input,
                true,
                false);

            var y = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            // act
            bool result = x.Equals((ClrTypeReference)y);

            // assert
            Assert.False(result);
        }

        [InlineData(typeof(string), TypeContext.None, true, null)]
        [InlineData(typeof(string), TypeContext.None, null, false)]
        [InlineData(typeof(string), TypeContext.None, true, true)]
        [InlineData(typeof(string), TypeContext.None, false, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(int), TypeContext.None, true, false)]
        [InlineData(typeof(object), TypeContext.None, true, false)]
        [Theory]
        public void IClrTypeReference_EqualsToDifferent_False(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new ClrTypeReference(
                typeof(string),
                TypeContext.Input,
                true,
                false);

            var y = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            // act
            bool result = x.Equals((IClrTypeReference)y);

            // assert
            Assert.False(result);
        }

        [InlineData(typeof(string), TypeContext.None, true, null)]
        [InlineData(typeof(string), TypeContext.None, null, false)]
        [InlineData(typeof(string), TypeContext.None, true, true)]
        [InlineData(typeof(string), TypeContext.None, false, false)]
        [InlineData(typeof(string), TypeContext.Output, true, false)]
        [InlineData(typeof(int), TypeContext.None, true, false)]
        [InlineData(typeof(object), TypeContext.None, true, false)]
        [Theory]
        public void Object_EqualsToDifferent_False(
            Type clrType,
            TypeContext context,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new ClrTypeReference(
                typeof(string),
                TypeContext.Input,
                true,
                false);

            var y = new ClrTypeReference(
                clrType,
                context,
                isTypeNullable,
                isElementTypeNullable);

            // act
            bool result = x.Equals((object)y);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void ClrTypeReference_RefEquals_True()
        {
            // arrange
            var x = new ClrTypeReference(
                typeof(string),
                TypeContext.None,
                true,
                false);

            // act
            bool result = x.Equals((ClrTypeReference)x);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void IClrTypeReference_RefEquals_True()
        {
            // arrange
            var x = new ClrTypeReference(
                typeof(string),
                TypeContext.None,
                true,
                false);

            // act
            bool result = x.Equals((IClrTypeReference)x);

            // assert
            Assert.True(result);
        }

        [Fact]
        public void Object_RefEquals_True()
        {
            // arrange
            var x = new ClrTypeReference(
                typeof(string),
                TypeContext.None,
                true,
                false);

            // act
            bool result = x.Equals((object)x);

            // assert
            Assert.True(result);
        }

        [InlineData(typeof(string), null, false)]
        [InlineData(typeof(string), true, true)]
        [InlineData(typeof(string), false, false)]
        [InlineData(typeof(int), true, false)]
        [InlineData(typeof(object), true, false)]
        [Theory]
        public void ClrTypeReference_GetHashCode_NotEquals(
            Type clrType,
            bool? isTypeNullable,
            bool? isElementTypeNullable)
        {
            // arrange
            var x = new ClrTypeReference(
                typeof(string),
                TypeContext.None,
                true,
                false);

            var y = new ClrTypeReference(
                clrType,
                TypeContext.Input,
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
            var typeReference = new ClrTypeReference(
                typeof(string),
                TypeContext.Input);

            // act
            string result = typeReference.ToString();

            // assert
            Assert.Equal("Input: System.String", result);
        }
    }
}
