using HotChocolate.Language;

namespace HotChocolate.Types.Descriptors;

public class SyntaxTypeReferenceTests
{
    private readonly ITypeInspector _typeInspector = new DefaultTypeInspector();

    [Fact]
    public void TypeReference_Create()
    {
        // arrange
        var namedType = new NamedTypeNode("Foo");

        // act
        var typeReference = TypeReference.Create(
            namedType,
            TypeContext.Input,
            scope: "foo");

        // assert
        Assert.Equal(namedType, typeReference.Type);
        Assert.Equal(TypeContext.Input, typeReference.Context);
        Assert.Equal("foo", typeReference.Scope);
    }

    [Fact]
    public void TypeReference_Create_With_Name()
    {
        // arrange
        // act
        var typeReference = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // assert
        Assert.Equal("Foo", typeReference.Type.NamedType().Name.Value);
        Assert.Equal(TypeContext.Input, typeReference.Context);
        Assert.Equal("foo", typeReference.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_Equals_To_Null()
    {
        // arrange
        var x = TypeReference.Create(
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
        var x = TypeReference.Create(
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
        var x = TypeReference.Create(
            "Foo",
            TypeContext.None);

        var y = TypeReference.Create(
            "Foo",
            TypeContext.Output);

        var z = TypeReference.Create(
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
        var x = TypeReference.Create(
            "Foo",
            TypeContext.None,
            scope: "a");

        var y = TypeReference.Create(
            "Foo",
            TypeContext.Output,
            scope: "a");

        var z = TypeReference.Create(
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
    public void TypeReference_Equals_To_Null()
    {
        // arrange
        var x = TypeReference.Create(
            "Foo",
            TypeContext.None);

        // act
        var result = x.Equals((TypeReference)null);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void TypeReference_Equals_To_Same()
    {
        // arrange
        var x = TypeReference.Create(
            "Foo",
            TypeContext.None);

        // act
        var xx = x.Equals((TypeReference)x);

        // assert
        Assert.True(xx);
    }

    [Fact]
    public void TypeReference_Equals_To_SyntaxTypeRef()
    {
        // arrange
        var x = TypeReference.Create(
            "Foo",
            TypeContext.None);

        // act
        var xx = x.Equals(TypeReference.Create(_typeInspector.GetType(typeof(int))));

        // assert
        Assert.False(xx);
    }

    [Fact]
    public void TypeReference_Equals_Context_None_Does_Not_Matter()
    {
        // arrange
        var x = TypeReference.Create(
            "Foo",
            TypeContext.None);

        var y = TypeReference.Create(
            "Foo",
            TypeContext.Output);

        var z = TypeReference.Create(
            "Foo",
            TypeContext.Input);

        // act
        var xy = x.Equals((TypeReference)y);
        var xz = x.Equals((TypeReference)z);
        var yz = y.Equals((TypeReference)z);

        // assert
        Assert.True(xy);
        Assert.True(xz);
        Assert.False(yz);
    }

    [Fact]
    public void TypeReference_Equals_Scope_Different()
    {
        // arrange
        var x = TypeReference.Create(
            "Foo",
            TypeContext.None,
            scope: "a");

        var y = TypeReference.Create(
            "Foo",
            TypeContext.Output,
            scope: "a");

        var z = TypeReference.Create(
            "Foo",
            TypeContext.Input);

        // act
        var xy = x.Equals((TypeReference)y);
        var xz = x.Equals((TypeReference)z);
        var yz = y.Equals((TypeReference)z);

        // assert
        Assert.True(xy);
        Assert.False(xz);
        Assert.False(yz);
    }

    [Fact]
    public void Object_Equals_To_Null()
    {
        // arrange
        var x = TypeReference.Create(
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
        var x = TypeReference.Create(
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
        var x = TypeReference.Create(
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
        var x = TypeReference.Create(
            "Foo",
            TypeContext.None);

        var y = TypeReference.Create(
            "Foo",
            TypeContext.Output);

        var z = TypeReference.Create(
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
        var x = TypeReference.Create(
            "Foo",
            TypeContext.None,
            scope: "a");

        var y = TypeReference.Create(
            "Foo",
            TypeContext.Output,
            scope: "a");

        var z = TypeReference.Create(
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
    public void SyntaxTypeReference_ToString()
    {
        // arrange
        var typeReference = TypeReference.Create(
            "Foo",
            TypeContext.Input);

        // act
        var result = typeReference.ToString();

        // assert
        Assert.Equal("Foo (Input)", result);
    }

    [Fact]
    public void SyntaxTypeReference_WithType()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.WithType(new NamedTypeNode("Bar"));

        // assert
        Assert.Equal("Bar", Assert.IsType<NamedTypeNode>(typeReference2.Type).Name.Value);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_WithType_Null()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        Action action = () => typeReference1.WithType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void SyntaxTypeReference_WithContext()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.WithContext(TypeContext.Output);

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(TypeContext.Output, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_WithContext_Nothing()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.WithContext();

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(TypeContext.None, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_WithScope()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.WithScope("bar");

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal("bar", typeReference2.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_WithScope_Nothing()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.WithScope();

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Null(typeReference2.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_With()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With(
            new NamedTypeNode("Bar"),
            TypeContext.Output,
            scope: "bar");

        // assert
        Assert.Equal("Bar", Assert.IsType<NamedTypeNode>(typeReference2.Type).Name.Value);
        Assert.Equal(TypeContext.Output, typeReference2.Context);
        Assert.Equal("bar", typeReference2.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_With_Nothing()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With();

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_With_Type()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With(new NamedTypeNode("Bar"));

        // assert
        Assert.Equal("Bar", Assert.IsType<NamedTypeNode>(typeReference2.Type).Name.Value);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_With_Type_Null()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        Action action = () => typeReference1.With(null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void SyntaxTypeReference_With_Context()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With(context: TypeContext.None);

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(TypeContext.None, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_With_Scope()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            "Foo",
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With(scope: "bar");

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal("bar", typeReference2.Scope);
    }

    [Fact]
    public void SyntaxTypeReference_GetHashCode()
    {
        // arrange
        var x = TypeReference.Create(
            "Foo",
            TypeContext.None,
            scope: "foo");

        var y = TypeReference.Create(
            "Foo",
            TypeContext.None,
            scope: "foo");

        var z = TypeReference.Create(
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
