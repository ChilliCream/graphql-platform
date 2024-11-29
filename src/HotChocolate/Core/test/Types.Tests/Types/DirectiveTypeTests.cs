using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
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
            schema.GetType<ObjectType>("Bar").Directives,
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
                    "The specified directive `@foo` " +
                    "is unique and cannot be added twice.",
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
            schema.GetType<ObjectType>("Bar")
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
                .Ignore<CustomDirective2>(null, t => t.Argument2);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Ignore_ExpressionIsNull_ArgumentNullException()
    {
        // arrange
        var descriptor =
            DirectiveTypeDescriptor.New<CustomDirective2>(
                DescriptorContext.Create());

        // act
        void Action() => descriptor.Ignore(null);

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
        var directive = schema.GetDirectiveType("foo");
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
        var directive = schema.GetDirectiveType("foo");
        Assert.NotNull(directive.Middleware);

        await schema.MakeExecutable().ExecuteAsync("{ foo }");
        await schema.MakeExecutable().ExecuteAsync("{ foo1 }");
        await schema.MakeExecutable().ExecuteAsync("{ foo foo1 }");
        var result = await schema.MakeExecutable().ExecuteAsync("{ foo foo1 }");

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
        var directive = schema.GetDirectiveType("foo");
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
        var directive = schema.GetDirectiveType("foo");
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
                new DirectiveType(
                    d => d
                        .Name("foo")
                        .Location(DirectiveLocation.Object)
                        .Use((_, _) => _ => default)))
            .Create();

        // assert
        var directive = schema.GetDirectiveType("foo");
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
                new DirectiveType(
                    d => d
                        .Name("foo")
                        .Location(DirectiveLocation.Object)
                        .Use<DirectiveMiddleware>()))
            .Create();

        // assert
        var directive = schema.GetDirectiveType("foo");
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
                new DirectiveType(
                    d => d
                        .Name("foo")
                        .Location(DirectiveLocation.Object)
                        .Use((_, next) => new DirectiveMiddleware(next))))
            .Create();

        // assert
        var directive = schema.GetDirectiveType("foo");
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
                    new DirectiveType(
                        d => d.Name("foo")
                            .Location(DirectiveLocation.Object)
                            .Use(null)))
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
            .Print()
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
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task AnnotationBased_DeprecatedInputTypes_NonNullableField_Invalid()
    {
        // arrange
        // act
        static async Task call() =>
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
        var exception = await Assert.ThrowsAsync<SchemaException>(call);
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
                new DirectiveType(
                    x => x
                        .Name("Qux")
                        .Location(DirectiveLocation.FieldDefinition)
                        .Argument("bar")
                        .Type<IntType>()
                        .Deprecated("a")))
            .BuildRequestExecutorAsync();

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
                    new DirectiveType(
                        x => x
                            .Name("Qux")
                            .Location(DirectiveLocation.FieldDefinition)
                            .Argument("bar")
                            .Type<NonNullType<IntType>>()
                            .Deprecated("a")))
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
                @"directive @Qux(bar: String @deprecated(reason: ""reason""))
                    on FIELD_DEFINITION")
            .BuildSchemaAsync();

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
                    @"directive @Qux(bar: String! @deprecated(reason: ""reason""))
                        on FIELD_DEFINITION")
                .BuildSchemaAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(call);
        exception.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public void Directive_ValidateArgs_InvalidArg()
    {
        // arrange
        var sourceText = @"
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
        var sourceText = @"
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
        var sourceText = @"
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
        var sourceText = $@"
            type Query {{
                foo: String @a(d: {long.MaxValue})
            }}

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
            .BuildSchemaAsync();

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
            .BuildSchemaAsync();

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
            .BuildSchemaAsync();

        // assert
        schema.MatchSnapshot();
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
        private readonly FieldDelegate _next;

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
        private static int _instances;
        private readonly int _count;

        public DirectiveMiddleware1(FieldDelegate next)
        {
            _next = next;
            _count = Interlocked.Increment(ref _instances);
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
        public string Argument { get; set; }
    }

    public class CustomDirective2
    {
        public string Argument1 { get; set; }

        public string Argument2 { get; set; }
    }

    public class DirectiveWithDefaults
    {
        [DefaultValue("abc")] public string Argument1 { get; set; }

        public string Argument2 { get; set; }
    }

    [DirectiveType("anno", DirectiveLocation.FieldDefinition)]
    public sealed class AnnotationDirective
    {
        public AnnotationDirective(string foo)
        {
            Foo = foo;
        }

        public string Foo { get; }
    }

    [DirectiveType(DirectiveLocation.FieldDefinition)]
    public sealed class FooDirective
    {
        public FooDirective(string foo)
        {
            Foo = foo;
        }

        public string Foo { get; }
    }
}
