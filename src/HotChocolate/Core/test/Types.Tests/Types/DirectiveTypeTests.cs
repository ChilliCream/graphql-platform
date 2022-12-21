using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

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
        Assert.True(directiveType.HasMiddleware);
        Assert.NotEmpty(directiveType.MiddlewareComponents);
        Assert.Equal(typeof(CustomDirective), directiveType.RuntimeType);
        Assert.Collection(directiveType.Arguments,
            t => Assert.Equal("argument", t.Name));
    }

    [Fact]
    public void ConfigureTypedDirective_DefaultBinding_Explicit()
    {
        // arrange
        // act
        DirectiveType directiveType =
            CreateDirective(new CustomDirectiveType(),
                b => b.ModifyOptions(o =>
                    o.DefaultBindingBehavior = BindingBehavior.Explicit));

        // assert
        Assert.True(directiveType.HasMiddleware);
        Assert.NotEmpty(directiveType.MiddlewareComponents);
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
        Assert.True(directiveType.HasMiddleware);
        Assert.NotEmpty(directiveType.MiddlewareComponents);
        Assert.Equal(typeof(CustomDirective), directiveType.RuntimeType);
        Assert.Empty(directiveType.Arguments);
    }

    [Fact]
    public void ConfigureDirectiveWithResolver()
    {
        // arrange
        var directiveType = new DirectiveType(t => t
            .Name("Foo")
            .Location(DirectiveLocation.Field)
            .Use(_ => _ => default));

        // act
        directiveType = CreateDirective(directiveType);

        // assert
        Assert.True(directiveType.HasMiddleware);
        Assert.NotEmpty(directiveType.MiddlewareComponents);
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

        var objectType = new ObjectType(t =>
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
                Assert.Equal("foo", t.Name);
                Assert.Equal("1", t.GetArgumentValue<string>("a"));
            },
            t =>
            {
                Assert.Equal("foo", t.Name);
                Assert.Equal("2", t.GetArgumentValue<string>("a"));
            });
    }

    [Fact]
    public void ExecutableRepeatableDirectives()
    {
        // arrange
        var directiveType = new DirectiveType(
            t => t.Name("foo")
                .Repeatable()
                .Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.FieldDefinition)
                .Use(_ => _ => default)
                .Argument("a").Type<StringType>());


        var objectType = new ObjectType(t =>
        {
            t.Name("Bar");
            t.Directive("foo", new ArgumentNode("a", "1"));
            t.Field("foo").Resolve(() => "baz").Directive("foo", new ArgumentNode("a", "2"));
        });

        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(objectType)
            .AddDirectiveType(directiveType)
            .Create();

        // assert
        IReadOnlyCollection<IDirective> collection =
            schema.GetType<ObjectType>("Bar")
                .Fields["foo"].ExecutableDirectives;

        Assert.Collection(collection,
            t =>
            {
                Assert.Equal("foo", t.Name);
                Assert.Equal("1", t.GetArgumentValue<string>("a"));
            },
            t =>
            {
                Assert.Equal("foo", t.Name);
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

        var objectType = new ObjectType(t =>
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
        Assert.Collection(exception.Errors,
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
                .Use(_ => _ => default)
                .Argument("a").Type<StringType>());


        var objectType = new ObjectType(t =>
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
        IReadOnlyCollection<IDirective> collection =
            schema.GetType<ObjectType>("Bar")
                .Fields["foo"].ExecutableDirectives;

        Assert.Collection(collection,
            t =>
            {
                Assert.Equal("foo", t.Name);
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
            .AddQueryType(c => c
                .Name("Query")
                .Directive("foo")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(new DirectiveType<CustomDirective2>(d => d
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
            .AddQueryType(c => c
                .Name("Query")
                .Directive("foo")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(new DirectiveType<CustomDirective2>(d => d
                .Name("foo")
                .Location(DirectiveLocation.Object)
                .Use(_ => _ => default)))
            .Create();

        // assert
        var directive = schema.GetDirectiveType("foo");
        Assert.Collection(directive.MiddlewareComponents,
            t => Assert.NotNull(t));
    }

    [Fact]
    public void Use_ClassMiddleware()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("foo")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(new DirectiveType<CustomDirective2>(d => d
                .Name("foo")
                .Location(DirectiveLocation.Object)
                .Use<DirectiveMiddleware>()))
            .Create();

        // assert
        var directive = schema.GetDirectiveType("foo");
        Assert.Collection(directive.MiddlewareComponents, Assert.NotNull);
    }

    [Fact]
    public void Use_ClassMiddleware_WithFactory()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("foo")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(new DirectiveType<CustomDirective2>(d => d
                .Name("foo")
                .Location(DirectiveLocation.Object)
                .Use((_, next) => new DirectiveMiddleware(next))))
            .Create();

        // assert
        var directive = schema.GetDirectiveType("foo");
        Assert.Collection(directive.MiddlewareComponents, Assert.NotNull);
    }

    [Fact]
    public void Use_ClassMiddleware_WithFactoryNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action() =>
            SchemaBuilder.New()
                .AddQueryType(c => c.Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
                .AddDirectiveType(new DirectiveType<CustomDirective2>(d => d.Name("foo")
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
            .AddQueryType(c => c
                .Name("Query")
                .Directive("foo")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(new DirectiveType(d => d
                .Name("foo")
                .Location(DirectiveLocation.Object)
                .Use(_ => _ => default)))
            .Create();

        // assert
        var directive = schema.GetDirectiveType("foo");
        Assert.Collection(directive.MiddlewareComponents, Assert.NotNull);
    }

    [Fact]
    public void Use2_ClassMiddleware()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("foo")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(new DirectiveType(d => d
                .Name("foo")
                .Location(DirectiveLocation.Object)
                .Use<DirectiveMiddleware>()))
            .Create();

        // assert
        var directive = schema.GetDirectiveType("foo");
        Assert.Collection(directive.MiddlewareComponents, Assert.NotNull);
    }

    [Fact]
    public void Use2_ClassMiddleware_WithFactory()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType(c => c
                .Name("Query")
                .Directive("foo")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(new DirectiveType(d => d
                .Name("foo")
                .Location(DirectiveLocation.Object)
                .Use((_, next) => new DirectiveMiddleware(next))))
            .Create();

        // assert
        var directive = schema.GetDirectiveType("foo");
        Assert.Collection(directive.MiddlewareComponents, Assert.NotNull);
    }

    [Fact]
    public void Use2_ClassMiddleware_WithFactoryNull_ArgumentNullException()
    {
        // arrange
        // act
        void Action() =>
            SchemaBuilder.New()
                .AddQueryType(c => c.Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
                .AddDirectiveType(new DirectiveType(d => d.Name("foo")
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
            .AddQueryType(c => c
                .Name("Query")
                .Directive("foo")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddDirectiveType(new DirectiveType<DirectiveWithDefaults>(d => d
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
    public async Task AnnotationBased_Depreacted_NullableArguments_Valid()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("bar")
                .Resolve("asd")
                .Directive<DeprecatedDirective>())
            .AddDirectiveType(new DirectiveType<DeprecatedDirective>(
                x => x.Location(DirectiveLocation.FieldDefinition)))
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task AnnotationBased_DepreactedInputTypes_NonNullableField_Invalid()
    {
        // arrange
        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("bar")
                .Resolve("asd")
                .Directive<DeprecatedNonNull>())
            .AddDirectiveType(new DirectiveType<DeprecatedNonNull>(
                x => x.Location(DirectiveLocation.FieldDefinition)))
            .BuildRequestExecutorAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(call);
        exception.Errors.Single().ToString().MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_Depreacted_NullableArguments_Valid()
    {
        // arrange
        // act
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("bar")
                .Resolve("asd")
                .Directive("Qux"))
            .AddDirectiveType(
                new DirectiveType(x => x
                    .Name("Qux")
                    .Location(DirectiveLocation.FieldDefinition)
                    .Argument("bar")
                    .Type<IntType>()
                    .Deprecated("a")))
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task CodeFirst_DepreactedInputTypes_NonNullableField_Invalid()
    {
        // arrange
        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("bar")
                .Resolve("asd")
                .Directive("Qux"))
            .AddDirectiveType(
                new DirectiveType(x => x
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
    public async Task SchemaFirst_DepreactedDirective_NullableFields_Valid()
    {
        // arrange
        // act
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("bar")
                .Resolve("asd")
                .Directive("Qux"))
            .AddDocumentFromString(@"
                    directive @Qux(bar: String @deprecated(reason: ""reason"")) on FIELD_DEFINITION
                ")
            .BuildSchemaAsync();

        // assert
        schema.ToString().MatchSnapshot();
    }

    [Fact]
    public async Task SchemaFirst_DepreactedDirective_NonNullableField_Invalid()
    {
        // arrange
        // act
        Func<Task> call = async () => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x.Name("Query").Field("bar").Resolve("asd").Directive("Qux"))
            .AddDocumentFromString(@"
                    directive @Qux(bar: String! @deprecated(reason: ""reason"")) on FIELD_DEFINITION
                ")
            .BuildSchemaAsync();

        // assert
        var exception = await Assert.ThrowsAsync<SchemaException>(call);
        exception.Errors.Single().ToString().MatchSnapshot();
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
            descriptor.Use(_ => _ => default);
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
            descriptor.Use(_ => _ => default);
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

        public Task InvokeAsync(IDirectiveContext context) =>
            Task.CompletedTask;
    }

    public class DeprecatedDirective
    {
        [Obsolete("reason")]
        public int? ObsoleteWithReason { get; set; }

        [Obsolete]
        public int? Obsolete { get; set; }

        [GraphQLDeprecated("reason")]
        public int? Deprecated { get; set; }
    }

    public class DeprecatedNonNull
    {
        [Obsolete("reason")]
        public int ObsoleteWithReason { get; set; }
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
        [DefaultValue("abc")]
        public string Argument1 { get; set; }
        public string Argument2 { get; set; }
    }
}
