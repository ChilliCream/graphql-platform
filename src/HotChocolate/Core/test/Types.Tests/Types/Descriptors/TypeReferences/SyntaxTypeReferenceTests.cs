using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class SyntaxTypeReferenceTests
    {
        [Fact]
        public void TypeReference_Create()
        {
            // arrange
            var namedType = new NamedTypeNode("Foo");

            // act
            SyntaxTypeReference typeReference = TypeReference.Create(
                namedType,
                TypeContext.Input,
                scope: "foo",
                nullable: new bool[] { false });

            // assert
            Assert.Equal(namedType, typeReference.Type);
            Assert.Equal(TypeContext.Input, typeReference.Context);
            Assert.Equal("foo", typeReference.Scope);
            Assert.Collection(typeReference.Nullable, Assert.False);
        }

        [Fact]
        public void TypeReference_Create_With_Name()
        {
            // arrange
            // act
            SyntaxTypeReference typeReference = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new bool[] { false });

            // assert
            Assert.Equal("Foo", typeReference.Type.NamedType().Name.Value);
            Assert.Equal(TypeContext.Input, typeReference.Context);
            Assert.Equal("foo", typeReference.Scope);
            Assert.Collection(typeReference.Nullable, Assert.False);
        }

        [Fact]
        public void SyntaxTypeReference_RewriteType_Type_Is_The_Same()
        {
            // arrange
            SyntaxTypeReference typeReference = TypeReference.Create("Foo");

            // act
            SyntaxTypeReference rewritten = typeReference.Rewrite();

            // assert
            Assert.Equal(typeReference, rewritten);
        }

        [Fact]
        public void SyntaxTypeReference_RewriteType_SimpleNamedType()
        {
            // arrange
            SyntaxTypeReference typeReference = TypeReference.Create(
                "Foo",
                nullable: new bool[] { false });

            // act
            SyntaxTypeReference rewritten = typeReference.Rewrite();

            // assert
            Assert.Equal("Foo",
                Assert.IsType<NamedTypeNode>(
                    Assert.IsType<NonNullTypeNode>(rewritten.Type).Type).Name.Value);
        }

        [Fact]
        public void SyntaxTypeReference_RewriteType_NonNullToNullable()
        {
            // arrange
            SyntaxTypeReference typeReference = TypeReference.Create(
                new NonNullTypeNode(new NamedTypeNode("Foo")),
                nullable: new bool[] { true });

            // act
            SyntaxTypeReference rewritten = typeReference.Rewrite();

            // assert
            Assert.Equal("Foo",
                Assert.IsType<NamedTypeNode>(
                    rewritten.Type).Name.Value);
        }

        [Fact]
        public void SyntaxTypeReference_RewriteType_List()
        {
            // arrange
            SyntaxTypeReference typeReference = TypeReference.Create(
                new ListTypeNode(new NamedTypeNode("Foo")),
                nullable: new bool[] { false });

            // act
            SyntaxTypeReference rewritten = typeReference.Rewrite();

            // assert
            Assert.Equal("Foo",
                Assert.IsType<NamedTypeNode>(
                    Assert.IsType<ListTypeNode>(
                        Assert.IsType<NonNullTypeNode>(rewritten.Type).Type)
                            .Type).Name.Value);
        }

        [Fact]
        public void SyntaxTypeReference_RewriteType_List_2()
        {
            // arrange
            SyntaxTypeReference typeReference = TypeReference.Create(
                new ListTypeNode(new NamedTypeNode("Foo")),
                nullable: new bool[] { false, false });

            // act
            SyntaxTypeReference rewritten = typeReference.Rewrite();

            // assert
            Assert.Equal("Foo",
                Assert.IsType<NamedTypeNode>(
                    Assert.IsType<NonNullTypeNode>(
                        Assert.IsType<ListTypeNode>(
                            Assert.IsType<NonNullTypeNode>(rewritten.Type).Type)
                                .Type).Type).Name.Value);
        }

        [Fact]
        public void SyntaxTypeReference_RewriteType_NestedList()
        {
            // arrange
            SyntaxTypeReference typeReference = TypeReference.Create(
                new ListTypeNode(new ListTypeNode(new NamedTypeNode("Foo"))),
                nullable: new bool[] { false, false });

            // act
            SyntaxTypeReference rewritten = typeReference.Rewrite();

            // assert
            Assert.Equal("Foo",
                Assert.IsType<NamedTypeNode>(
                    Assert.IsType<ListTypeNode>(
                        Assert.IsType<NonNullTypeNode>(
                            Assert.IsType<ListTypeNode>(
                                Assert.IsType<NonNullTypeNode>(rewritten.Type).Type)
                                    .Type).Type).Type).Name.Value);
        }


        [Fact]
        public void SyntaxTypeReference_Equals_To_Null()
        {
            // arrange
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None);

            // act
            var result = x.Equals((SyntaxTypeReference)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public void SyntaxTypeReference_Equals_To_Same()
        {
            // arrange
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None);

            // act
            var xx = x.Equals((SyntaxTypeReference)x);

            // assert
            Assert.True(xx);
        }

        [Fact]
        public void SyntaxTypeReference_Equals_Context_None_Does_Not_Matter()
        {
            // arrange
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None);

            SyntaxTypeReference y = TypeReference.Create(
                "Foo",
                TypeContext.Output);

            SyntaxTypeReference z = TypeReference.Create(
                "Foo",
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
        public void SyntaxTypeReference_Equals_Scope_Different()
        {
            // arrange
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None,
                scope: "a");

            SyntaxTypeReference y = TypeReference.Create(
                "Foo",
                TypeContext.Output,
                scope: "a");

            SyntaxTypeReference z = TypeReference.Create(
                "Foo",
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
        public void SyntaxTypeReference_Equals_Nullability()
        {
            // arrange
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None,
                nullable: new bool[] { true, false });

            SyntaxTypeReference y = TypeReference.Create(
                "Foo",
                TypeContext.Output,
                nullable: new bool[] { false, false });

            SyntaxTypeReference z = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None);

            // act
            var xx = x.Equals(TypeReference.Create(typeof(int)));

            // assert
            Assert.False(xx);
        }

        [Fact]
        public void ITypeReference_Equals_Context_None_Does_Not_Matter()
        {
            // arrange
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None);

            SyntaxTypeReference y = TypeReference.Create(
                "Foo",
                TypeContext.Output);

            SyntaxTypeReference z = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None,
                scope: "a");

            SyntaxTypeReference y = TypeReference.Create(
                "Foo",
                TypeContext.Output,
                scope: "a");

            SyntaxTypeReference z = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None,
                nullable: new bool[] { true, false });

            SyntaxTypeReference y = TypeReference.Create(
                "Foo",
                TypeContext.Output,
                nullable: new bool[] { false, false });

            SyntaxTypeReference z = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None);

            SyntaxTypeReference y = TypeReference.Create(
                "Foo",
                TypeContext.Output);

            SyntaxTypeReference z = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None,
                scope: "a");

            SyntaxTypeReference y = TypeReference.Create(
                "Foo",
                TypeContext.Output,
                scope: "a");

            SyntaxTypeReference z = TypeReference.Create(
                "Foo",
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
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None,
                nullable: new bool[] { true, false });

            SyntaxTypeReference y = TypeReference.Create(
                "Foo",
                TypeContext.Output,
                nullable: new bool[] { false, false });

            SyntaxTypeReference z = TypeReference.Create(
                "Foo",
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
        public void SyntaxTypeReference_ToString()
        {
            // arrange
            SyntaxTypeReference typeReference = TypeReference.Create(
                "Foo",
                TypeContext.Input);

            // act
            var result = typeReference.ToString();

            // assert
            Assert.Equal("Input: Foo", result);
        }

        [Fact]
        public void SyntaxTypeReference_WithType()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.WithType(new NamedTypeNode("Bar"));

            // assert
            Assert.Equal("Bar", Assert.IsType<NamedTypeNode>(typeReference2.Type).Name.Value);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_WithType_Null()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            Action action = () => typeReference1.WithType(null!);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void SyntaxTypeReference_WithContext()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.WithContext(TypeContext.Output);

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(TypeContext.Output, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_WithContext_Nothing()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.WithContext();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(TypeContext.None, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_WithScope()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.WithScope("bar");

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal("bar", typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_WithScope_Nothing()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.WithScope();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Null(typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_WithNullable()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.WithNullable(new[] { false });

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Collection(typeReference2.Nullable!, Assert.False);
        }

        [Fact]
        public void SyntaxTypeReference_WithNullable_Nothing()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.WithNullable();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Null(typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_With()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.With(
                new NamedTypeNode("Bar"),
                TypeContext.Output,
                scope: "bar",
                nullable: new[] { false });

            // assert
            Assert.Equal("Bar", Assert.IsType<NamedTypeNode>(typeReference2.Type).Name.Value);
            Assert.Equal(TypeContext.Output, typeReference2.Context);
            Assert.Equal("bar", typeReference2.Scope);
            Assert.Collection(typeReference2.Nullable!, Assert.False);
        }

        [Fact]
        public void SyntaxTypeReference_With_Nothing()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.With();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_With_Type()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.With(new NamedTypeNode("Bar"));

            // assert
            Assert.Equal("Bar", Assert.IsType<NamedTypeNode>(typeReference2.Type).Name.Value);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_With_Type_Null()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            Action action = () => typeReference1.With(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void SyntaxTypeReference_With_Context()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.With(context: TypeContext.None);

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(TypeContext.None, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_With_Scope()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.With(scope: "bar");

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal("bar", typeReference2.Scope);
            Assert.Equal(typeReference1.Nullable, typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_With_Nullable()
        {
            // arrange
            SyntaxTypeReference typeReference1 = TypeReference.Create(
                "Foo",
                TypeContext.Input,
                scope: "foo",
                nullable: new[] { true });

            // act
            SyntaxTypeReference typeReference2 = typeReference1.With(nullable: null);

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
            Assert.Null(typeReference2.Nullable);
        }

        [Fact]
        public void SyntaxTypeReference_GetHashCode()
        {
            // arrange
            SyntaxTypeReference x = TypeReference.Create(
                "Foo",
                TypeContext.None,
                scope: "foo",
                nullable: new[] { false });

            SyntaxTypeReference y = TypeReference.Create(
                "Foo",
                TypeContext.None,
                scope: "foo",
                nullable: new[] { false });

            SyntaxTypeReference z = TypeReference.Create(
                "Foo",
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
