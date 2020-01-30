using System;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Xunit;

namespace HotChocolate.Types
{
    public class EnumTypeDescriptorTests
        : DescriptorTestBase
    {
        [Fact]
        public void InferNameFromType()
        {
            // act
            var descriptor = EnumTypeDescriptor.New(Context, typeof(FooEnum));

            // assert
            Assert.Equal("FooEnum", descriptor.CreateDefinition().Name);
        }

        [Fact]
        public void NoTypeProvided()
        {
            // act
            Action a = () => EnumTypeDescriptor.New(Context, (Type)null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void InferValuesFromType()
        {
            // act
            var descriptor = EnumTypeDescriptor.New(Context, typeof(FooEnum));

            // assert
            EnumTypeDefinition description = descriptor.CreateDefinition();
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
            var descriptor = EnumTypeDescriptor.New(Context, typeof(FooEnum));

            // act
            IEnumTypeDescriptor desc = descriptor;
            desc.Item(FooEnum.Bar1).Name("FOOBAR");

            // assert
            EnumTypeDefinition description = descriptor.CreateDefinition();
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
            var descriptor = EnumTypeDescriptor.New(Context, typeof(FooEnum));

            // act
            IEnumTypeDescriptor desc = descriptor;
            desc.Item(FooEnum.Bar1).Name("FOOBAR");
            desc.BindValues(BindingBehavior.Explicit);

            // assert
            EnumTypeDefinition description = descriptor.CreateDefinition();
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
            var descriptor = EnumTypeDescriptor.New(Context);

            // act
            IEnumTypeDescriptor desc = descriptor;
            desc.Directive(new NameString("Bar"));

            // assert
            EnumTypeDefinition description = descriptor.CreateDefinition();
            Assert.Collection(description.Directives,
                t => Assert.Equal("Bar", t.ParsedDirective.Name.Value));
        }

        [Fact]
        public void AddDirectiveWithDirectiveNode()
        {
            // arrange
            var descriptor = EnumTypeDescriptor.New(Context);

            // act
            IEnumTypeDescriptor desc = descriptor;
            desc.Directive(new DirectiveNode("Bar"));

            // assert
            EnumTypeDefinition description = descriptor.CreateDefinition();
            Assert.Collection(description.Directives,
                t => Assert.Equal("Bar", t.ParsedDirective.Name.Value));
        }

        [Fact]
        public void AddDirectiveWithArgument()
        {
            // arrange
            var descriptor = EnumTypeDescriptor.New(Context);

            // act
            IEnumTypeDescriptor desc = descriptor;
            desc.Directive("Bar",
                new ArgumentNode("a", new StringValueNode("b")));

            // assert
            EnumTypeDefinition description = descriptor.CreateDefinition();
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
