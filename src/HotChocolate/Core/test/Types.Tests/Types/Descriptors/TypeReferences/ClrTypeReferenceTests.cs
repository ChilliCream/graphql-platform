using HotChocolate.Internal;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors;

public class ClrTypeReferenceTests
{
    private readonly ITypeInspector _typeInspector = new DefaultTypeInspector();

    [InlineData(typeof(string[]), TypeContext.Input, "foo")]
    [InlineData(typeof(string[]), TypeContext.Input, null)]
    [InlineData(typeof(string), TypeContext.Input, null)]
    [InlineData(typeof(string[]), TypeContext.Output, null)]
    [InlineData(typeof(string), TypeContext.None, null)]
    [Theory]
    public void TypeReference_Create(
        Type clrType,
        TypeContext context,
        string scope)
    {
        // arrange
        // act
        var typeReference = TypeReference.Create(
            _typeInspector.GetType(clrType),
            context,
            scope: scope);

        // assert
        Assert.Equal(clrType, typeReference.Type.Source);
        Assert.Equal(context, typeReference.Context);
        Assert.Equal(scope, typeReference.Scope);
    }

    [Fact]
    public void TypeReference_Create_And_Infer_Output_Context()
    {
        // arrange
        // act
        var typeReference = TypeReference.Create(
            _typeInspector.GetType(typeof(ObjectType<string>)),
            scope: "abc");

        // assert
        Assert.Equal(typeof(ObjectType<string>), typeReference.Type.Source);
        Assert.Equal(TypeContext.Output, typeReference.Context);
        Assert.Equal("abc", typeReference.Scope);
    }

    [Fact]
    public void TypeReference_Create_And_Infer_Input_Context()
    {
        // arrange
        // act
        var typeReference = TypeReference.Create(
            _typeInspector.GetType(typeof(InputObjectType<string>)),
            scope: "abc");

        // assert
        Assert.Equal(typeof(InputObjectType<string>), typeReference.Type.Source);
        Assert.Equal(TypeContext.Input, typeReference.Context);
        Assert.Equal("abc", typeReference.Scope);
    }

    [Fact]
    public void ClrTypeReference_Equals_To_Null()
    {
        // arrange
        var x = TypeReference.Create(
            _typeInspector.GetType(typeof(string)));

        // act
        var result = x.Equals((ExtendedTypeReference)null);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void ClrTypeReference_Equals_To_Same()
    {
        // arrange
        var x = TypeReference.Create(
            _typeInspector.GetType(typeof(string)));

        // act
        var xx = x.Equals((ExtendedTypeReference)x);

        // assert
        Assert.True(xx);
    }

    [Fact]
    public void ClrTypeReference_Equals_Context_None_Does_Not_Matter()
    {
        // arrange
        var x = TypeReference.Create(
            _typeInspector.GetType(typeof(string)));

        var y = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Output);

        var z = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
        var x = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.None,
            scope: "a");

        var y = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Output,
            scope: "a");

        var z = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
            _typeInspector.GetType(typeof(string)));

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
            _typeInspector.GetType(typeof(string)));

        // act
        var xx = x.Equals((TypeReference)x);

        // assert
        Assert.True(xx);
    }

    [Fact]
    public void TypeReference_Equals_To_SyntaxTypeRef()
    {
        // arrange
        var x = TypeReference.Create(_typeInspector.GetType(typeof(string)));

        // act
        var xx = x.Equals(TypeReference.Create(new StringType("foo")));

        // assert
        Assert.False(xx);
    }

    [Fact]
    public void TypeReference_Equals_Context_None_Does_Not_Matter()
    {
        // arrange
        var x = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.None);

        var y = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Output);

        var z = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
            _typeInspector.GetType(typeof(string)),
            TypeContext.None,
            scope: "a");

        var y = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Output,
            scope: "a");

        var z = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
            _typeInspector.GetType(typeof(string)));

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
            _typeInspector.GetType(typeof(string)));

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
            _typeInspector.GetType(typeof(string)));

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
            _typeInspector.GetType(typeof(string)));

        var y = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Output);

        var z = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
            _typeInspector.GetType(typeof(string)),
            TypeContext.None,
            scope: "a");

        var y = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Output,
            scope: "a");

        var z = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
    public void ClrTypeReference_ToString()
    {
        // arrange
        var typeReference = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Input);

        // act
        var result = typeReference.ToString();

        // assert
        Assert.Equal("String (Input)", result);
    }

    [Fact]
    public void ClrTypeReference_WithType()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 =
            typeReference1.WithType(_typeInspector.GetType(typeof(int)));

        // assert
        Assert.Equal(typeof(int), typeReference2.Type.Source);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public void ClrTypeReference_WithType_Null()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Input,
            scope: "foo");

        // act
        Action action = () => typeReference1.WithType(default!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void ClrTypeReference_WithContext()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
    public void ClrTypeReference_WithContext_Nothing()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
    public void ClrTypeReference_WithScope()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
    public void ClrTypeReference_WithScope_Nothing()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
    public void ClrTypeReference_With()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With(
            _typeInspector.GetType(typeof(int)),
            TypeContext.Output,
            scope: "bar");

        // assert
        Assert.Equal(typeof(int), typeReference2.Type.Source);
        Assert.Equal(TypeContext.Output, typeReference2.Context);
        Assert.Equal("bar", typeReference2.Scope);
    }

    [Fact]
    public void ClrTypeReference_With_Nothing()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
    public void ClrTypeReference_With_Type()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Input,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With(
            _typeInspector.GetType(typeof(int)));

        // assert
        Assert.Equal(typeof(int), typeReference2.Type.Source);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public void ClrTypeReference_With_Context()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
    public void ClrTypeReference_With_Scope()
    {
        // arrange
        var typeReference1 = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
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
    public void ClrTypeReference_GetHashCode()
    {
        // arrange
        var x = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.None,
            scope: "foo");

        var y = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.None,
            scope: "foo");

        var z = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Input);

        // act
        var xh = x.GetHashCode();
        var yh = y.GetHashCode();
        var zh = z.GetHashCode();

        // assert
        Assert.Equal(xh, yh);
        Assert.NotEqual(xh, zh);
    }

    [Fact]
    public void ClrTypeReference_GetHashCode_Context_HasNoEffect()
    {
        // arrange
        var x = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.None);

        var y = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Output);

        var z = TypeReference.Create(
            _typeInspector.GetType(typeof(string)),
            TypeContext.Input);

        // act
        var xh = x.GetHashCode();
        var yh = y.GetHashCode();
        var zh = z.GetHashCode();

        // assert
        Assert.Equal(xh, yh);
        Assert.Equal(xh, zh);
    }
}
