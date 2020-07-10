using System;
using System.Collections.Generic;
using HotChocolate.Utilities;
using Xunit;
using Nullable = System.Nullable;

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
            ClrTypeReference typeReference = TypeReference.Create(
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
        public void Foo()
        {
            TypeReference ref1 =  TypeReference.Create(typeof(string), TypeContext.None);
            TypeReference ref2 =  TypeReference.Create(typeof(string), TypeContext.None);
            TypeReference ref3 =  TypeReference.Create(typeof(string), TypeContext.Output);
            
            var set = new HashSet<TypeReference>();
            set.Add(ref1);

            bool r2 = set.Contains(ref2);
            bool r3 = set.Contains(ref3);
        }

        [Fact]
        public void TypeReference_Create_Generic()
        {
            // arrange
            // act
            ClrTypeReference typeReference = TypeReference.Create<int>(
                TypeContext.Input,
                scope: "abc",
                nullable: new bool[] { true, false });

            // assert
            Assert.Equal(typeof(int), typeReference.Type);
            Assert.Equal(TypeContext.Input, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable!,
                Assert.True,
                Assert.False);
        }

        [Fact]
        public void TypeReference_Create_And_Infer_Output_Context()
        {
            // arrange
            // act
            ClrTypeReference typeReference = TypeReference.Create(
                typeof(ObjectType<string>),
                scope: "abc",
                nullable: new bool[] { true, false });

            // assert
            Assert.Equal(typeof(ObjectType<string>), typeReference.Type);
            Assert.Equal(TypeContext.Output, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable!,
                Assert.True,
                Assert.False);
        }

        [Fact]
        public void TypeReference_Create_And_Infer_Input_Context()
        {
            // arrange
            // act
            ClrTypeReference typeReference = TypeReference.Create(
                typeof(InputObjectType<string>),
                scope: "abc",
                nullable: new bool[] { true, false });

            // assert
            Assert.Equal(typeof(InputObjectType<string>), typeReference.Type);
            Assert.Equal(TypeContext.Input, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable!,
                Assert.True,
                Assert.False);
        }

        [Fact]
        public void TypeReference_Create_Generic_And_Infer_Output_Context()
        {
            // arrange
            // act
            ClrTypeReference typeReference = TypeReference.Create<ObjectType<string>>(
                scope: "abc",
                nullable: new bool[] { true, false });

            // assert
            Assert.Equal(typeof(ObjectType<string>), typeReference.Type);
            Assert.Equal(TypeContext.Output, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable!,
                Assert.True,
                Assert.False);
        }

        [Fact]
        public void TypeReference_Create_Generic_And_Infer_Input_Context()
        {
            // arrange
            // act
            ClrTypeReference typeReference = TypeReference.Create<InputObjectType<string>>(
                scope: "abc",
                nullable: new bool[] { true, false });

            // assert
            Assert.Equal(typeof(InputObjectType<string>), typeReference.Type);
            Assert.Equal(TypeContext.Input, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable!,
                Assert.True,
                Assert.False);
        }


        [InlineData(
            typeof(string),
            new bool[] { false },
            typeof(NonNullType<NativeType<string>>))]
        [InlineData(
            typeof(string),
            new bool[] { true },
            typeof(string))]
        [InlineData(
            typeof(string),
            null,
            typeof(string))]
        [InlineData(
            typeof(List<List<string>>),
            new bool[] { false, true, false },
            typeof(NonNullType<NativeType<List<List<NonNullType<NativeType<string>>>>>>))]
        [InlineData(
            typeof(List<List<string>>),
            new bool[] { false },
            typeof(NonNullType<NativeType<List<List<string>>>>))]
        [Theory]
        public void ClrTypeReference_RewriteType(
            Type type,
            bool[] nullable,
            Type expectedRewrittenType)
        {
            // arrange
            ClrTypeReference typeReference = TypeReference.Create(
                type,
                nullable: nullable);

            // act
            ClrTypeReference rewritten = typeReference.Rewrite();

            // assert
            Assert.Equal(expectedRewrittenType.GetTypeName(), rewritten.Type.GetTypeName());
        }

        [Fact]
        public void ClrTypeReference_Equals_To_Null()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            // act
            var result = x.Equals((ClrTypeReference)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void ClrTypeReference_Equals_To_Same()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            // act
            var xx = x.Equals((ClrTypeReference)x);

            // assert
            Assert.True(xx);
        }

        [Fact]
        public void ClrTypeReference_Equals_Context_None_Does_Not_Matter()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            var y = TypeReference.Create(
                typeof(string),
                TypeContext.Output);

            var z = TypeReference.Create(
                typeof(string),
                TypeContext.Input);

            // act
            var xy = x.Equals(y);
            var xz = x.Equals(z);
            var yz = y.Equals(z);

            // assert
            Assert.True(xy);
            Assert.True(xz);
            Assert.False(yz);
        }

        [Fact]
        public void ClrTypeReference_Equals_Scope_Different()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None,
                scope: "a");

            var y = TypeReference.Create(
                typeof(string),
                TypeContext.Output,
                scope: "a");

            var z = TypeReference.Create(
                typeof(string),
                TypeContext.Input);

            // act
            var xy = x.Equals(y);
            var xz = x.Equals(z);
            var yz = y.Equals(z);

            // assert
            Assert.True(xy);
            Assert.False(xz);
            Assert.False(yz);
        }

        [Fact]
        public void ClrTypeReference_Equals_Nullability()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None,
                nullable: new bool[] { true, false });

            var y = TypeReference.Create(
                typeof(string),
                TypeContext.Output,
                nullable: new bool[] { false, false });

            var z = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                nullable: new bool[] { true, false });

            // act
            var xy = x.Equals(y);
            var xz = x.Equals(z);
            var yz = y.Equals(z);

            // assert
            Assert.False(xy);
            Assert.True(xz);
            Assert.False(yz);
        }

        [Fact]
        public void ITypeReference_Equals_To_Null()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            // act
            var result = x.Equals((ITypeReference)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void ITypeReference_Equals_To_Same()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            // act
            var xx = x.Equals((ITypeReference)x);

            // assert
            Assert.True(xx);
        }

        [Fact]
        public void ITypeReference_Equals_To_SyntaxTypeRef()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            // act
            var xx = x.Equals(TypeReference.Create(new NameType("foo")));

            // assert
            Assert.False(xx);
        }

        [Fact]
        public void ITypeReference_Equals_Context_None_Does_Not_Matter()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            var y = TypeReference.Create(
                typeof(string),
                TypeContext.Output);

            var z = TypeReference.Create(
                typeof(string),
                TypeContext.Input);

            // act
            var xy = x.Equals((ITypeReference)y);
            var xz = x.Equals((ITypeReference)z);
            var yz = y.Equals((ITypeReference)z);

            // assert
            Assert.True(xy);
            Assert.True(xz);
            Assert.False(yz);
        }

        [Fact]
        public void ITypeReference_Equals_Scope_Different()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None,
                scope: "a");

            var y = TypeReference.Create(
                typeof(string),
                TypeContext.Output,
                scope: "a");

            var z = TypeReference.Create(
                typeof(string),
                TypeContext.Input);

            // act
            var xy = x.Equals((ITypeReference)y);
            var xz = x.Equals((ITypeReference)z);
            var yz = y.Equals((ITypeReference)z);

            // assert
            Assert.True(xy);
            Assert.False(xz);
            Assert.False(yz);
        }

        [Fact]
        public void ITypeReference_Equals_Nullability()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None,
                nullable: new bool[] { true, false });

            var y = TypeReference.Create(
                typeof(string),
                TypeContext.Output,
                nullable: new bool[] { false, false });

            var z = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                nullable: new bool[] { true, false });

            // act
            var xy = x.Equals((ITypeReference)y);
            var xz = x.Equals((ITypeReference)z);
            var yz = y.Equals((ITypeReference)z);

            // assert
            Assert.False(xy);
            Assert.True(xz);
            Assert.False(yz);
        }

        [Fact]
        public void Object_Equals_To_Null()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            // act
            var result = x.Equals((object)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void Object_Equals_To_Same()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            // act
            var xx = x.Equals((object)x);

            // assert
            Assert.True(xx);
        }

        [Fact]
        public void Object_Equals_To_Object()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            // act
            var xx = x.Equals(new object());

            // assert
            Assert.False(xx);
        }

        [Fact]
        public void Object_Equals_Context_None_Does_Not_Matter()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None);

            var y = TypeReference.Create(
                typeof(string),
                TypeContext.Output);

            var z = TypeReference.Create(
                typeof(string),
                TypeContext.Input);

            // act
            var xy = x.Equals((object)y);
            var xz = x.Equals((object)z);
            var yz = y.Equals((object)z);

            // assert
            Assert.True(xy);
            Assert.True(xz);
            Assert.False(yz);
        }

        [Fact]
        public void Object_Equals_Scope_Different()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None,
                scope: "a");

            var y = TypeReference.Create(
                typeof(string),
                TypeContext.Output,
                scope: "a");

            var z = TypeReference.Create(
                typeof(string),
                TypeContext.Input);

            // act
            var xy = x.Equals((object)y);
            var xz = x.Equals((object)z);
            var yz = y.Equals((object)z);

            // assert
            Assert.True(xy);
            Assert.False(xz);
            Assert.False(yz);
        }

        [Fact]
        public void Object_Equals_Nullability()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None,
                nullable: new bool[] { true, false });

            var y = TypeReference.Create(
                typeof(string),
                TypeContext.Output,
                nullable: new bool[] { false, false });

            var z = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                nullable: new bool[] { true, false });

            // act
            var xy = x.Equals((object)y);
            var xz = x.Equals((object)z);
            var yz = y.Equals((object)z);

            // assert
            Assert.False(xy);
            Assert.True(xz);
            Assert.False(yz);
        }

        [Fact]
        public void ClrTypeReference_ToString()
        {
            // arrange
            ClrTypeReference typeReference = TypeReference.Create(
                typeof(string),
                TypeContext.Input);

            // act
            var result = typeReference.ToString();

            // assert
            Assert.Equal("Input: System.String", result);
        }

        [Fact]
        public void ClrTypeReference_WithType()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.WithType(typeof(int));

            // assert
            Assert.Equal(typeof(int), typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_WithType_Null()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            Action action = () => typeReference1.WithType(null!);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void ClrTypeReference_WithContext()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.WithContext(TypeContext.Output);

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(TypeContext.Output, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_WithContext_Nothing()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.WithContext();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(TypeContext.None, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_WithScope()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.WithScope("bar");

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal("bar", typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_WithScope_Nothing()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.WithScope();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Null(typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_WithNullable()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.WithNullable(new[] { false });

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Collection(typeReference2.Nullable!, Assert.False);
        }

        [Fact]
        public void ClrTypeReference_WithNullable_Nothing()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.WithNullable();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Null(typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_With()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.With(
                typeof(int),
                TypeContext.Output,
                scope: "bar",
                nullable: new[] { false });

            // assert
            Assert.Equal(typeof(int), typeReference2.Type);
            Assert.Equal(TypeContext.Output, typeReference2.Context);
            Assert.Equal("bar", typeReference2.Scope);
            Assert.Collection(typeReference2.Nullable!, Assert.False);
        }

        [Fact]
        public void ClrTypeReference_With_Nothing()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.With();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_With_Type()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.With(typeof(int));

            // assert
            Assert.Equal(typeof(int), typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_With_Type_Null()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            Action action = () => typeReference1.With(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void ClrTypeReference_With_Context()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.With(context: TypeContext.None);

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(TypeContext.None, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_With_Scope()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.With(scope: "bar");

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal("bar", typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_With_Nullable()
        {
            // arrange
            ClrTypeReference typeReference1 = TypeReference.Create(
                typeof(string),
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            ClrTypeReference typeReference2 = typeReference1.With(nullable: null);

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Null(typeReference2.Nullable);
        }

        [Fact]
        public void ClrTypeReference_GetHashCode()
        {
            // arrange
            ClrTypeReference x = TypeReference.Create(
                typeof(string),
                TypeContext.None,
                scope: "foo",
                nullable: new[] { false });

            ClrTypeReference y = TypeReference.Create(
                typeof(string),
                TypeContext.None,
                scope: "foo",
                nullable: new[] { false });

            ClrTypeReference z = TypeReference.Create(
                typeof(string),
                TypeContext.Input);

            // act
            var xh = x.GetHashCode();
            var yh = y.GetHashCode();
            var zh = z.GetHashCode();

            // assert
            Assert.Equal(xh, yh);
            Assert.NotEqual(xh, zh);
        }
    }
}
