using System;
using HotChocolate.Configuration;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types
{
    public class EnumTypeDescriptorTests
    {
        [Fact]
        public void InitialName()
        {
            // act
            var descriptor = new EnumTypeDescriptor("Foo");

            // assert
            Assert.Equal("Foo", descriptor.CreateDescription().Name);
        }

        [Fact]
        public void NoNameProvided()
        {
            // act
            Action a = () => new EnumTypeDescriptor((string)null);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void InferNameFromType()
        {
            // act
            var descriptor = new EnumTypeDescriptor(typeof(FooEnum));

            // assert
            Assert.Equal("FooEnum", descriptor.CreateDescription().Name);
        }

        [Fact]
        public void NoTypeProvided()
        {
            // act
            Action a = () => new EnumTypeDescriptor((Type)null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void InferValuesFromType()
        {
            // act
            var descriptor = new EnumTypeDescriptor(typeof(FooEnum));

            // assert
            EnumTypeDescription description = descriptor.CreateDescription();
            Assert.Collection(description.Values,
                t =>
                {
                    Assert.Equal("BAR1", t.Name);
                    Assert.Equal(FooEnum.Bar1, t.Value);
                },
                t =>
                {
                    Assert.Equal("BAR2", t.Name);
                    Assert.Equal(FooEnum.Bar2, t.Value);
                });
        }

        [Fact]
        public void SpecifyOneValueInferTheOthers()
        {
            // arrange
            var descriptor = new EnumTypeDescriptor(typeof(FooEnum));

            // act
            IEnumTypeDescriptor desc = descriptor;
            desc.Item(FooEnum.Bar1).Name("FOOBAR");

            // assert
            EnumTypeDescription description = descriptor.CreateDescription();
            Assert.Collection(description.Values,
                t =>
                {
                    Assert.Equal("FOOBAR", t.Name);
                    Assert.Equal(FooEnum.Bar1, t.Value);
                },
                t =>
                {
                    Assert.Equal("BAR2", t.Name);
                    Assert.Equal(FooEnum.Bar2, t.Value);
                });
        }

        [Fact]
        public void ExplicitValueBinding()
        {
            // arrange
            var descriptor = new EnumTypeDescriptor(typeof(FooEnum));

            // act
            IEnumTypeDescriptor desc = descriptor;
            desc.Item(FooEnum.Bar1).Name("FOOBAR");
            desc.BindItems(BindingBehavior.Explicit);

            // assert
            EnumTypeDescription description = descriptor.CreateDescription();
            Assert.Collection(description.Values,
                t =>
                {
                    Assert.Equal("FOOBAR", t.Name);
                    Assert.Equal(FooEnum.Bar1, t.Value);
                });
        }

        [Fact]
        public void AddDirective()
        {
            // arrange
            var descriptor = new EnumTypeDescriptor("Foo");

            // act
            IEnumTypeDescriptor desc = descriptor;
            desc.Directive("Bar");

            // assert
            EnumTypeDescription description = descriptor.CreateDescription();
            Assert.Collection(description.Directives,
                t => Assert.Equal("Bar", t.ParsedDirective.Name.Value));
        }

        [Fact]
        public void AddDirectiveWithDirectiveNode()
        {
            // arrange
            var descriptor = new EnumTypeDescriptor("Foo");

            // act
            IEnumTypeDescriptor desc = descriptor;
            desc.Directive(new DirectiveNode("Bar"));

            // assert
            EnumTypeDescription description = descriptor.CreateDescription();
            Assert.Collection(description.Directives,
                t => Assert.Equal("Bar", t.ParsedDirective.Name.Value));
        }

        [Fact]
        public void AddDirectiveWithArgument()
        {
            // arrange
            var descriptor = new EnumTypeDescriptor("Foo");

            // act
            IEnumTypeDescriptor desc = descriptor;
            desc.Directive("Bar",
                new ArgumentNode("a", new StringValueNode("b")));

            // assert
            EnumTypeDescription description = descriptor.CreateDescription();
            Assert.Collection(description.Directives,
                t =>
                {
                    Assert.Equal("Bar", t.ParsedDirective.Name.Value);
                    Assert.Collection(t.ParsedDirective.Arguments,
                        x =>
                        {
                            Assert.Equal("a", x.Name.Value);
                            Assert.IsType<StringValueNode>(x.Value);
                            Assert.Equal("b", ((StringValueNode)x.Value).Value);
                        });
                });
        }


        private enum FooEnum
        {
            Bar1,
            Bar2
        }
    }
}
