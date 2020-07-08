using System;
using System.Collections.Generic;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class ClrTypeReferenceTests
    {
        [InlineData(typeof(string[]), TypeContext.Input, "foo", new bool[] { false })]
        [InlineData(typeof(string[]), TypeContext.Input, null, new bool[] { false, false })]
        [InlineData(typeof(string), TypeContext.Input, null, null)]
        [InlineData(typeof(string[]), TypeContext.Output, "foo", new bool[] { false })]
        [InlineData(typeof(string[]), TypeContext.Output, null, new bool[] { false, false })]
        [InlineData(typeof(string), TypeContext.Output, null, null)]
        [InlineData(typeof(string[]), TypeContext.None, "foo", new bool[] { false })]
        [InlineData(typeof(string[]), TypeContext.None, null, new bool[] { false, false })]
        [InlineData(typeof(string), TypeContext.None, null, null)]
        [Theory]
        public void TypeReference_Create(
            Type clrType,
            TypeContext context,
            string scope,
            bool[] nullable)
        {
            // arrange
            // act
            var typeReference = TypeReference.Create(
                clrType,
                context,
                scope: scope,
                nullable: nullable);

            // assert
            Assert.Equal(clrType, typeReference.Type);
            Assert.Equal(context, typeReference.Context);
            Assert.Equal(scope, typeReference.Scope);
            Assert.Equal(nullable, typeReference.Nullable);
        }

        [Fact]
        public void TypeReference_Create_Generic()
        {
            // arrange
            // act
            var typeReference = TypeReference.Create<int>(
                TypeContext.Input,
                scope: "abc",
                nullable: new bool[] { true, false });

            // assert
            Assert.Equal(typeof(int), typeReference.Type);
            Assert.Equal(TypeContext.None, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable,
                t => Assert.True(t),
                t => Assert.False(t));
        }

        [Fact]
        public void TypeReference_Create_And_Infer_Output_Context()
        {
            // arrange
            // act
            var typeReference = TypeReference.Create(
                typeof(ObjectType<string>),
                scope: "abc",
                nullable: new bool[] { true, false });

            // assert
            Assert.Equal(typeof(ObjectType<string>), typeReference.Type);
            Assert.Equal(TypeContext.Output, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable,
                t => Assert.True(t),
                t => Assert.False(t));
        }

        [Fact]
        public void TypeReference_Create_And_Infer_Input_Context()
        {
            // arrange
            // act
            var typeReference = TypeReference.Create(
                typeof(InputObjectType<string>),
                scope: "abc",
                nullable: new bool[] { true, false });

            // assert
            Assert.Equal(typeof(InputObjectType<string>), typeReference.Type);
            Assert.Equal(TypeContext.Output, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable,
                t => Assert.True(t),
                t => Assert.False(t));
        }

        [Fact]
        public void TypeReference_Create_Generic_And_Infer_Output_Context()
        {
            // arrange
            // act
            var typeReference = TypeReference.Create<ObjectType<string>>(
                scope: "abc",
                nullable: new bool[] { true, false });

            // assert
            Assert.Equal(typeof(ObjectType<string>), typeReference.Type);
            Assert.Equal(TypeContext.Output, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable,
                t => Assert.True(t),
                t => Assert.False(t));
        }

        [Fact]
        public void TypeReference_Create_Generic_And_Infer_Input_Context()
        {
            // arrange
            // act
            var typeReference = TypeReference.Create<InputObjectType<string>>(
                scope: "abc",
                nullable: new bool[] { true, false });

            // assert
            Assert.Equal(typeof(InputObjectType<string>), typeReference.Type);
            Assert.Equal(TypeContext.Output, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable,
                t => Assert.True(t),
                t => Assert.False(t));
        }


        [InlineData(
            typeof(string), 
            new bool [] { false }, 
            typeof(NonNullType<NativeType<string>>))]
        [InlineData(
            typeof(List<List<string>>), 
            new bool [] { false, true, false }, 
            typeof(NonNullType<NativeType<List<List<NonNullType<NativeType<string>>>>>>))]
        [Theory]
        public void ClrTypeReference_RewriteType(
            Type type,
            bool[] nullable,
            Type expectedRewrittenType)
        {
            // arrange
            var typeReference = TypeReference.Create(
                type,
                nullable: nullable);

            // act
            var rewritten = typeReference.Rewrite();

            // assert
            string s = rewritten.Type.GetTypeName();
            Assert.Equal(expectedRewrittenType.GetTypeName(), rewritten.Type.GetTypeName());
        }

        /*


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
        */
    }
}
