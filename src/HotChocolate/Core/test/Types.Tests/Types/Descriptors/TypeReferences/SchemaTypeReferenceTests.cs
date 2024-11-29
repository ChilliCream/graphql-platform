using static HotChocolate.Tests.TestHelper;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types.Descriptors;

public class SchemaTypeReferenceTests
{
    [Fact]
    public async Task TypeReference_Create_From_OutputType()
    {
        // arrange
        var type = await CreateTypeAsync<ObjectType<Foo>>();

        // act
        var typeReference = TypeReference.Create(
            type,
            scope: "abc");

        // assert
        Assert.Equal(type, typeReference.Type);
        Assert.Equal(TypeContext.Output, typeReference.Context);
        Assert.Equal("abc", typeReference.Scope);
    }

    [Fact]
    public async Task TypeReference_Create_From_InputType()
    {
        // arrange
        var type = await CreateTypeAsync<InputObjectType<Bar>>();

        // act
        var typeReference = TypeReference.Create(
            type,
            scope: "abc");

        // assert
        Assert.Equal(type, typeReference.Type);
        Assert.Equal(TypeContext.Input, typeReference.Context);
        Assert.Equal("abc", typeReference.Scope);
    }

    [Fact]
    public async Task TypeReference_Create_From_ScalarType()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();

        // act
        var typeReference = TypeReference.Create(
            type,
            scope: "abc");

        // assert
        Assert.Equal(type, typeReference.Type);
        Assert.Equal(TypeContext.None, typeReference.Context);
        Assert.Equal("abc", typeReference.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_Equals_To_Null()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type);

        // act
        var result = x.Equals((SchemaTypeReference)null);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task SchemaTypeReference_Equals_To_Same()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type);

        // act
        var xx = x.Equals((SchemaTypeReference)x);

        // assert
        Assert.True(xx);
    }

    [Fact]
    public async Task SchemaTypeReference_Equals_Scope_Different()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type, scope: "abc");
        var y = TypeReference.Create(type, scope: "def");
        var z = TypeReference.Create(type, scope: "abc");

        // act
        var xy = x.Equals(y);
        var xz = x.Equals(y);

        // assert
        Assert.False(xy);
        Assert.False(xz);
    }

    [Fact]
    public async Task TypeReference_Equals_To_Null()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type);

        // act
        var result = x.Equals((TypeReference)null);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task TypeReference_Equals_To_Same()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type);

        // act
        var xx = x.Equals((TypeReference)x);

        // assert
        Assert.True(xx);
    }

    [Fact]
    public async Task TypeReference_Equals_To_SyntaxTypeRef()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type);

        // act
        var xx = x.Equals(TypeReference.Create(new StringType("foo")));

        // assert
        Assert.False(xx);
    }

    [Fact]
    public async Task TypeReference_Equals_Scope_Different()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type, scope: "abc");
        var y = TypeReference.Create(type, scope: "def");
        var z = TypeReference.Create(type, scope: "abc");

        // act
        var xy = x.Equals((TypeReference)y);
        var xz = x.Equals((TypeReference)y);

        // assert
        Assert.False(xy);
        Assert.False(xz);
    }

    [Fact]
    public async Task Object_Equals_To_Null()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type);

        // act
        var result = x.Equals((object)null);

        // assert
        Assert.False(result);
    }

    [Fact]
    public async Task Object_Equals_To_Same()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type);

        // act
        var xx = x.Equals((object)x);

        // assert
        Assert.True(xx);
    }

    [Fact]
    public async Task Object_Equals_To_Object()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type);

        // act
        var xx = x.Equals(new object());

        // assert
        Assert.False(xx);
    }

    [Fact]
    public async Task Object_Equals_Scope_Different()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(type, scope: "abc");
        var y = TypeReference.Create(type, scope: "def");
        var z = TypeReference.Create(type, scope: "abc");

        // act
        var xy = x.Equals((object)y);
        var xz = x.Equals((object)y);

        // assert
        Assert.False(xy);
        Assert.False(xz);
    }

    [Fact]
    public async Task SchemaTypeReference_ToString()
    {
        // arrange
        var type = await CreateTypeAsync<StringType>();
        var typeReference = TypeReference.Create(type);

        // act
        var result = typeReference.ToString();

        // assert
        Assert.Equal("HotChocolate.Types.StringType", result);
    }

    [Fact]
    public async Task SchemaTypeReference_WithType()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var intType = await CreateTypeAsync<IntType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.WithType(intType);

        // assert
        Assert.Equal(intType, typeReference2.Type);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_WithType_Null()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        Action action = () => typeReference1.WithType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task SchemaTypeReference_WithContext()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.WithContext(TypeContext.Output);

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(TypeContext.Output, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_WithContext_Nothing()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.WithContext();

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(TypeContext.None, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_WithScope()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.WithScope("bar");

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal("bar", typeReference2.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_WithScope_Nothing()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.WithScope();

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Null(typeReference2.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_With()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var intType = await CreateTypeAsync<IntType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With(
            intType,
            scope: "bar");

        // assert
        Assert.Equal(intType, typeReference2.Type);
        Assert.Equal(TypeContext.None, typeReference2.Context);
        Assert.Equal("bar", typeReference2.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_With_Nothing()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With();

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_With_Type()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var intType = await CreateTypeAsync<IntType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With(intType);

        // assert
        Assert.Equal(intType, typeReference2.Type);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_With_Type_Null()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        Action action = () => typeReference1.With(null);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public async Task SchemaTypeReference_With_Context()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With(context: TypeContext.None);

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(TypeContext.None, typeReference2.Context);
        Assert.Equal(typeReference1.Scope, typeReference2.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_With_Scope()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var typeReference1 = TypeReference.Create(
            stringType,
            scope: "foo");

        // act
        var typeReference2 = typeReference1.With(scope: "bar");

        // assert
        Assert.Equal(typeReference1.Type, typeReference2.Type);
        Assert.Equal(typeReference1.Context, typeReference2.Context);
        Assert.Equal("bar", typeReference2.Scope);
    }

    [Fact]
    public async Task SchemaTypeReference_GetHashCode()
    {
        // arrange
        var stringType = await CreateTypeAsync<StringType>();
        var x = TypeReference.Create(
            stringType,
            scope: "foo");

        var y = TypeReference.Create(
            stringType,
            scope: "foo");

        var z = TypeReference.Create(
            stringType);

        // act
        var xh = x.GetHashCode();
        var yh = y.GetHashCode();
        var zh = z.GetHashCode();

        // assert
        Assert.Equal(xh, yh);
        Assert.NotEqual(xh, zh);
    }

    [Fact]
    public void SchemaTypeReference_InferTypeContext_From_SchemaType()
    {
        // arrange
        // act
        var context = SchemaTypeReference.InferTypeContext(typeof(ObjectType<Foo>));

        // assert
        Assert.Equal(TypeContext.Output, context);
    }

    [Fact]
    public void SchemaTypeReference_InferTypeContext_Object_From_SchemaType()
    {
        // arrange
        // act
        var context = SchemaTypeReference.InferTypeContext((object)typeof(ObjectType<Foo>));

        // assert
        Assert.Equal(TypeContext.Output, context);
    }

    [Fact]
    public void SchemaTypeReference_InferTypeContext_Object_From_String_None()
    {
        // arrange
        // act
        var context = SchemaTypeReference.InferTypeContext((object)"foo");

        // assert
        Assert.Equal(TypeContext.None, context);
    }

    [Fact]
    public void SchemaTypeReference_InferTypeContext_From_RuntimeType_None()
    {
        // arrange
        // act
        var context = SchemaTypeReference.InferTypeContext(typeof(Foo));

        // assert
        Assert.Equal(TypeContext.None, context);
    }

    [Fact]
    public void SchemaTypeReference_InferTypeContext_Type_Is_Null()
    {
        // arrange
        // act
        Action action = () => SchemaTypeReference.InferTypeContext(default(Type)!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
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
