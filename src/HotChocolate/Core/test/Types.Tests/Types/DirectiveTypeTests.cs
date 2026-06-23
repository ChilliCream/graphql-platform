using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class DirectiveTypeTests : TypeTestBase
{
    [Fact]
    public void ConfigureTypedDirectiveWithResolver()
    {
        // arrange
        // act
        DirectiveType directiveType =
            CreateDirective(new CustomDirectiveType());

        // assert
        Assert.NotNull(directiveType.Middleware);
        Assert.Equal(typeof(CustomDirective), directiveType.RuntimeType);
        Assert.Collection(
            directiveType.Arguments,
            t => Assert.Equal("argument", t.Name));
    }

    [Fact]
    public void ConfigureTypedDirective_DefaultBinding_Explicit()
    {
        // arrange
        // act
        DirectiveType directiveType =
            CreateDirective(
                new CustomDirectiveType(),
                b => b.ModifyOptions(
                    o =>
                        o.DefaultBindingBehavior = BindingBehavior.Explicit));

        // assert
        Assert.NotNull(directiveType.Middleware);
        Assert.Equal(typeof(CustomDirective), directiveType.RuntimeType);
        Assert.Empty(directiveType.Arguments);
    }

    [Fact]
    public void ConfigureTypedDirectiveNoArguments()
    {
        // arrange
        // act
        DirectiveType directiveType =
            CreateDirective(new Custom2DirectiveType());

        // assert
        Assert.NotNull(directiveType.Middleware);
        Assert.Equal(typeof(CustomDirective), directiveType.RuntimeType);
        Assert.Empty(directiveType.Arguments);
    }

    [Fact]
    public void ConfigureDirectiveWithResolver()
    {
        // arrange
        var directiveType = new DirectiveType(
            t => t
                .Name("Foo")
                .Location(DirectiveLocation.Field)
                .Use((_, _) => _ => default));

        // act
        directiveType = CreateDirective(directiveType);

        // assert
        Assert.NotNull(directiveType.Middleware);
        Assert.Equal(typeof(object), directiveType.RuntimeType);
    }

    [Fact]
    public void ConfigureIsNull()
    {
        // act
        void Action() => new DirectiveType(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void NoName()
    {
        // act
        void Action() => CreateDirective(new DirectiveType(_ => { }));

        // assert
        Assert.Throws<SchemaException>(Action);
    }

    [Fact]
    public void RepeatableDirective()
    {
        // arrange
        var directiveType = new DirectiveType(
            t => t.Name("foo")
                .Repeatable()
                .Location(DirectiveLocation.Object)
                .Argument("a").Type<StringType>());

        var objectType = new ObjectType(
            t =>
            {
                t.Name("Bar");
                t.Directive("foo", new ArgumentNode("a", "1"));
                t.Directive("foo", new ArgumentNode("a", "2"));
                t.Field("foo").Resolve(() => "baz");
            });

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(objectType)
            .AddDirectiveType(directiveType)
            .Create();

        // assert
        Assert.Collection(
            schema.Types.GetType<ObjectType>("Bar").Directives,
            t =>
            {
                Assert.Equal("foo", t.Type.Name);
                Assert.Equal("1", t.GetArgumentValue<string>("a"));
            },
            t =>
            {
                Assert.Equal("foo", t.Type.Name);
                Assert.Equal("2", t.GetArgumentValue<string>("a"));
            });
    }

    [Fact]
    public void UniqueDirective()
    {
        // arrange
        var directiveType = new DirectiveType(
            t => t.Name("foo")
                .Location(DirectiveLocation.Object)
                .Argument("a").Type<StringType>());

        var objectType = new ObjectType(
            t =>
            {
                t.Name("Bar");
                t.Directive("foo", new ArgumentNode("a", "1"));
                t.Directive("foo", new ArgumentNode("a", "2"));
                t.Field("foo").Resolve(() => "baz");
            });

        // act
        void Action() =>
            SchemaBuilder.New()
                .AddQueryType(objectType)
                .AddDirectiveType(directiveType)
                .Create();

        // assert
        var exception = Assert.Throws<SchemaException>(Action);
        Assert.Collection(
            exception.Errors,
            t =>
            {
                Assert.Equal(
                    "The specified directive `@foo` "
                    + "is unique and cannot be added twice.",
                    t.Message);
            });
    }

    [Fact]
    public void ExecutableUniqueDirectives()
    {
        // arrange
        var directiveType = new DirectiveType(
            t => t.Name("foo")
                .Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.FieldDefinition)
                .Use((_, _) => _ => default)
                .Argument("a").Type<StringType>());

        var objectType = new ObjectType(
            t =>
            {
                t.Name("Bar");
                t.Directive("foo", new ArgumentNode("a", "1"));
                t.Field("foo").Resolve(() => "baz")
                    .Directive("foo", new ArgumentNode("a", "2"));
            });

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(objectType)
            .AddDirectiveType(directiveType)
            .Create();

        // assert
        IReadOnlyCollection<Directive> collection =
            schema.Types.GetType<ObjectType>("Bar")
                .Fields["foo"].Directives
                .Where(t => t.Type.Middleware is not null)
                .ToList();

        Assert.Collection(
            collection,
            t =>
            {
                Assert.Equal("foo", t.Type.Name);
                Assert.Equal("2", t.GetArgumentValue<string>("a"));
            });
    }

    [Fact]
    public void Ignore_DescriptorIsNull_ArgumentNullException()
    {
        // arrange
        // act
        Action action = () =>
            DirectiveTypeDescriptorExtensions
                .Ignore<CustomDirective2>(null!, t => t.Argument2);

        // assert
        Assert.Throws<NullReferenceException>(action);
    }

    [Fact]
    public void Ignore_ExpressionIsNull_ArgumentNullException()
    {
        // arrange
        var descriptor =
            DirectiveTypeDescriptor.New<CustomDirective2>(
                DescriptorContext.Create());

        // act
        void Action() => descriptor.Ignore(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void Ignore_Argument2_Property()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddDirectiveType(
                new DirectiveType<CustomDirective2>(
                    d => d
                        .Name("foo")
                        .Location(DirectiveLocation.Object)
                        .Ignore(t => t.Argument2)))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Use_DelegateMiddleware()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddDirectiveType(
                new DirectiveType<CustomDirective2>(
                    d => d
                        .Name("foo")
                        .Location(DirectiveLocation.Object)
                        .Use((_, _) => _ => default)))
            .Create();

        // assert
        var directive = schema.DirectiveTypes["foo"];
        Assert.NotNull(directive.Middleware);
    }

    [Fact]
    public async Task Use_EnsureClassMiddlewareDoesNotTrap_Next()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                descriptor =>
                {
                    descriptor
                        .Name("Query");

                    descriptor
                        .Field("foo")
                        .Type<StringType>()
                        .Resolve("bar")
                        .Directive("foo");

                    descriptor
                        .Field("foo1")
                        .Type<IntType>()
                        .Resolve(1)
                        .Directive("foo");
                })
            .AddDirectiveType(
                new DirectiveType<CustomDirective2>(
                    d => d
                        .Name("foo")
                        .Location(DirectiveLocation.FieldDefinition)
                        .Use<DirectiveMiddleware1>()))
            .Create();

        // assert
        var directive = schema.DirectiveTypes["foo"];
        Assert.NotNull(directive.Middleware);

        await schema.MakeExecutable().ExecuteAsync("{ foo }", TestContext.Current.CancellationToken);
        await schema.MakeExecutable().ExecuteAsync(
            "{ foo1 }",
            TestContext.Current.CancellationToken);
        await schema.MakeExecutable().ExecuteAsync(
            "{ foo foo1 }",
            TestContext.Current.CancellationToken);
        var result = await schema.MakeExecutable().ExecuteAsync(
            "{ foo foo1 }",
            TestContext.Current.CancellationToken);

        result.MatchSnapshot();
    }

    [Fact]
    public void Use_ClassMiddleware()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddDirectiveType(
                new DirectiveType<CustomDirective2>(
                    d => d
                        .Name("foo")
                        .Location(DirectiveLocation.Object)
                        .Use<DirectiveMiddleware>()))
            .Create();

        // assert
        var directive = schema.DirectiveTypes["foo"];
        Assert.NotNull(directive.Middleware);
    }

    [Fact]
    public void Use_ClassMiddleware_WithFactory()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddDirectiveType(
                new DirectiveType<CustomDirective2>(
                    d => d
                        .Name("foo")
                        .Location(DirectiveLocation.Object)
                        .Use((_, next) => new DirectiveMiddleware(next))))
            .Create();

        // assert
        var directive = schema.DirectiveTypes["foo"];
        Assert.NotNull(directive.Middleware);
    }

    [Fact]
    public void Use_ClassMiddleware_WithFactoryNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action() =>
            SchemaBuilder.New()
                .AddQueryType(
                    c => c.Name("Query")
                        .Directive("foo")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolve("bar"))
                .AddDirectiveType(
                    new DirectiveType<CustomDirective2>(
                        d => d.Name("foo")
                            .Location(DirectiveLocation.Object)
                            .Use(null)))
                .Create();

        // assert
        Assert.Throws<SchemaException>(Action);
    }

    [Fact]
    public void Use2_DelegateMiddleware()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddDirectiveType(
                d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use((_, _) => _ => default))
            .Create();

        // assert
        var directive = schema.DirectiveTypes["foo"];
        Assert.NotNull(directive.Middleware);
    }

    [Fact]
    public void Use2_ClassMiddleware()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddDirectiveType(
                d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use<DirectiveMiddleware>())
            .Create();

        // assert
        var directive = schema.DirectiveTypes["foo"];
        Assert.NotNull(directive.Middleware);
    }

    [Fact]
    public void Use2_ClassMiddleware_WithFactory()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddDirectiveType(
                d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use((_, next) => new DirectiveMiddleware(next)))
            .Create();

        // assert
        var directive = schema.DirectiveTypes["foo"];
        Assert.NotNull(directive.Middleware);
    }

    [Fact]
    public void Use2_ClassMiddleware_WithFactoryNull_ArgumentNullException()
    {
        // arrange
        // act
        static void Action()
            => SchemaBuilder.New()
                .AddQueryType(
                    c => c.Name("Query")
                        .Directive("foo")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolve("bar"))
                .AddDirectiveType(
                    d => d.Name("foo")
                        .Location(DirectiveLocation.Object)
                        .Use(null!))
                .Create();

        // assert
        Assert.Throws<SchemaException>(Action);
    }

    [Fact]
    public void Infer_Directive_Argument_Defaults_From_Properties()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(
                c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
            .AddDirectiveType(
                new DirectiveType<DirectiveWithDefaults>(
                    d => d
                        .Name("foo")
                        .Location(DirectiveLocation.Object)
                        .Use<DirectiveMiddleware>()))
            .Create();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public void Specify_Argument_Type_With_SDL_Syntax()
    {
        SchemaBuilder.New()
            .AddDirectiveType<DirectiveWithSyntaxTypeArg>()
            .ModifyOptions(o => o.StrictValidation = false)
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public async Task AnnotationBased_Deprecated_NullableArguments_Valid()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("bar")
                    .Resolve("asd")
                    .Directive<Deprecated2Directive>())
            .AddDirectiveType(
                new DirectiveType<Deprecated2Directive>(
                    x => x.Location(DirectiveLocation.FieldDefinition)))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotationBased_DeprecatedInputTypes_NonNullableField_Invalid()
    {
        // arrange
        // act
        static async Task Call() =>
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    x => x
                        .Name("Query")
                        .Field("bar")
                        .Resolve("asd")
                        .Directive<DeprecatedNonNull>())
                .AddDirectiveType(
                    new DirectiveType<DeprecatedNonNull>(
                        x => x.Location(DirectiveLocation.FieldDefinition)))
                .BuildRequestExecutorAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(Call);
        exception.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_Deprecated_NullableArguments_Valid()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("bar")
                    .Resolve("asd")
                    .Directive("Qux"))
            .AddDirectiveType(
                x => x
                    .Name("Qux")
                    .Location(DirectiveLocation.FieldDefinition)
                    .Argument("bar")
                    .Type<IntType>()
                    .Deprecated("a"))
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_DeprecatedInputTypes_NonNullableField_Invalid()
    {
        // arrange
        // act
        static async Task call()
            => await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    x => x
                        .Name("Query")
                        .Field("bar")
                        .Resolve("asd")
                        .Directive("Qux", new ArgumentNode("bar", 1)))
                .AddDirectiveType(
                    x => x
                        .Name("Qux")
                        .Location(DirectiveLocation.FieldDefinition)
                        .Argument("bar")
                        .Type<NonNullType<IntType>>()
                        .Deprecated("a"))
                .BuildRequestExecutorAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(call);
        exception.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_DeprecatedDirective_NullableFields_Valid()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("bar")
                    .Resolve("asd")
                    .Directive("Qux"))
            .AddDocumentFromString(
                """
                directive @Qux(bar: String @deprecated(reason: "reason")) on FIELD_DEFINITION
                """)
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_DeprecatedDirective_NonNullableField_Invalid()
    {
        // arrange
        // act
        static async Task call()
            => await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType(
                    x => x
                        .Name("Query")
                        .Field("bar")
                        .Resolve("asd")
                        .Directive("Qux", new ArgumentNode("bar", "abc")))
                .AddDocumentFromString(
                    """
                    directive @Qux(bar: String! @deprecated(reason: "reason")) on FIELD_DEFINITION
                    """)
                .BuildSchemaAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(call);
        exception.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public void Directive_ValidateArgs_InvalidArg()
    {
        // arrange
        const string sourceText = @"
            type Query {
                foo: String @a(d:1 e:true)
            }

            directive @a(c:Int d:Int! e:Int) on FIELD_DEFINITION";

        // act
        void Action() =>
            SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .AddResolver("Query", "foo", "bar")
                .Create();

        // assert
        var errors = Assert.Throws<SchemaException>(Action).Errors;
        Assert.Single(errors);
        Assert.Equal(ErrorCodes.Schema.InvalidArgument, errors[0].Code);
        errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void Directive_ValidateArgs_ArgMissing()
    {
        // arrange
        const string sourceText = @"
            type Query {
                foo: String @a
            }

            directive @a(c:Int d:Int! e:Int) on FIELD_DEFINITION";

        // act
        void Action() =>
            SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .AddResolver("Query", "foo", "bar")
                .Create();

        // assert
        var errors = Assert.Throws<SchemaException>(Action).Errors;
        Assert.Single(errors);
        Assert.Equal(ErrorCodes.Schema.InvalidArgument, errors[0].Code);
        errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void Directive_ValidateArgs_NonNullArgIsNull()
    {
        // arrange
        const string sourceText = @"
            type Query {
                foo: String @a(d: null)
            }

            directive @a(c:Int d:Int! e:Int) on FIELD_DEFINITION";

        // act
        void Action() =>
            SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .AddResolver("Query", "foo", "bar")
                .Create();

        // assert
        var errors = Assert.Throws<SchemaException>(Action).Errors;
        Assert.Single(errors);
        Assert.Equal(ErrorCodes.Schema.InvalidArgument, errors[0].Code);
        errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public void Directive_ValidateArgs_Overflow()
    {
        // arrange
        var sourceText =
            $$"""
            type Query {
               foo: String @a(d: {{long.MaxValue}})
            }

            directive @a(c:Int d:Int! e:Int) on FIELD_DEFINITION
            """;

        // act
        void Action() =>
            SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .AddResolver("Query", "foo", "bar")
                .Create();

        // assert
        var errors = Assert.Throws<SchemaException>(Action).Errors;
        Assert.Single(errors);
        Assert.Equal(ErrorCodes.Schema.InvalidArgument, errors[0].Code);
        errors[0].Message.MatchSnapshot();
    }

    [Fact]
    public async Task Directive_ArgumentDirective_AddedToSchema()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("bar")
                    .Resolve("asd")
                    .Directive("Qux"))
            .AddDocumentFromString(
                """
                directive @Example on ARGUMENT_DEFINITION
                directive @Qux(bar: String @Example) on FIELD_DEFINITION
                """)
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.True(
            schema.DirectiveTypes
                .Single(d => d.Name == "Qux")
                .Arguments["bar"]
                .Directives[0].Type.Name == "Example");
    }

    [Fact]
    public async Task AnnotationBased_Directive()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("bar")
                    .Resolve("asd")
                    .Directive("anno"))
            .AddType<AnnotationDirective>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotationBased_Directive_InferName()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("bar")
                    .Resolve("asd")
                    .Directive("foo"))
            .AddType<FooDirective>()
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotationBased_Directive_InferDirectiveType()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(
                x => x
                    .Name("Query")
                    .Field("bar")
                    .Resolve("asd")
                    .Directive(new FooDirective("abc")))
            .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        // assert
        schema.MatchSnapshot();
    }

    [Fact]
    public void DirectiveType_WithDirectivesAndDeprecation_CompletesCollection()
    {
        // arrange
        var onDirectiveDefinition = new DirectiveType(d => d
            .Name("onDirectiveDefinition")
            .Location(DirectiveLocation.DirectiveDefinition));

        var customConfiguration = new DirectiveTypeConfiguration("custom")
        {
            Locations = DirectiveLocation.Object,
            DeprecationReason = "Use something else."
        };
        customConfiguration.Directives.Add(
            new DirectiveConfiguration(new DirectiveNode("onDirectiveDefinition")));

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("custom")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(onDirectiveDefinition)
            .AddDirectiveType(DirectiveType.CreateUnsafe(customConfiguration))
            .Create();

        // assert
        var custom = schema.DirectiveTypes["custom"];
        Assert.True(custom.IsDeprecated);
        Assert.Equal("Use something else.", custom.DeprecationReason);
        var directive = Assert.Single(custom.Directives);
        Assert.Equal("onDirectiveDefinition", directive.Name);
    }

    [Fact]
    public void DirectiveType_DirectiveWithoutDirectiveDefinitionLocation_Errors()
    {
        // arrange
        var onObject = new DirectiveType(d => d
            .Name("onObject")
            .Location(DirectiveLocation.Object));

        var customConfiguration = new DirectiveTypeConfiguration("custom")
        {
            Locations = DirectiveLocation.Object
        };
        customConfiguration.Directives.Add(
            new DirectiveConfiguration(new DirectiveNode("onObject")));

        // act
        void Action() => SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(onObject)
            .AddDirectiveType(DirectiveType.CreateUnsafe(customConfiguration))
            .Create();

        // assert
        var exception = Assert.Throws<SchemaException>(Action);
        exception.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public void DeprecatedDirective_DeclaresDirectiveDefinitionLocation()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .Create();

        // act
        var deprecated = schema.DirectiveTypes[DirectiveNames.Deprecated.Name];

        // assert
        Assert.True(deprecated.Locations.HasFlag(DirectiveLocation.DirectiveDefinition));
    }

    [Fact]
    public void CodeFirst_DeprecatedAndDirectives_AppliedToDirectiveType()
    {
        // arrange
        var onDirectiveDefinition = new DirectiveType(d => d
            .Name("onDirectiveDefinition")
            .Location(DirectiveLocation.DirectiveDefinition));

        var custom = new DirectiveType(d => d
            .Name("custom")
            .Location(DirectiveLocation.Object)
            .Deprecated("Use something else.")
            .Directive("onDirectiveDefinition"));

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("custom")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(onDirectiveDefinition)
            .AddDirectiveType(custom)
            .Create();

        // assert
        var customType = schema.DirectiveTypes["custom"];
        Assert.True(customType.IsDeprecated);
        Assert.Equal("Use something else.", customType.DeprecationReason);
        var directive = Assert.Single(customType.Directives);
        Assert.Equal("onDirectiveDefinition", directive.Name);
    }

    [Fact]
    public void CodeFirst_GenericDirectiveType_DeprecatedAndDirectives_Applied()
    {
        // arrange
        var onDirectiveDefinition = new DirectiveType(d => d
            .Name("onDirectiveDefinition")
            .Location(DirectiveLocation.DirectiveDefinition));

        var custom = new DirectiveType<MarkerDirective>(d => d
            .Name("custom")
            .Location(DirectiveLocation.Object)
            .Deprecated("Use something else.")
            .Directive("onDirectiveDefinition"));

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("custom")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(onDirectiveDefinition)
            .AddDirectiveType(custom)
            .Create();

        // assert
        var customType = schema.DirectiveTypes["custom"];
        Assert.True(customType.IsDeprecated);
        Assert.Equal("Use something else.", customType.DeprecationReason);
        var directive = Assert.Single(customType.Directives);
        Assert.Equal("onDirectiveDefinition", directive.Name);
    }

    [Fact]
    public void CodeFirst_DirectiveType_TypedDirectiveInstance_Applied()
    {
        // arrange
        var marker = new DirectiveType<MarkerDirective>(d => d
            .Name("marker")
            .Location(DirectiveLocation.DirectiveDefinition));

        var custom = new DirectiveType(d => d
            .Name("custom")
            .Location(DirectiveLocation.Object)
            .Directive(new MarkerDirective()));

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("custom")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(marker)
            .AddDirectiveType(custom)
            .Create();

        // assert
        var customType = schema.DirectiveTypes["custom"];
        var directive = Assert.Single(customType.Directives);
        Assert.Equal("marker", directive.Name);
    }

    [Fact]
    public void CodeFirst_DirectiveType_TypedDirectiveByType_Applied()
    {
        // arrange
        var marker = new DirectiveType<MarkerDirective>(d => d
            .Name("marker")
            .Location(DirectiveLocation.DirectiveDefinition));

        var custom = new DirectiveType(d => d
            .Name("custom")
            .Location(DirectiveLocation.Object)
            .Directive<MarkerDirective>());

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("custom")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(marker)
            .AddDirectiveType(custom)
            .Create();

        // assert
        var customType = schema.DirectiveTypes["custom"];
        var directive = Assert.Single(customType.Directives);
        Assert.Equal("marker", directive.Name);
    }

    [Fact]
    public void CodeFirst_DirectiveType_DeprecatedNoReason_UsesDefault()
    {
        // arrange
        var custom = new DirectiveType(d => d
            .Name("custom")
            .Location(DirectiveLocation.Object)
            .Deprecated());

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("custom")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(custom)
            .Create();

        // assert
        var customType = schema.DirectiveTypes["custom"];
        Assert.True(customType.IsDeprecated);
        Assert.Equal(
            DirectiveNames.Deprecated.Arguments.DefaultReason,
            customType.DeprecationReason);
    }

    [Fact]
    public void AnnotationBased_ObsoleteDirectiveClass_SetsDeprecation()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("outdated")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
#pragma warning disable CS0618 // Type is obsolete; obsolescence is the scenario under test.
            .AddDirectiveType(new DirectiveType<OutdatedDirective>(
                d => d.Location(DirectiveLocation.Object)))
#pragma warning restore CS0618
            .Create();

        // assert
        var outdated = schema.DirectiveTypes["outdated"];
        Assert.True(outdated.IsDeprecated);
        Assert.Equal("Use the replacement directive.", outdated.DeprecationReason);
    }

    [Fact]
    public void AnnotationBased_GraphQLDeprecatedDirectiveClass_SetsDeprecation()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("legacy")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(new DirectiveType<LegacyDirective>(
                d => d.Location(DirectiveLocation.Object)))
            .Create();

        // assert
        var legacy = schema.DirectiveTypes["legacy"];
        Assert.True(legacy.IsDeprecated);
        Assert.Equal("Use the replacement directive.", legacy.DeprecationReason);
    }

    [Fact]
    public void DirectiveArgument_AppliesDirective()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(d => d
                .Name("foo")
                .Location(DirectiveLocation.Field)
                .Argument("arg", a => a
                    .Type<IntType>()
                    .Directive("deprecated")))
            .Create();

        // assert
        var argument = schema.DirectiveTypes["foo"].Arguments["arg"];
        Assert.True(argument.Directives.ContainsDirective("deprecated"));
    }

    public class DirectiveWithSyntaxTypeArg : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("bar");
            descriptor.Location(DirectiveLocation.Field);
            descriptor.Argument("a").Type("Int");
        }
    }

    public class CustomDirectiveType : DirectiveType<CustomDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<CustomDirective> descriptor)
        {
            descriptor.Name("Custom");
            descriptor.Location(DirectiveLocation.Enum);
            descriptor.Location(DirectiveLocation.Field);
            descriptor.Use((_, _) => _ => default);
        }
    }

    public class Custom2DirectiveType : DirectiveType<CustomDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<CustomDirective> descriptor)
        {
            descriptor.Name("Custom");
            descriptor.Location(DirectiveLocation.Enum);
            descriptor.Location(DirectiveLocation.Field);
            descriptor.Use((_, _) => _ => default);
            descriptor.BindArgumentsImplicitly().BindArgumentsExplicitly();
        }
    }

    public class DirectiveMiddleware
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly FieldDelegate _next;
#pragma warning restore IDE0052 // Remove unread private members

        public DirectiveMiddleware(FieldDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(IMiddlewareContext context) =>
            Task.CompletedTask;
    }

    public class DirectiveMiddleware1
    {
        private readonly FieldDelegate _next;
        private static int s_instances;
        private readonly int _count;

        public DirectiveMiddleware1(FieldDelegate next)
        {
            _next = next;
            _count = Interlocked.Increment(ref s_instances);
        }

        public async Task InvokeAsync(IMiddlewareContext context)
        {
            await _next(context);
            context.OperationResult.SetExtension("_" + _count, _count);
        }
    }

    public class Deprecated2Directive
    {
        [Obsolete("reason")] public int? ObsoleteWithReason { get; set; }

        [Obsolete] public int? Obsolete { get; set; }

        [GraphQLDeprecated("reason")] public int? Deprecated { get; set; }
    }

    public class DeprecatedNonNull
    {
        [Obsolete("reason")] public int ObsoleteWithReason { get; set; }
    }

    public class CustomDirective
    {
        public required string Argument { get; set; }
    }

    public class CustomDirective2
    {
        public string? Argument1 { get; set; }

        public string? Argument2 { get; set; }
    }

    public class DirectiveWithDefaults
    {
        [DefaultValue("abc")] public required string Argument1 { get; set; }

        public string? Argument2 { get; set; }
    }

    [DirectiveType("anno", DirectiveLocation.FieldDefinition)]
    public sealed class AnnotationDirective
    {
        public AnnotationDirective(string? foo)
        {
            Foo = foo;
        }

        public string? Foo { get; }
    }

    [DirectiveType(DirectiveLocation.FieldDefinition)]
    public sealed class FooDirective
    {
        public FooDirective(string? foo)
        {
            Foo = foo;
        }

        public string? Foo { get; }
    }

    public sealed class MarkerDirective;

    [Obsolete("Use the replacement directive.")]
    public sealed class OutdatedDirective;

    [GraphQLDeprecated("Use the replacement directive.")]
    public sealed class LegacyDirective;
}
