using System;
using System.Threading.Tasks;
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
            InputObjectType<Bar> type = await CreateTypeAsync<InputObjectType<Bar>>();

            // act
            SchemaTypeReference typeReference = TypeReference.Create(
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
            StringType type = await CreateTypeAsync<StringType>();

            // act
            SchemaTypeReference typeReference = TypeReference.Create(
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
        public async Task ITypeReference_Equals_To_SyntaxTypeRef()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type);

            // act
            var xx = x.Equals(TypeReference.Create(new NameType("foo")));

            // assert
            Assert.False(xx);
        }

        [Fact]
        public async Task ITypeReference_Equals_Scope_Different()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type, scope: "abc");
            SchemaTypeReference y = TypeReference.Create(type, scope: "def");
            SchemaTypeReference z = TypeReference.Create(type, scope: "abc");

            // act
            var xy = x.Equals((ITypeReference)y);
            var xz = x.Equals((ITypeReference)y);

            // assert
            Assert.False(xy);
            Assert.False(xz);
        }

        [Fact]
        public async Task Object_Equals_To_Null()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type);

            // act
            var result = x.Equals((object)null);

            // assert
            Assert.False(result);
        }

        [Fact]
        public async Task Object_Equals_To_Same()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type);

            // act
            var xx = x.Equals((object)x);

            // assert
            Assert.True(xx);
        }

        [Fact]
        public async Task Object_Equals_To_Object()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type);

            // act
            var xx = x.Equals(new object());

            // assert
            Assert.False(xx);
        }

        [Fact]
        public async Task Object_Equals_Scope_Different()
        {
            // arrange
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(type, scope: "abc");
            SchemaTypeReference y = TypeReference.Create(type, scope: "def");
            SchemaTypeReference z = TypeReference.Create(type, scope: "abc");

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
            StringType type = await CreateTypeAsync<StringType>();
            SchemaTypeReference typeReference = TypeReference.Create(type);

            // act
            var result = typeReference.ToString();

            // assert
            Assert.Equal("None: HotChocolate.Types.StringType", result);
        }

        [Fact]
        public async Task SchemaTypeReference_WithType()
        {
            // arrange
            StringType stringType = await CreateTypeAsync<StringType>();
            IntType intType = await CreateTypeAsync<IntType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
                stringType,
                scope: "foo");

            // act
            SchemaTypeReference typeReference2 = typeReference1.WithType(intType);

            // assert
            Assert.Equal(intType, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
        }

        [Fact]
        public async Task SchemaTypeReference_WithType_Null()
        {
            // arrange
            StringType stringType = await CreateTypeAsync<StringType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
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
            StringType stringType = await CreateTypeAsync<StringType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
                stringType,
                scope: "foo");

            // act
            SchemaTypeReference typeReference2 = typeReference1.WithContext(TypeContext.Output);

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(TypeContext.Output, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
        }

        [Fact]
        public async Task SchemaTypeReference_WithContext_Nothing()
        {
            // arrange
            StringType stringType = await CreateTypeAsync<StringType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
                stringType,
                scope: "foo");

            // act
            SchemaTypeReference typeReference2 = typeReference1.WithContext();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(TypeContext.None, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
        }

        [Fact]
        public async Task SchemaTypeReference_WithScope()
        {
            // arrange
            StringType stringType = await CreateTypeAsync<StringType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
                stringType,
                scope: "foo");

            // act
            SchemaTypeReference typeReference2 = typeReference1.WithScope("bar");

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal("bar", typeReference2.Scope);
        }

        [Fact]
        public async Task SchemaTypeReference_WithScope_Nothing()
        {
            // arrange
            StringType stringType = await CreateTypeAsync<StringType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
                stringType,
                scope: "foo");

            // act
            SchemaTypeReference typeReference2 = typeReference1.WithScope();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Null(typeReference2.Scope);
        }

        [Fact]
        public async Task SchemaTypeReference_With()
        {
            // arrange
            StringType stringType = await CreateTypeAsync<StringType>();
            IntType intType = await CreateTypeAsync<IntType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
                stringType,
                scope: "foo");

            // act
            SchemaTypeReference typeReference2 = typeReference1.With(
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
            StringType stringType = await CreateTypeAsync<StringType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
                stringType,
                scope: "foo");

            // act
            SchemaTypeReference typeReference2 = typeReference1.With();

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
        }

        [Fact]
        public async Task SchemaTypeReference_With_Type()
        {
            // arrange
            StringType stringType = await CreateTypeAsync<StringType>();
            IntType intType = await CreateTypeAsync<IntType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
                stringType,
                scope: "foo");

            // act
            SchemaTypeReference typeReference2 = typeReference1.With(intType);

            // assert
            Assert.Equal(intType, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
        }

        [Fact]
        public async Task SchemaTypeReference_With_Type_Null()
        {
            // arrange
            StringType stringType = await CreateTypeAsync<StringType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
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
            StringType stringType = await CreateTypeAsync<StringType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
                stringType,
                scope: "foo");

            // act
            SchemaTypeReference typeReference2 = typeReference1.With(context: TypeContext.None);

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(TypeContext.None, typeReference2.Context);
            Assert.Equal(typeReference1.Scope, typeReference2.Scope);
        }

        [Fact]
        public async Task SchemaTypeReference_With_Scope()
        {
            // arrange
            StringType stringType = await CreateTypeAsync<StringType>();
            SchemaTypeReference typeReference1 = TypeReference.Create(
                stringType,
                scope: "foo");

            // act
            SchemaTypeReference typeReference2 = typeReference1.With(scope: "bar");

            // assert
            Assert.Equal(typeReference1.Type, typeReference2.Type);
            Assert.Equal(typeReference1.Context, typeReference2.Context);
            Assert.Equal("bar", typeReference2.Scope);
        }

        [Fact]
        public async Task SchemaTypeReference_GetHashCode()
        {
            // arrange
            StringType stringType = await CreateTypeAsync<StringType>();
            SchemaTypeReference x = TypeReference.Create(
                stringType,
                scope: "foo");

            SchemaTypeReference y = TypeReference.Create(
                stringType,
                scope: "foo");

            SchemaTypeReference z = TypeReference.Create(
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
            TypeContext context = SchemaTypeReference.InferTypeContext(typeof(ObjectType<Foo>));

            // assert
            Assert.Equal(TypeContext.Output, context);
        }

        [Fact]
        public void SchemaTypeReference_InferTypeContext_Object_From_SchemaType()
        {
            // arrange
            // act
            TypeContext context = SchemaTypeReference.InferTypeContext((object)typeof(ObjectType<Foo>));

            // assert
            Assert.Equal(TypeContext.Output, context);
        }

        [Fact]
        public void SchemaTypeReference_InferTypeContext_Object_From_String_None()
        {
            // arrange
            // act
            TypeContext context = SchemaTypeReference.InferTypeContext((object)"foo");

            // assert
            Assert.Equal(TypeContext.None, context);
        }

        [Fact]
        public void SchemaTypeReference_InferTypeContext_From_RuntimeType_None()
        {
            // arrange
            // act
            TypeContext context = SchemaTypeReference.InferTypeContext(typeof(Foo));

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
}
