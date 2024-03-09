using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Snapshooter.Xunit;
using static HotChocolate.Types.FieldBindingFlags;
using static HotChocolate.WellKnownContextData;
using SnapshotExtensions = CookieCrumble.SnapshotExtensions;

namespace HotChocolate.Types;

public class ObjectTypeTests : TypeTestBase
{
    [Fact]
    public void ObjectType_DynamicName()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddObjectType(
                d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>()
                    .Field("bar")
                    .Type<StringType>()
                    .Resolve("foo"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<ObjectType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void ObjectType_DynamicName_NonGeneric()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddObjectType(
                d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))
                    .Field("bar")
                    .Type<StringType>()
                    .Resolve("foo"))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<ObjectType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void GenericObjectType_DynamicName()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddObjectType(
                d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>())
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<ObjectType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void GenericObjectType_DynamicName_NonGeneric()
    {
        // act
        var schema = SchemaBuilder.New()
            .AddObjectType(
                d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType)))
            .ModifyOptions(o => o.StrictValidation = false)
            .Create();

        // assert
        var type = schema.GetType<ObjectType>("StringFoo");
        Assert.NotNull(type);
    }

    [Fact]
    public void InitializeExplicitFieldWithImplicitResolver()
    {
        // arrange
        // act
        var fooType = CreateType(
            new ObjectType<Foo>(
                d => d
                    .Field(f => f.Description)
                    .Name("a")));

        // assert
        Assert.NotNull(fooType.Fields["a"].Resolver);
    }

    [Fact]
    public void IntArgumentIsInferredAsNonNullType()
    {
        // arrange
        // act
        var fooType =
            CreateType(new ObjectType<QueryWithIntArg>());

        // assert
        IType argumentType = fooType.Fields["bar"]
            .Arguments.First()
            .Type;

        Assert.NotNull(argumentType);
        Assert.True(argumentType.IsNonNullType());
        Assert.Equal("Int", argumentType.NamedType().Name);
    }

    [Fact]
    public async Task FieldMiddlewareIsIntegrated()
    {
        // arrange
        var resolverContext = new Mock<IMiddlewareContext>();
        resolverContext.SetupAllProperties();

        // act
        var fooType = CreateType(
            new ObjectType(
                c => c
                    .Name("Foo")
                    .Field("bar")
                    .Resolve(() => "baz")),
            b => b.Use(
                next => async context =>
                {
                    await next(context);

                    if (context.Result is string s)
                    {
                        context.Result = s.ToUpperInvariant();
                    }
                }));

        // assert
        await fooType.Fields["bar"].Middleware(resolverContext.Object);
        Assert.Equal("BAZ", resolverContext.Object.Result);
    }

    [Fact]
    public void Deprecated_Field_With_Reason()
    {
        // arrange
        var resolverContext = new Mock<IMiddlewareContext>();
        resolverContext.SetupAllProperties();

        // act
        var fooType = CreateType(
            new ObjectType(
                c => c
                    .Name("Foo")
                    .Field("bar")
                    .Deprecated("fooBar")
                    .Resolve(() => "baz")));

        // assert
        Assert.Equal("fooBar", fooType.Fields["bar"].DeprecationReason);
        Assert.True(fooType.Fields["bar"].IsDeprecated);
    }

    [Fact]
    public void Deprecated_Field_With_Reason_Is_Serialized()
    {
        // arrange
        var resolverContext = new Mock<IMiddlewareContext>();
        resolverContext.SetupAllProperties();

        // act
        var schema = CreateSchema(
            new ObjectType(
                c => c
                    .Name("Foo")
                    .Field("bar")
                    .Deprecated("fooBar")
                    .Resolve(() => "baz")));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Deprecated_Field_Without_Reason()
    {
        // arrange
        var resolverContext = new Mock<IMiddlewareContext>();
        resolverContext.SetupAllProperties();

        // act
        var fooType = CreateType(
            new ObjectType(
                c => c
                    .Name("Foo")
                    .Field("bar")
                    .Deprecated()
                    .Resolve(() => "baz")));

        // assert
        Assert.Equal(
            WellKnownDirectives.DeprecationDefaultReason,
            fooType.Fields["bar"].DeprecationReason);
        Assert.True(fooType.Fields["bar"].IsDeprecated);
    }

    [Fact]
    public void Deprecated_Field_Without_Reason_Is_Serialized()
    {
        // arrange
        var resolverContext = new Mock<IMiddlewareContext>();
        resolverContext.SetupAllProperties();

        // act
        var schema = CreateSchema(
            new ObjectType(
                c => c
                    .Name("Foo")
                    .Field("bar")
                    .Deprecated()
                    .Resolve(() => "baz")));

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void InitializesImplicitFieldWithImplicitResolver()
    {
        // arrange
        // act
        var fooType = CreateType(new ObjectType<Foo>());

        // assert
        Assert.NotNull(fooType.Fields.First().Resolver);
    }

    [Fact]
    public void EnsureObjectTypeKindIsCorrect()
    {
        // arrange
        // act
        var someObject = CreateType(new ObjectType<Foo>());

        // assert
        Assert.Equal(TypeKind.Object, someObject.Kind);
    }

    /// <summary>
    /// For the type detection the order of the resolver or type descriptor function should not matter.
    ///
    /// descriptor.Field("test")
    ///   .Resolver{List{string}}(() => new List{string}())
    ///   .Type{ListType{StringType}}();
    ///
    /// descriptor.Field("test")
    ///   .Type{ListType{StringType}}();
    ///   .Resolver{List{string}}(() => new List{string}())
    /// </summary>
    [Fact]
    public void ObjectTypeWithDynamicField_TypeDeclareOrderShouldNotMatter()
    {
        // act
        var fooType = CreateType(new FooType());

        // assert
        Assert.True(fooType.Fields.TryGetField("test", out var field));
        Assert.IsType<ListType>(field.Type);
        Assert.IsType<StringType>(((ListType)field.Type).ElementType);
    }

    [Fact]
    public void GenericObjectTypes()
    {
        // arrange
        // act
        var genericType =
            CreateType(new ObjectType<GenericFoo<string>>());

        // assert
        Assert.Equal("GenericFooOfString", genericType.Name);
    }

    [Fact]
    public void NestedGenericObjectTypes()
    {
        // arrange
        // act
        var genericType =
            CreateType(new ObjectType<GenericFoo<GenericFoo<string>>>());

        // assert
        Assert.Equal("GenericFooOfGenericFooOfString", genericType.Name);
    }

    [Fact]
    public void BindFieldToResolverTypeField()
    {
        // arrange
        // act
        var fooType = CreateType(
            new ObjectType<Foo>(
                d => d
                    .Field<FooResolver>(t => t.GetBar(default))));

        // assert
        Assert.Equal("foo", fooType.Fields["bar"].Arguments.First().Name);
        Assert.NotNull(fooType.Fields["bar"].Resolver);
        Assert.IsType<StringType>(fooType.Fields["bar"].Type);
    }


    [Fact]
    public void TwoInterfacesProvideFieldAWithDifferentOutputType()
    {
        // arrange
        var source = @"
                interface A {
                    a: String
                }

                interface B {
                    a: Int
                }

                type C implements A & B {
                    a: String
                }";

        // act
        try
        {
            SchemaBuilder.New()
                .AddDocumentFromString(source)
                .AddResolver("C.a", _ => new("foo"))
                .Create();
        }
        catch (SchemaException ex)
        {
            ex.Message.MatchSnapshot();
            return;
        }

        Assert.True(false, "Schema exception was not thrown.");
    }

    [Fact]
    public void TwoInterfacesProvideFieldAWithDifferentArguments1()
    {
        // arrange
        var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(b: String): String
                }

                type C implements A & B {
                    a(a: String): String
                }";

        // act
        try
        {
            SchemaBuilder.New()
                .AddDocumentFromString(source)
                .AddResolver("C.a", _ => new("foo"))
                .Create();
        }
        catch (SchemaException ex)
        {
            ex.Message.MatchSnapshot();
            return;
        }

        Assert.True(false, "Schema exception was not thrown.");
    }

    [Fact]
    public void TwoInterfacesProvideFieldAWithDifferentArguments2()
    {
        // arrange
        var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: Int): String
                }

                type C implements A & B {
                    a(a: String): String
                }";

        // act
        try
        {
            SchemaBuilder.New()
                .AddDocumentFromString(source)
                .AddResolver("C.a", _ => new("foo"))
                .Create();
        }
        catch (SchemaException ex)
        {
            ex.Message.MatchSnapshot();
            return;
        }

        Assert.True(false, "Schema exception was not thrown.");
    }

    [Fact]
    public void TwoInterfacesProvideFieldAWithDifferentArguments3()
    {
        // arrange
        var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String, b: String): String
                }

                type C implements A & B {
                    a(a: String): String
                }";

        // act
        try
        {
            SchemaBuilder.New()
                .AddDocumentFromString(source)
                .AddResolver("C.a", _ => new("foo"))
                .Create();
        }
        catch (SchemaException ex)
        {
            ex.Message.MatchSnapshot();
            return;
        }

        Assert.True(false, "Schema exception was not thrown.");
    }

    [Fact]
    public void SpecifyQueryTypeNameInSchemaFirst()
    {
        // arrange
        var source = @"
                type A { field: String }
                type B { field: String }
                type C { field: String }

                schema {
                  query: A
                  mutation: B
                  subscription: C
                }
            ";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .Use(_ => _)
            .Create();

        Assert.Equal("A", schema.QueryType.Name);
        Assert.Equal("B", schema.MutationType?.Name);
        Assert.Equal("C", schema.SubscriptionType?.Name);
    }

    [Fact]
    public void SpecifyQueryTypeNameInSchemaFirstWithOptions()
    {
        // arrange
        var source = @"
                type A { field: String }
                type B { field: String }
                type C { field: String }";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .Use(_ => _)
            .ModifyOptions(
                o =>
                {
                    o.QueryTypeName = "A";
                    o.MutationTypeName = "B";
                    o.SubscriptionTypeName = "C";
                })
            .Create();

        Assert.Equal("A", schema.QueryType.Name);
        Assert.Equal("B", schema.MutationType?.Name);
        Assert.Equal("C", schema.SubscriptionType?.Name);
    }

    [Fact]
    public void NoQueryType()
    {
        // arrange
        var source = @"type A { field: String }";

        // act
        void Action()
            => SchemaBuilder.New()
                .AddDocumentFromString(source)
                .Use(_ => _)
                .Create();

        Assert.Throws<SchemaException>(Action).Errors.MatchSnapshot();
    }

    [Fact]
    public void ObjectFieldDoesNotMatchInterfaceDefinitionArgTypeInvalid()
    {
        // arrange
        var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String): String
                }

                type C implements A & B {
                    a(a: [String]): String
                }";

        // act
        try
        {
            SchemaBuilder.New()
                .AddDocumentFromString(source)
                .AddResolver("C.a", _ => new("foo"))
                .Create();
        }
        catch (SchemaException ex)
        {
            ex.Message.MatchSnapshot();
            return;
        }

        Assert.True(false, "Schema exception was not thrown.");
    }

    [Fact]
    public void ObjectFieldDoesNotMatchInterfaceDefinitionReturnTypeInvalid()
    {
        // arrange
        var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String): String
                }

                type C implements A & B {
                    a(a: String): Int
                }";

        // act
        try
        {
            SchemaBuilder.New()
                .AddDocumentFromString(source)
                .AddResolver("C.a", _ => new("foo"))
                .Create();
        }
        catch (SchemaException ex)
        {
            ex.Message.MatchSnapshot();
            return;
        }

        Assert.True(false, "Schema exception was not thrown.");
    }

    [Fact]
    public void ObjectTypeImplementsAllFields()
    {
        // arrange
        var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String): String
                }

                type C implements A & B {
                    a(a: String): String
                }

                schema {
                  query: C
                }
            ";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddResolver("C.a", _ => new("foo"))
            .Create();

        // assert
        var type = schema.GetType<ObjectType>("C");
        Assert.Equal(2, type.Implements.Count);
    }

    [Fact]
    public void ObjectTypeImplementsAllFieldsWithWrappedTypes()
    {
        // arrange
        var source = @"
                interface A {
                    a(a: String!): String!
                }

                interface B {
                    a(a: String!): String!
                }

                type C implements A & B {
                    a(a: String!): String!
                }

                schema {
                  query: C
                }
            ";

        // act
        var schema = SchemaBuilder.New()
            .AddDocumentFromString(source)
            .AddResolver("C.a", _ => new("foo"))
            .Create();

        // assert
        var type = schema.GetType<ObjectType>("C");
        Assert.Equal(2, type.Implements.Count);
    }

    [Fact]
    public void NonNullAttribute_StringIsRewritten_NonNullStringType()
    {
        // arrange
        // act
        var fooType = CreateType(new ObjectType<Bar>());

        // assert
        Assert.True(fooType.Fields["baz"].Type.IsNonNullType());
        Assert.Equal("String", fooType.Fields["baz"].Type.NamedType().Name);
    }

    [Fact]
    public void ObjectType_FieldDefaultValue_SerializesCorrectly()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve(() => "")
                .Argument(
                    "_456",
                    a => a.Type<InputObjectType<Foo>>()
                        .DefaultValue(new Foo())));

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(objectType)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_ResolverOverrides_FieldMember()
    {
        // arrange
        var objectType = new ObjectType<Foo>(
            t => t
                .Field(f => f.Description)
                .Resolve("World"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ description }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_FuncString_Resolver()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve(() => "fooBar"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_FuncString_ResolverInferType()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Resolve(() => "fooBar"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_ConstantString_Resolver()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve("fooBar"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_ConstantString_ResolverInferType()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Resolve("fooBar"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_FuncCtxString_Resolver()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve(ctx => ctx.Selection.Field.Name));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_FuncCtxString_ResolverInferType()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Resolve(ctx => ctx.Selection.Field.Name));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_FuncCtxCtString_Resolver()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve((ctx, _) => ctx.Selection.Field.Name));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_FuncCtxCtString_ResolverInferType()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Resolve((ctx, _) => ctx.Selection.Field.Name));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_FuncObject_Resolver()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve(() => (object)"fooBar"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_ConstantObject_Resolver()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve((object)"fooBar"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_FuncCtxObject_Resolver()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve(ctx => (object)ctx.Selection.Field.Name));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_FuncCtxCtObject_Resolver()
    {
        // arrange
        var objectType = new ObjectType(
            t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve((ctx, _) => (object)ctx.Selection.Field.Name));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ _123 }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectTypeOfFoo_FuncString_Resolver()
    {
        // arrange
        var objectType = new ObjectType<Foo>(
            t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve(() => "fooBar"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ description }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectTypeOfFoo_ConstantString_Resolver()
    {
        // arrange
        var objectType = new ObjectType<Foo>(
            t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve("fooBar"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ description }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectTypeOfFoo_FuncCtxString_Resolver()
    {
        // arrange
        var objectType = new ObjectType<Foo>(
            t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve(ctx => ctx.Selection.Field.Name));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ description }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectTypeOfFoo_FuncCtxCtString_Resolver()
    {
        // arrange
        var objectType = new ObjectType<Foo>(
            t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve((ctx, _) => ctx.Selection.Field.Name));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ description }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectTypeOfFoo_FuncObject_Resolver()
    {
        // arrange
        var objectType = new ObjectType<Foo>(
            t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve(() => (object)"fooBar"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ description }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectTypeOfFoo_ConstantObject_Resolver()
    {
        // arrange
        var objectType = new ObjectType<Foo>(
            t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve((object)"fooBar"));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ description }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectTypeOfFoo_FuncCtxObject_Resolver()
    {
        // arrange
        var objectType = new ObjectType<Foo>(
            t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve(ctx => (object)ctx.Selection.Field.Name));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ description }").ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectTypeOfFoo_FuncCtxCtObject_Resolver()
    {
        // arrange
        var objectType = new ObjectType<Foo>(
            t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve((ctx, _) => (object)ctx.Selection.Field.Name));

        // act
        var executor =
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create()
                .MakeExecutable();

        // assert
        executor.Execute("{ description }").ToJson().MatchSnapshot();
    }

    [Fact]
    public async Task ObjectType_SourceTypeObject_BindsResolverCorrectly()
    {
        // arrange
        var objectType = new ObjectType(
            t => t.Name("Bar")
                .Field<FooResolver>(f => f.GetDescription(default))
                .Name("desc")
                .Type<StringType>());

        var schema = SchemaBuilder.New()
            .AddQueryType(objectType)
            .Create();

        var executor = schema.MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ desc }")
                .SetGlobalState(InitialValue, new Foo())
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public void InferInterfaceImplementation()
    {
        // arrange
        // act
        var fooType = CreateType(
            new ObjectType<Foo>(),
            b => b.AddType(new InterfaceType<IFoo>()));

        // assert
        Assert.IsType<InterfaceType<IFoo>>(
            fooType.Implements[0]);
    }

    [Fact]
    public void IgnoreFieldWithShortcut()
    {
        // arrange
        // act
        var fooType = CreateType(
            new ObjectType<Foo>(
                d =>
                {
                    d.Ignore(t => t.Description);
                    d.Field("foo").Type<StringType>().Resolve("abc");
                }));

        // assert
        Assert.Collection(
            fooType.Fields.Where(t => !t.IsIntrospectionField),
            t => Assert.Equal("foo", t.Name));
    }

    [Fact]
    public void UnIgnoreFieldWithShortcut()
    {
        // arrange
        // act
        var fooType = CreateType(
            new ObjectType<Foo>(
                d =>
                {
                    d.Ignore(t => t.Description);
                    d.Field("foo").Type<StringType>().Resolve("abc");
                    d.Field(t => t.Description).Ignore(false);
                }));

        // assert
        Assert.Collection(
            fooType.Fields.Where(t => !t.IsIntrospectionField),
            t => Assert.Equal("description", t.Name),
            t => Assert.Equal("foo", t.Name));
    }

    [Fact]
    public void IgnoreField_DescriptorIsNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action() => ObjectTypeDescriptorExtensions.Ignore<Foo>(null, t => t.Description);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void IgnoreField_ExpressionIsNull_ArgumentNullException()
    {
        // arrange
        var descriptor = new Mock<IObjectTypeDescriptor<Foo>>();

        // act
        void Action() => descriptor.Object.Ignore(null);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void DoNotAllow_InputTypes_OnFields()
    {
        // arrange
        // act
        void Action() =>
            SchemaBuilder.New()
                .AddType(
                    new ObjectType(
                        t => t.Name("Foo")
                            .Field("bar")
                            .Type<NonNullType<InputObjectType<Foo>>>()))
                .Create();

        // assert
        Assert.Throws<SchemaException>(Action)
            .Errors[0]
            .Message.MatchSnapshot();
    }

    [Fact]
    public void DoNotAllow_DynamicInputTypes_OnFields()
    {
        // arrange
        // act
        void Action() =>
            SchemaBuilder.New()
                .AddType(
                    new ObjectType(
                        t => t.Name("Foo")
                            .Field("bar")
                            .Type(new NonNullType(new InputObjectType<Foo>()))))
                .Create();

        // assert
        Assert.Throws<SchemaException>(Action)
            .Errors[0]
            .Message.MatchSnapshot();
    }

    [Fact]
    public void Support_Argument_Attributes()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Baz>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void Support_Argument_Generic_Attributes()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Baz>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }
#endif

    [Fact]
    public void Argument_Type_IsInferred_From_Parameter()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithIntArg>(
                t => t
                    .Field(f => f.GetBar(1))
                    .Argument("foo", a => a.DefaultValue(default)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Argument_Type_Cannot_Be_Inferred()
    {
        // arrange
        // act
        void Action() =>
            SchemaBuilder.New()
                .AddQueryType<QueryWithIntArg>(
                    t => t.Field(f => f.GetBar(1))
                        .Argument("bar", a => a.DefaultValue(default)))
                .Create();

        // assert
        Assert.Throws<SchemaException>(Action)
            .Errors[0]
            .Message.MatchSnapshot();
    }

    [Fact]
    public void CreateObjectTypeWithXmlDocumentation()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithDocumentation>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void CreateObjectTypeWithXmlDocumentation_IgnoreXmlDocs()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithDocumentation>()
            .ModifyOptions(options => options.UseXmlDocumentation = false)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void CreateObjectTypeWithXmlDocumentation_IgnoreXmlDocs_SchemaCreate()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithDocumentation>()
            .ModifyOptions(o => o.UseXmlDocumentation = false)
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Field_Is_Missing_Type_Throws_SchemaException()
    {
        // arrange
        // act
        void Action()
            => SchemaBuilder.New()
                .AddObjectType(
                    t => t.Name("abc")
                        .Field("def")
                        .Resolve((object)"ghi"))
                .Create();

        // assert
        Assert.Throws<SchemaException>(Action)
            .Errors.Select(t => new { t.Message, t.Code, })
            .MatchSnapshot();
    }

    [Fact]
    public void Deprecate_Obsolete_Fields()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddType(new ObjectType<FooObsolete>())
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Deprecate_Fields_With_Deprecated_Attribute()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddType(new ObjectType<FooDeprecated>())
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_From_Struct()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(new ObjectType<FooStruct>())
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Execute_With_Query_As_Struct()
    {
        // arrange
        var executor = SchemaBuilder.New()
            .AddQueryType(new ObjectType<FooStruct>())
            .Create()
            .MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.Create()
                .SetDocument("{ bar baz }")
                .SetGlobalState(
                    InitialValue,
                    new FooStruct { Qux = "Qux_Value", Baz = "Baz_Value", })
                .Build());

        // assert
        result.ToJson().MatchSnapshot();
    }

    [Fact]
    public void ObjectType_From_Dictionary()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<FooWithDict>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Infer_List_From_Queryable()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<MyListQuery>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void NonNull_Attribute_With_Explicit_Nullability_Definition()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<AnnotatedNestedList>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Infer_Non_Null_Filed()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<Bar>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Ignore_Fields_With_GraphQLIgnoreAttribute()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<FooIgnore>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Declare_Resolver_With_Result_Type_String()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("test")
                    .Resolve(
                        _ => new ValueTask<object>("abc"),
                        typeof(string)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Declare_Resolver_With_Result_Type_NativeTypeListOfInt()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("test")
                    .Resolve(
                        _ => new ValueTask<object>("abc"),
                        typeof(NativeType<List<int>>)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Declare_Resolver_With_Result_Type_ListTypeOfIntType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("test")
                    .Resolve(
                        _ => new ValueTask<object>("abc"),
                        typeof(ListType<IntType>)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Declare_Resolver_With_Result_Type_Override_ListTypeOfIntType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Resolve(
                        _ => new ValueTask<object>("abc"),
                        typeof(ListType<IntType>)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Declare_Resolver_With_Result_Type_Weak_Override_ListTypeOfIntType()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Resolve(_ => new ValueTask<object>("abc"), typeof(int)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Declare_Resolver_With_Result_Type_Is_Null()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                t => t
                    .Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Resolve(_ => new ValueTask<object>("abc"), null))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Infer_Argument_Default_Values()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<QueryWithArgumentDefaults>()
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Nested_Lists_With_Sdl_First()
    {
        SchemaBuilder.New()
            .AddDocumentFromString("type Query { some: [[Some]] } type Some { foo: String }")
            .Use(_ => _ => default)
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Nested_Lists_With_Code_First()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryWithNestedList>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public void Execute_Nested_Lists_With_Code_First()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryWithNestedList>()
            .Create()
            .MakeExecutable()
            .Execute("{ fooMatrix { baz } }")
            .ToJson()
            .MatchSnapshot();
    }

    [Fact]
    public void ResolveWith()
    {
        SchemaBuilder.New()
            .AddQueryType<ResolveWithQueryType>()
            .Create()
            .MakeExecutable()
            .Execute("{ foo baz }")
            .ToJson()
            .MatchSnapshot();
    }

    [Fact]
    public void ResolveWithAsync()
    {
        SchemaBuilder.New()
            .AddQueryType<ResolveWithQueryTypeAsync>()
            .Create()
            .MakeExecutable()
            .Execute("{ foo baz qux quux quuz }")
            .ToJson()
            .MatchSnapshot();
    }

    [Fact]
    public void ResolveWith_NonGeneric()
    {
        SchemaBuilder.New()
            .AddQueryType<ResolveWithNonGenericObjectType>()
            .Create()
            .MakeExecutable()
            .Execute("{ foo }")
            .ToJson()
            .MatchSnapshot();
    }

    [Fact]
    public void IgnoreIndexers()
    {
        SchemaBuilder.New()
            .AddQueryType<QueryWithIndexer>()
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void ObjectType_InObjectType_ThrowsSchemaException()
    {
        // arrange
        // act
        var ex = Record.Exception(
            () => SchemaBuilder
                .New()
                .AddQueryType(x => x.Name("Query").Field("Foo").Resolve("bar"))
                .AddType<ObjectType<ObjectType<Foo>>>()
                .ModifyOptions(o => o.StrictRuntimeTypeValidation = true)
                .Create());

        // assert
        Assert.IsType<SchemaException>(ex);
        ex.Message.MatchSnapshot();
    }

    [Fact]
    public void Specify_Field_Type_With_SDL_Syntax()
    {
        SchemaBuilder.New()
            .AddQueryType(
                d =>
                {
                    d.Name("Query");
                    d.Field("Foo").Type("String").Resolve(_ => null);
                })
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void Specify_Argument_Type_With_SDL_Syntax()
    {
        SchemaBuilder.New()
            .AddQueryType(
                d =>
                {
                    d.Name("Query");
                    d.Field("Foo")
                        .Argument("a", t => t.Type("Int"))
                        .Type("String")
                        .Resolve(_ => null);
                })
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public void Infer_Types_Correctly_When_Using_ResolveWith()
    {
        SchemaBuilder.New()
            .AddQueryType<InferNonNullTypesWithResolveWith>()
            .Create()
            .Print()
            .MatchSnapshot();
    }

    [Fact]
    public async Task Override_Instance_Check_With_Options()
    {
        Snapshot.FullName();

        var globalCheck = false;

        await new ServiceCollection()
            .AddGraphQL()
            .ModifyOptions(
                o => o.DefaultIsOfTypeCheck = (objectType, context, value) =>
                {
                    globalCheck = true;
                    return true;
                })
            .AddQueryType(t => t.Field("abc").Type("Foo").Resolve(new object()))
            .AddInterfaceType(t => t.Name("Foo").Field("abc").Type("String"))
            .AddObjectType(
                t => t.Name("Bar").Implements("Foo").Field("abc").Type("String").Resolve("abc"))
            .ExecuteRequestAsync("{ abc { abc } }")
            .MatchSnapshotAsync();

        Assert.True(globalCheck);
    }

    [Fact]
    public async Task AnotationBased_DeprecatedArgument_Should_BeDeprecated()
    {
        // arrangt
        Snapshot.FullName();

        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithDeprecatedArguments>()
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task AnotationBased_DeprecatedArgument_NonNullableIsDeprecated_Throw()
    {
        // arrange
        Snapshot.FullName();

        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<QueryWithDeprecatedArgumentsIllegal>()
            .BuildRequestExecutorAsync();

        // assert
        var ex = await Assert.ThrowsAsync<SchemaException>(call);
        ex.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_DeprecatedArgument_Should_BeDeprecated()
    {
        // arrange
        Snapshot.FullName();

        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x
                    .Field("foo")
                    .Argument("bar", x => x.Type<IntType>().Deprecated("Is deprecated"))
                    .Resolve(""))
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_DeprecatedArgument_NonNullableIsDeprecated_Throw()
    {
        // arrange
        Snapshot.FullName();

        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x
                    .Field("foo")
                    .Argument(
                        "bar",
                        x => x.Type<NonNullType<IntType>>().Deprecated("Is deprecated"))
                    .Resolve(""))
            .BuildRequestExecutorAsync();

        // assert
        var ex = await Assert.ThrowsAsync<SchemaException>(call);
        ex.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_DeprecatedArgument_Should_BeDeprecated()
    {
        // arrange
        Snapshot.FullName();

        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                @"
                    type Query {
                        foo(bar: String @deprecated(reason:""reason"")): Int!
                    }
                ")
            .AddResolver("Query", "foo", x => 1)
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.Print().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_DeprecatedArgument_NonNullableIsDeprecated_Throw()
    {
        // arrange
        Snapshot.FullName();

        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(
                @"
                    type Query {
                        foo(bar: String! @deprecated(reason:""reason"")): Int!
                    }
                ")
            .AddResolver("Query", "foo", x => 1)
            .BuildRequestExecutorAsync();

        // assert
        var ex = await Assert.ThrowsAsync<SchemaException>(call);
        ex.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public async Task Static_Field_Inference_1()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<WithStaticField>(d => d.BindFields(Instance | Static))
                .BuildSchemaAsync();

        // assert
        SnapshotExtensions.MatchSnapshot(schema);
    }

    [Fact]
    public async Task Static_Field_Inference_2()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<WithStaticField2>()
                .BuildSchemaAsync();

        // assert
        SnapshotExtensions.MatchSnapshot(schema);
    }


    [Fact]
    public async Task Static_Field_Inference_3()
    {
        // arrange
        // act
        async Task Error() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<WithStaticField>()
                .ModifyOptions(o => o.DefaultBindingBehavior = BindingBehavior.Explicit)
                .BuildSchemaAsync();

        // assert
        await Assert.ThrowsAsync<SchemaException>(Error);
    }

    [Fact]
    public async Task Static_Field_Inference_4()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<WithStaticField>()
                .ModifyOptions(
                    o =>
                    {
                        o.DefaultBindingBehavior = BindingBehavior.Explicit;
                        o.DefaultFieldBindingFlags = Instance | Static;
                    })
                .BuildSchemaAsync();

        // assert
        SnapshotExtensions.MatchSnapshot(schema);
    }

    [Fact]
    public async Task Static_Field_Inference_4_Execute()
    {
        // arrange
        // act
        var result =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<WithStaticField>()
                .ModifyOptions(
                    o =>
                    {
                        o.DefaultBindingBehavior = BindingBehavior.Explicit;
                        o.DefaultFieldBindingFlags = Instance | Static;
                    })
                .ExecuteRequestAsync("{ hello staticHello }");

        // assert
        SnapshotExtensions.MatchSnapshot(result);
    }

    [Fact]
    public async Task Static_Field_Inference_5()
    {
        // arrange
        // act
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType()
                .AddTypeExtension(typeof(BookQuery))
                .ModifyOptions(o => o.DefaultFieldBindingFlags = InstanceAndStatic)
                .BuildSchemaAsync();

        // assert
        SnapshotExtensions.MatchSnapshot(schema);
    }

    [Fact]
    public void ResolverWithAbstractBase_ShouldResolve()
    {
        var result = SchemaBuilder.New()
            .AddQueryType(
                d => d
                    .Field("value")
                    .ResolveWith<ResolverWithAbstractBase>(x => x.GetValue())
                )
            .Create()
            .MakeExecutable()
            .Execute("{ value }")
            .ToJson();

        SnapshotExtensions.MatchInlineSnapshot(result,
        """
        {
            "data": {
                "value": 1024
            }
        }
        """);
    }

    [Fact]
    public void AbstractResolver_ShouldThrow()
    {
        var ex = Record.Exception(() => SchemaBuilder.New()
            .AddQueryType(d => d.Field("value").ResolveWith<ResolverBase>(x => x.GetValue()))
            .Create());

        Assert.IsType<SchemaException>(ex);
        Assert.Contains("non-abstract type is required", ex.Message);
    }

    [Fact]
    public void AbstractResolver_UsingMethodInfo_ShouldThrow()
    {
        var method = typeof(ResolverBase).GetMethod(nameof(ResolverBase.GetValue))!;

        var ex = Record.Exception(() => SchemaBuilder.New()
            .AddQueryType(d => d.Field("value").ResolveWith(method))
            .Create());

        Assert.IsType<SchemaException>(ex);
        Assert.Contains("non-abstract type is required", ex.Message);
    }

    [Fact]
    public async Task Ignore_Generic_Methods()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<QueryWithGenerics>()
                .BuildSchemaAsync();

        SnapshotExtensions.MatchSnapshot(schema);
    }

    public abstract class ResolverBase
    {
        public int GetValue() => 1024;
    }

    public class ResolverWithAbstractBase : ResolverBase
    {
    }

    public class GenericFoo<T>
    {
        public T Value { get; }
    }

    public class Foo
        : IFoo
    {
        public Foo() { }

        public Foo(string description)
        {
            Description = description;
        }

        public string Description { get; } = "hello";
    }

    public interface IFoo
    {
        string Description { get; }
    }

    public class FooResolver
    {
        public string GetBar(string foo) => "hello foo";

        public string GetDescription([Parent] Foo foo) => foo.Description;
    }

    public class QueryWithIntArg
    {
        public string GetBar(int foo) => "hello foo";
    }

#nullable enable
    public class Bar
    {
        [GraphQLNonNullType] public string Baz { get; set; } = default!;
    }
#nullable disable

    public class Baz
    {
        public string Qux(
            [GraphQLName("arg2")] [GraphQLDescription("argdesc")] [GraphQLNonNullType]
            string arg) => arg;

        public string Quux([GraphQLType(typeof(ListType<StringType>))] string arg) => arg;
    }

#if NET6_0_OR_GREATER
    public class Baz2
    {
        public string Qux(
            [GraphQLName("arg2")] [GraphQLDescription("argdesc")] [GraphQLNonNullType]
            string arg) => arg;

        public string Quux([GraphQLType<ListType<StringType>>] string arg) => arg;
    }
#endif

    public class FooType
        : ObjectType<Foo>
    {
        protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Description);
            descriptor.Field("test")
                .Resolve(() => new List<string>())
                .Type<ListType<StringType>>();
        }
    }

    public class FooObsolete
    {
        [Obsolete("Baz")]
        public string Bar() => "foo";
    }

    public class FooIgnore
    {
        [GraphQLIgnore]
        public string Bar() => "foo";

        public string Baz() => "foo";
    }

    public class FooDeprecated
    {
        [GraphQLDeprecated("Use Bar2.")]
        public string Bar() => "foo";

        public string Bar2() => "Foo 2: Electric foo-galoo";
    }

    public struct FooStruct
    {
        // should be ignored by the automatic field
        // inference.
        public string Qux;

        // should be included by the automatic field
        // inference.
        public string Baz { get; set; }

        // should be ignored by the automatic field
        // inference since we cannot determine what object means
        // in the graphql context.
        // This field has to be included explicitly.
        public object Quux { get; set; }

        // should be included by the automatic field
        // inference.
        public string GetBar() => Qux + "_Bar_Value";
    }

    public class FooWithDict
    {
        public Dictionary<string, Bar> Map { get; set; }
    }

    public class MyList
        : MyListBase;

    public class MyListBase
        : IQueryable<Bar>
    {
        public Type ElementType => throw new NotImplementedException();

        public Expression Expression => throw new NotImplementedException();

        public IQueryProvider Provider => throw new NotImplementedException();

        public IEnumerator<Bar> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class MyListQuery
    {
        public MyList List { get; set; }
    }

    public class FooWithNullable
    {
        public bool? Bar { get; set; }

        public List<bool?> Bars { get; set; }
    }

    public class QueryWithArgumentDefaults
    {
        public string Field1(
            string a = null,
            string b = "abc") => null;

        public string Field2(
            [DefaultValue(null)] string a,
            [DefaultValue("abc")] string b) => null;
    }

    [ExtendObjectType("Some")]
    public class SomeTypeExtensionWithInterface : INode
    {
        [GraphQLType(typeof(NonNullType<IdType>))]
        public string Id { get; }
    }

    public class QueryWithNestedList
    {
        public List<List<FooIgnore>> FooMatrix =>
            [[new(),],];
    }

    public class ResolveWithQuery
    {
        public int Foo { get; set; } = 123;
    }

    public class ResolveWithQueryResolver
    {
        public string Bar { get; set; } = "Bar";

        public Task<string> FooAsync() => Task.FromResult("Foo");

        public Task<bool> BarAsync(IResolverContext context)
            => Task.FromResult(context is not null);
    }

    public class ResolveWithQueryType : ObjectType<ResolveWithQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<ResolveWithQuery> descriptor)
        {
            descriptor.Field(t => t.Foo).ResolveWith<ResolveWithQueryResolver>(t => t.Bar);
            descriptor.Field("baz").ResolveWith<ResolveWithQueryResolver>(t => t.Bar);
        }
    }

    public class ResolveWithQueryTypeAsync : ObjectType<ResolveWithQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<ResolveWithQuery> descriptor)
        {
            descriptor.Field(t => t.Foo).ResolveWith<ResolveWithQueryResolver>(t => t.FooAsync());
            descriptor.Field("baz").ResolveWith<ResolveWithQueryResolver>(t => t.FooAsync());

            descriptor.Field("qux").ResolveWith<ResolveWithQueryResolver, string>(t => t.Bar);
            descriptor.Field("quux")
                .ResolveWith<ResolveWithQueryResolver, string>(t => t.FooAsync());

            descriptor.Field("quuz")
                .ResolveWith<ResolveWithQueryResolver, bool>(t => t.BarAsync(default));
        }
    }

    public class ResolveWithNonGenericObjectType : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            var type = typeof(ResolveWithQuery);

            descriptor.Name("ResolveWithQuery");
            descriptor.Field("foo").Type<IntType>().ResolveWith(type.GetProperty("Foo")!);
        }
    }

    public class AnnotatedNestedList
    {
        [GraphQLNonNullType(true, false, false)]
        public List<List<string>> NestedList { get; set; }
    }

    public class QueryWithIndexer
    {
        public string this[int i]
        {
            get => throw new NotImplementedException();
        }

        public string GetFoo() => throw new NotImplementedException();
    }

#nullable enable

    public class InferNonNullTypesWithResolveWith : ObjectType
    {
        protected override void Configure(IObjectTypeDescriptor descriptor)
        {
            descriptor.Name("Query");

            descriptor
                .Field("foo")
                .ResolveWith<InferNonNullTypesWithResolveWithResolvers>(t => t.Foo);

            descriptor
                .Field("bar")
                .ResolveWith<InferNonNullTypesWithResolveWithResolvers>(t => t.Bar);
        }
    }

    public class InferNonNullTypesWithResolveWithResolvers
    {
        public string? Foo => "Foo";

        public string Bar => "Bar";
    }

    public class QueryWithDeprecatedArguments
    {
        public string Field([GraphQLDeprecated("Not longer allowed")] string? deprecated) => "";
    }

    public class QueryWithDeprecatedArgumentsIllegal
    {
        public string Field([GraphQLDeprecated("Not longer allowed")] int deprecated) => "";
    }

    public class WithStaticField
    {
        public static string StaticHello() => "hello";

        public string Hello() => "hello";
    }

    [ObjectType(IncludeStaticMembers = true)]
    public class WithStaticField2
    {
        public static string StaticHello() => "hello";

        public string Hello() => "hello";
    }

    [ExtendObjectType(OperationType.Query)]
    public static class BookQuery
    {
        public static Book GetBook()
            => new Book();
    }

    public class Book
    {
        public int Id => default;

        public string Title { get; set; } = default!;

        public static bool IsComic => true;
    }

    public class QueryWithGenerics
    {
        public string Bar() => "bar";

        public T Foo<T>() => default!;
    }
}
