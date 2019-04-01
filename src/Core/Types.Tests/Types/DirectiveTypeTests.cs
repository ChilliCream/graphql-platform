using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Language;
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
            Assert.True(directiveType.IsExecutable);
            Assert.NotNull(directiveType.Middleware);
            Assert.Equal(typeof(CustomDirective), directiveType.ClrType);
        }

        [Fact]
        public void ConfigureDirectiveWithResolver()
        {
            // arrange
            var directiveType = new DirectiveType(
                t => t.Name("Foo")
                    .Location(DirectiveLocation.Field)
                    .Use(next => context => Task.CompletedTask));
            // act
            directiveType = CreateDirective(directiveType);

            // assert
            Assert.True(directiveType.IsExecutable);
            Assert.NotNull(directiveType.Middleware);
            Assert.Null(directiveType.ClrType);
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
            Action a = () => new DirectiveType(d => { });

            // assert
            Assert.Throws<InvalidOperationException>(a);
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
                    .Use(next => context => Task.CompletedTask)
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
                    .Use(next => context => Task.CompletedTask)
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

        public class CustomDirectiveType
            : DirectiveType<CustomDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<CustomDirective> descriptor)
            {
                descriptor.Name("Custom");
                descriptor.Location(DirectiveLocation.Enum);
                descriptor.Location(DirectiveLocation.Field);
                descriptor.Use(next => context => Task.CompletedTask);
            }
        }

        public class CustomDirective
        {
            public string Argument { get; set; }
        }
    }
}
