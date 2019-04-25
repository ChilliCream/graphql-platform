using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using Xunit;
using Snapshooter.Xunit;
using System;
using HotChocolate.Language;
using System.Linq;
using Moq;

namespace HotChocolate.Types
{
    public class ObjectTypeExtensionTests
    {
        [Fact]
        public void ObjectTypeExtension_AddField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType<FooTypeExtension>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields.ContainsField("test"));
        }

        [Fact]
        public void ObjectTypeExtension_OverrideResolver()
        {
            // arrange
            FieldResolverDelegate resolver =
                ctx => Task.FromResult<object>(null);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Type<StringType>()
                    .Resolver(resolver)))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.Equal(resolver, type.Fields["description"].Resolver);
        }

        [Fact]
        public async Task ObjectTypeExtension_AddResolverType()
        {
            // arrange
            var context = new Mock<IResolverContext>(MockBehavior.Strict);
            context.Setup(t => t.Resolver<FooResolver>())
                .Returns(new FooResolver());
            context.Setup(t => t.RequestAborted)
                .Returns(CancellationToken.None);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field<FooResolver>(t => t.GetName2())
                    .Type<StringType>()))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            object value = await type.Fields["name2"].Resolver(context.Object);
            Assert.Equal("FooResolver.GetName2", value);
        }

        [Fact]
        public void ObjectTypeExtension_AddMiddleware()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Type<StringType>()
                    .Use(next => context =>
                    {
                        context.Result = "BAR";
                        return Task.CompletedTask;
                    })))
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            executor.Execute("{ description }").MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeExtension_DepricateField()
        {
            // arrange
            FieldResolverDelegate resolver =
                ctx => Task.FromResult<object>(null);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Type<StringType>()
                    .DeprecationReason("Foo")))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["description"].IsDeprecated);
            Assert.Equal("Foo", type.Fields["description"].DeprecationReason);
        }

        [Fact]
        public void ObjectTypeExtension_SetTypeContextData()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Extend()
                    .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.ContextData.ContainsKey("foo"));
        }

        [Fact]
        public void ObjectTypeExtension_SetFieldContextData()
        {
            // arrange
            FieldResolverDelegate resolver =
                ctx => Task.FromResult<object>(null);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Extend()
                    .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["description"]
                .ContextData.ContainsKey("foo"));
        }

        [Fact]
        public void ObjectTypeExtension_SetArgumentContextData()
        {
            // arrange
            FieldResolverDelegate resolver =
                ctx => Task.FromResult<object>(null);

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Type<StringType>()
                    .Argument("a", a => a
                        .Type<StringType>()
                        .Extend()
                        .OnBeforeCreate(c => c.ContextData["foo"] = "bar"))))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["name"].Arguments["a"]
                .ContextData.ContainsKey("foo"));
        }

        [Fact]
        public void ObjectTypeExtension_SetDirectiveOnType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Directive("dummy")))
                .AddDirectiveType<DummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Directives.Contains("dummy"));
        }

        [Fact]
        public void ObjectTypeExtension_SetDirectiveOnField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Directive("dummy")))
                .AddDirectiveType<DummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["name"]
                .Directives.Contains("dummy"));
        }

        [Fact]
        public void ObjectTypeExtension_SetDirectiveOnArgument()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Argument("a", a => a.Directive("dummy"))))
                .AddDirectiveType<DummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            Assert.True(type.Fields["name"].Arguments["a"]
                .Directives.Contains("dummy"));
        }

        [Fact]
        public void ObjectTypeExtension_ReplaceDirectiveOnType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Directive("dummy_arg", new ArgumentNode("a", "a"))))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Directive("dummy_arg", new ArgumentNode("a", "b"))))
                .AddDirectiveType<DummyWithArgDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            string value = type.Directives["dummy_arg"]
                .First().GetArgument<string>("a");
            Assert.Equal("b", value);
        }

        [Fact]
        public void ObjectTypeExtension_ReplaceDirectiveOnField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Field(f => f.Description)
                    .Directive("dummy_arg", new ArgumentNode("a", "a"))))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Directive("dummy_arg", new ArgumentNode("a", "b"))))
                .AddDirectiveType<DummyWithArgDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            string value = type.Fields["description"].Directives["dummy_arg"]
                .First().GetArgument<string>("a");
            Assert.Equal("b", value);
        }

        [Fact]
        public void ObjectTypeExtension_ReplaceDirectiveOnArgument()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Field(f => f.GetName(default))
                    .Argument("a", a => a
                        .Type<StringType>()
                        .Directive("dummy_arg", new ArgumentNode("a", "a")))))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Argument("a", a =>
                        a.Directive("dummy_arg", new ArgumentNode("a", "b")))))
                .AddDirectiveType<DummyWithArgDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            string value = type.Fields["name"].Arguments["a"]
                .Directives["dummy_arg"]
                .First().GetArgument<string>("a");
            Assert.Equal("b", value);
        }

        [Fact]
        public void ObjectTypeExtension_CopyDependencies_ToType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooType>()
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Argument("a", a =>
                        a.Directive("dummy_arg", new ArgumentNode("a", "b")))))
                .AddDirectiveType<DummyWithArgDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            string value = type.Fields["name"].Arguments["a"]
                .Directives["dummy_arg"]
                .First().GetArgument<string>("a");
            Assert.Equal("b", value);
        }

        [Fact]
        public void ObjectTypeExtension_RepeatableDirectiveOnType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Directive("dummy_rep")))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Directive("dummy_rep")))
                .AddDirectiveType<RepeatableDummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            int count = type.Directives["dummy_rep"].Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void ObjectTypeExtension_RepeatableDirectiveOnField()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Field(f => f.Description)
                    .Directive("dummy_rep")))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("description")
                    .Directive("dummy_rep")))
                .AddDirectiveType<RepeatableDummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            int count = type.Fields["description"].Directives["dummy_rep"].Count();
            Assert.Equal(2, count);
        }

        [Fact]
        public void ObjectTypeExtension_RepeatableDirectiveOnArgument()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<Foo>(t => t
                    .Field(f => f.GetName(default))
                    .Argument("a", a => a
                        .Type<StringType>()
                        .Directive("dummy_rep", new ArgumentNode("a", "a")))))
                .AddType(new ObjectTypeExtension(d => d
                    .Name("Foo")
                    .Field("name")
                    .Argument("a", a =>
                        a.Directive("dummy_rep", new ArgumentNode("a", "b")))))
                .AddDirectiveType<RepeatableDummyDirective>()
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("Foo");
            int count = type.Fields["name"].Arguments["a"]
                .Directives["dummy_rep"]
                .Count();
            Assert.Equal(2, count);
        }

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(
                IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Description);
            }
        }

        public class FooTypeExtension
            : ObjectTypeExtension
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Foo");
                descriptor.Field("test")
                    .Resolver(() => new List<string>())
                    .Type<ListType<StringType>>();
            }
        }

        public class Foo
        {
            public string Description { get; } = "hello";

            public string GetName(string a)
            {
                return null;
            }
        }

        public class FooResolver
        {
            public string GetName2()
            {
                return "FooResolver.GetName2";
            }
        }

        public class DummyDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("dummy");
                descriptor.Location(DirectiveLocation.Object);
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.Location(DirectiveLocation.ArgumentDefinition);
            }
        }

        public class DummyWithArgDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("dummy_arg");
                descriptor.Argument("a").Type<StringType>();
                descriptor.Location(DirectiveLocation.Object);
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.Location(DirectiveLocation.ArgumentDefinition);
            }
        }

        public class RepeatableDummyDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("dummy_rep");
                descriptor.Repeatable();
                descriptor.Location(DirectiveLocation.Object);
                descriptor.Location(DirectiveLocation.FieldDefinition);
                descriptor.Location(DirectiveLocation.ArgumentDefinition);
            }
        }
    }
}
