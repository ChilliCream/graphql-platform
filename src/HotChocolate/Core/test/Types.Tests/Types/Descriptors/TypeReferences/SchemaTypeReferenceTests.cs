using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Types.Descriptors
{
    public class SchemaTypeReferenceTests
    {
        [Fact]
        public async Task TypeReference_Create_From_OutputType()
        {
            // arrange
            ObjectType<Foo> type = await CreateTypeAsync<ObjectType<Foo>>();

            // act
            SchemaTypeReference typeReference = TypeReference.Create(
                type,
                scope: "abc",
                nullable: new bool[] { true });

            // assert
            Assert.Equal(type, typeReference.Type);
            Assert.Equal(TypeContext.Output, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable, Assert.True);
        }

        [Fact]
        public async Task TypeReference_Create_From_InputType()
        {
            // arrange
            InputObjectType<Bar> type = await CreateTypeAsync<InputObjectType<Bar>>();

            // act
            SchemaTypeReference typeReference = TypeReference.Create(
                type,
                scope: "abc",
                nullable: new bool[] { true });

            // assert
            Assert.Equal(type, typeReference.Type);
            Assert.Equal(TypeContext.Input, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable, Assert.True);
        }

        [Fact]
        public async Task TypeReference_Create_From_ScalarType()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();

            // act
            SchemaTypeReference typeReference = TypeReference.Create(
                type,
                scope: "abc",
                nullable: new bool[] { true });

            // assert
            Assert.Equal(type, typeReference.Type);
            Assert.Equal(TypeContext.None, typeReference.Context);
            Assert.Equal("abc", typeReference.Scope);
            Assert.Collection(typeReference.Nullable, Assert.True);
        }

        [Fact]
        public async Task SchemaTypeReference_Equals_To_Null()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type);

            // act
            var result = x.Equals((SchemaTypeReference)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task SchemaTypeReference_Equals_To_Same()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type);

            // act
            var xx = x.Equals((SchemaTypeReference)x);

            // assert
            Assert.True(xx);
        }

        [Fact]
        public async Task SchemaTypeReference_Equals_Scope_Different()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type, scope: "abc");
            SchemaTypeReference y = TypeReference.Create(type, scope: "def");
            SchemaTypeReference z = TypeReference.Create(type, scope: "abc");

            // act
            var xy = x.Equals(y);
            var xz = x.Equals(y);

            // assert
            Assert.False(xy);
            Assert.False(xz);
        }

        [Fact]
        public async Task SchemaTypeReference_Equals_Nullability()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type, nullable: new[] { true, false });
            SchemaTypeReference y = TypeReference.Create(type, nullable: new[] { false, false });
            SchemaTypeReference z = TypeReference.Create(type, nullable: new[] { true, false });

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
        public async Task ITypeReference_Equals_To_Null()
        {
             // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type);

            // act
            var result = x.Equals((ITypeReference)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task ITypeReference_Equals_To_Same()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type);

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

            var y = new ClrTypeReference(
                typeof(string),
                TypeContext.Output);

            var z = new ClrTypeReference(
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

            var y = new ClrTypeReference(
                typeof(string),
                TypeContext.Output,
                scope: "a");

            var z = new ClrTypeReference(
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

            var y = new ClrTypeReference(
                typeof(string),
                TypeContext.Output,
                nullable: new bool[] { false, false });

            var z = new ClrTypeReference(
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

            var y = new ClrTypeReference(
                typeof(string),
                TypeContext.Output);

            var z = new ClrTypeReference(
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

            var y = new ClrTypeReference(
                typeof(string),
                TypeContext.Output,
                scope: "a");

            var z = new ClrTypeReference(
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

            var y = new ClrTypeReference(
                typeof(string),
                TypeContext.Output,
                nullable: new bool[] { false, false });

            var z = new ClrTypeReference(
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

        public class Foo
        {
            public string Bar => "bar";
        }

        public class Bar
        {
            public string Baz { get; set; }
        }
    }
}
