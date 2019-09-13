using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class DirectiveTests
        : TypeTestBase
    {
        [Fact]
        public void ConvertCustomDirectiveToDirectiveNode()
        {
            // arrange
            ISchema schema = CreateSchema();
            DirectiveType directiveType = schema.GetDirectiveType("Foo");
            var fooDirective = new FooDirective
            {
                Bar = "123",
                Child = new FooChild
                {
                    Bar = "456"
                }
            };

            // act
            var directive = new Directive(
                directiveType, fooDirective, new object());
            DirectiveNode directiveNode = directive.ToNode();

            // assert
            Assert.Equal(directiveType.Name, directiveNode.Name.Value);
            Assert.Collection(directiveNode.Arguments,
                t =>
                {
                    Assert.Equal("bar", t.Name.Value);
                    Assert.Equal("123", ((StringValueNode)t.Value).Value);
                },
                t =>
                {
                    Assert.Equal("child", t.Name.Value);
                    Assert.Collection(((ObjectValueNode)t.Value).Fields,
                        x =>
                        {
                            Assert.Equal("bar", x.Name.Value);
                            Assert.Equal("456",
                                ((StringValueNode)x.Value).Value);
                        });
                });
        }

        [Fact]
        public void MapCustomDirectiveToDifferentType()
        {
            // arrange
            ISchema schema = CreateSchema();
            DirectiveType directiveType = schema.GetDirectiveType("Foo");
            var fooDirective = new FooDirective
            {
                Bar = "123",
                Child = new FooChild
                {
                    Bar = "456"
                }
            };

            // act
            var directive = new Directive(
                directiveType, fooDirective, new object());
            FooChild mappedObject = directive.ToObject<FooChild>();

            // assert
            Assert.Equal("123", mappedObject.Bar);
        }

        [Fact]
        public void GetArgumentFromCustomDirective()
        {
            // arrange
            ISchema schema = CreateSchema();
            DirectiveType directiveType = schema.GetDirectiveType("Foo");
            var fooDirective = new FooDirective
            {
                Bar = "123",
                Child = new FooChild
                {
                    Bar = "456"
                }
            };

            // act
            var directive = new Directive(
                directiveType, fooDirective, new object());
            string barValue = directive.GetArgument<string>("bar");

            // assert
            Assert.Equal("123", barValue);
        }

        private static ISchema CreateSchema()
        {
            return CreateSchema(b =>
            {
                b.AddDirectiveType<FooDirectiveType>();
                b.AddType<InputObjectType<FooChild>>();
            });
        }

        public class FooDirectiveType
            : DirectiveType<FooDirective>
        {
            protected override void Configure(
                IDirectiveTypeDescriptor<FooDirective> descriptor)
            {
                descriptor.Name("Foo");
                descriptor.Location(DirectiveLocation.Schema);
            }
        }

        public class FooDirective
        {
            public string Bar { get; set; }

            public FooChild Child { get; set; }
        }

        public class FooChild
        {
            public string Bar { get; set; }
        }

        public class FooChild2
        {
            public string Bar { get; set; }
        }
    }
}
