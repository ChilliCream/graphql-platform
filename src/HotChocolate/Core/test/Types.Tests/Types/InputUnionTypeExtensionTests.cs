using System.Linq;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class InputUnionTypeExtensionTests
    {
        [Fact]
        public void InputUnionTypeExtension_AddType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType<FooType>()
                .AddType<FooTypeExtension>()
                .Create();

            // assert
            FooType type = schema.GetType<FooType>("Foo");
            Assert.Collection(type.Types.Values,
                t => Assert.IsType<AType>(t),
                t => Assert.IsType<BType>(t));
        }


        [Fact]
        public void InputUnionTypeExtension_SetTypeContextData()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType<FooType>()
                .AddType(new InputUnionTypeExtension(d => d
                    .Name("Foo")
                    .Extend()
                    .OnBeforeCreate(c => c.ContextData["foo"] = "bar")))
                .Create();

            // assert
            InputUnionType type = schema.GetType<InputUnionType>("Foo");
            Assert.True(type.ContextData.ContainsKey("foo"));
        }

        [Fact]
        public void InputUnionTypeExtension_SetDirectiveOnType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType<FooType>()
                .AddType(new InputUnionTypeExtension(d => d
                    .Name("Foo")
                    .Directive("dummy")))
                .AddDirectiveType<DummyDirective>()
                .Create();

            // assert
            InputUnionType type = schema.GetType<InputUnionType>("Foo");
            Assert.True(type.Directives.Contains("dummy"));
        }

        [Fact]
        public void InputUnionTypeExtension_ReplaceDirectiveOnType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType(new InputUnionType(t => t
                    .Name("Foo")
                    .Type<AType>()
                    .Directive("dummy_arg", new ArgumentNode("a", "a"))))
                .AddType(new InputUnionTypeExtension(d => d
                    .Name("Foo")
                    .Directive("dummy_arg", new ArgumentNode("a", "b"))))
                .AddDirectiveType<DummyWithArgDirective>()
                .Create();

            // assert
            InputUnionType type = schema.GetType<InputUnionType>("Foo");
            string value = type.Directives["dummy_arg"]
                .First().GetArgument<string>("a");
            Assert.Equal("b", value);
        }

        [Fact]
        public void InputUnionTypeExtension_CopyDependencies_ToType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType<FooType>()
                .AddType(new InputUnionTypeExtension(d => d
                    .Name("Foo")
                    .Type<BType>()))
                .Create();

            // assert
            FooType type = schema.GetType<FooType>("Foo");
            Assert.Collection(type.Types.Values,
                t => Assert.IsType<AType>(t),
                t => Assert.IsType<BType>(t));
        }

        [Fact]
        public void InputUnionTypeExtension_RepeatableDirectiveOnType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryType>()
                .AddType(new InputUnionType(t => t
                    .Name("Foo")
                    .Type<AType>()
                    .Directive("dummy_rep")))
                .AddType(new InputUnionTypeExtension(d => d
                    .Name("Foo")
                    .Directive("dummy_rep")))
                .AddDirectiveType<RepeatableDummyDirective>()
                .Create();

            // assert
            InputUnionType type = schema.GetType<InputUnionType>("Foo");
            int count = type.Directives["dummy_rep"].Count();
            Assert.Equal(2, count);
        }

        public class QueryType
            : ObjectType
        {
            protected override void Configure(
                IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");
                descriptor.Field("description").Resolver("Foo");
            }
        }

        public class AType
            : InputObjectType
        {
            protected override void Configure(
                IInputObjectTypeDescriptor descriptor)
            {
                descriptor.Name("A");
                descriptor.Field("description").Type<StringType>();
            }
        }

        public class BType
            : InputObjectType
        {
            protected override void Configure(
                IInputObjectTypeDescriptor descriptor)
            {
                descriptor.Name("B");
                descriptor.Field("description").Type<StringType>();
            }
        }

        public class FooType
            : InputUnionType
        {
            protected override void Configure(
                IInputUnionTypeDescriptor descriptor)
            {
                descriptor.Name("Foo");
                descriptor.Type<AType>();
            }
        }

        public class FooTypeExtension
            : InputUnionTypeExtension
        {
            protected override void Configure(
                IInputUnionTypeDescriptor descriptor)
            {
                descriptor.Name("Foo");
                descriptor.Type<BType>();
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

        public class DummyDirective
            : DirectiveType
        {
            protected override void Configure(
                IDirectiveTypeDescriptor descriptor)
            {
                descriptor.Name("dummy");
                descriptor.Location(DirectiveLocation.InputUnionDefinition);
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
                descriptor.Location(DirectiveLocation.InputUnionDefinition);
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
                descriptor.Location(DirectiveLocation.InputUnionDefinition);
            }
        }
    }
}
