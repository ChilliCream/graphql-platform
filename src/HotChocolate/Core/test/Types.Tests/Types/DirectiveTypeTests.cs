using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class DirectiveTypeTests
        : TypeTestBase
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
                t => Assert.Equal("argument", t.Name.Value));
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
                .Use(next => context => default(ValueTask)));

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
            Action a = () => new DirectiveType(null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void NoName()
        {
            // act
            Action a = () =>
                CreateDirective(new DirectiveType(d => { }));

            // assert
            Assert.Throws<SchemaException>(a);
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
                t.Field("foo").Resolver(() => "baz");
            });

            // act
            var schema = Schema.Create(t =>
            {
                t.RegisterDirective(directiveType);
                t.RegisterQueryType(objectType);
            });

            // assert
            IDirectiveCollection collection =
                schema.GetType<ObjectType>("Bar").Directives;
            Assert.Collection(collection,
                t =>
                {
                    Assert.Equal("foo", t.Name);
                    Assert.Equal("1", t.GetArgument<string>("a"));
                },
                t =>
                {
                    Assert.Equal("foo", t.Name);
                    Assert.Equal("2", t.GetArgument<string>("a"));
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
                    .Use(next => context => default(ValueTask))
                    .Argument("a").Type<StringType>());


            var objectType = new ObjectType(t =>
            {
                t.Name("Bar");
                t.Directive("foo", new ArgumentNode("a", "1"));
                t.Field("foo").Resolver(() => "baz")
                    .Directive("foo", new ArgumentNode("a", "2"));
            });

            // act
            var schema = Schema.Create(t =>
            {
                t.RegisterDirective(directiveType);
                t.RegisterQueryType(objectType);
            });

            // assert
            IReadOnlyCollection<IDirective> collection =
                schema.GetType<ObjectType>("Bar")
                    .Fields["foo"].ExecutableDirectives;

            Assert.Collection(collection,
                t =>
                {
                    Assert.Equal("foo", t.Name);
                    Assert.Equal("1", t.GetArgument<string>("a"));
                },
                t =>
                {
                    Assert.Equal("foo", t.Name);
                    Assert.Equal("2", t.GetArgument<string>("a"));
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
                t.Field("foo").Resolver(() => "baz");
            });

            // act
            Action a = () => Schema.Create(t =>
             {
                 t.RegisterDirective(directiveType);
                 t.RegisterQueryType(objectType);
             });

            // assert
            SchemaException exception = Assert.Throws<SchemaException>(a);
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
                    .Use(next => context => default(ValueTask))
                    .Argument("a").Type<StringType>());


            var objectType = new ObjectType(t =>
            {
                t.Name("Bar");
                t.Directive("foo", new ArgumentNode("a", "1"));
                t.Field("foo").Resolver(() => "baz")
                    .Directive("foo", new ArgumentNode("a", "2"));
            });

            // act
            var schema = Schema.Create(t =>
            {
                t.RegisterDirective(directiveType);
                t.RegisterQueryType(objectType);
            });

            // assert
            IReadOnlyCollection<IDirective> collection =
                schema.GetType<ObjectType>("Bar")
                    .Fields["foo"].ExecutableDirectives;

            Assert.Collection(collection,
                t =>
                {
                    Assert.Equal("foo", t.Name);
                    Assert.Equal("2", t.GetArgument<string>("a"));
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
            DirectiveTypeDescriptor<CustomDirective2> descriptor =
                DirectiveTypeDescriptor.New<CustomDirective2>(
                    DescriptorContext.Create());

            // act
            Action action = () =>
                DirectiveTypeDescriptorExtensions
                    .Ignore(descriptor, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Ignore_Argument2_Property()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
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
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddDirectiveType(new DirectiveType<CustomDirective2>(d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use(next => context => default(ValueTask))))
                .Create();

            // assert
            DirectiveType directive = schema.GetDirectiveType("foo");
            Assert.Collection(directive.MiddlewareComponents,
                t => Assert.NotNull(t));
        }

        [Fact]
        public void Use_ClassMiddleware()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddDirectiveType(new DirectiveType<CustomDirective2>(d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use<DirectiveMiddleware>()))
                .Create();

            // assert
            DirectiveType directive = schema.GetDirectiveType("foo");
            Assert.Collection(directive.MiddlewareComponents,
                t => Assert.NotNull(t));
        }

        [Fact]
        public void Use_ClassMiddleware_WithFactory()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddDirectiveType(new DirectiveType<CustomDirective2>(d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use((sp, next) => new DirectiveMiddleware(next))))
                .Create();

            // assert
            DirectiveType directive = schema.GetDirectiveType("foo");
            Assert.Collection(directive.MiddlewareComponents,
                t => Assert.NotNull(t));
        }

        [Fact]
        public void Use_ClassMiddleware_WithFactoryNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddDirectiveType(new DirectiveType<CustomDirective2>(d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use(null)))
                .Create();

            // assert
            Assert.Throws<SchemaException>(action);
        }

        [Fact]
        public void Use2_DelegateMiddleware()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use(next => context => default(ValueTask))))
                .Create();

            // assert
            DirectiveType directive = schema.GetDirectiveType("foo");
            Assert.Collection(directive.MiddlewareComponents,
                t => Assert.NotNull(t));
        }

        [Fact]
        public void Use2_ClassMiddleware()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use<DirectiveMiddleware>()))
                .Create();

            // assert
            DirectiveType directive = schema.GetDirectiveType("foo");
            Assert.Collection(directive.MiddlewareComponents,
                t => Assert.NotNull(t));
        }

        [Fact]
        public void Use2_ClassMiddleware_WithFactory()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use((sp, next) => new DirectiveMiddleware(next))))
                .Create();

            // assert
            DirectiveType directive = schema.GetDirectiveType("foo");
            Assert.Collection(directive.MiddlewareComponents,
                t => Assert.NotNull(t));
        }

        [Fact]
        public void Use2_ClassMiddleware_WithFactoryNull_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddDirectiveType(new DirectiveType(d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use(null)))
                .Create();

            // assert
            Assert.Throws<SchemaException>(action);
        }

        [Fact]
        public void Infer_Directive_Argument_Defaults_From_Properties()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Directive("foo")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolver("bar"))
                .AddDirectiveType(new DirectiveType<DirectiveWithDefaults>(d => d
                    .Name("foo")
                    .Location(DirectiveLocation.Object)
                    .Use<DirectiveMiddleware>()))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class CustomDirectiveType
            : DirectiveType<CustomDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<CustomDirective> descriptor)
            {
                descriptor.Name("Custom");
                descriptor.Location(DirectiveLocation.Enum);
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Use(next => context => default(ValueTask));
            }
        }

        public class Custom2DirectiveType
            : DirectiveType<CustomDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<CustomDirective> descriptor)
            {
                descriptor.Name("Custom");
                descriptor.Location(DirectiveLocation.Enum);
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Use(next => context => default(ValueTask));
                descriptor.BindArgumentsImplicitly().BindArgumentsExplicitly();
            }
        }

        public class DirectiveMiddleware
        {
            private FieldDelegate _next;

            public DirectiveMiddleware(FieldDelegate next)
            {
                _next = next;
            }

            public Task InvokeAsync(IDirectiveContext context) =>
                Task.CompletedTask;
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
}
