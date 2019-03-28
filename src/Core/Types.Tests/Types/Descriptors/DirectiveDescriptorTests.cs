using System;
using System.Linq;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using Xunit;

namespace HotChocolate.Types
{
    public class DirectiveDescriptorTests
        : DescriptorTestBase
    {
        [Fact]
        public void DeclareName()
        {
            // arrange
            // act
            DirectiveTypeDescriptor descriptor =
                DirectiveTypeDescriptor.New(Context, "Foo");

            // assert
            Assert.Equal("Foo", descriptor.CreateDefinition().Name);
        }

        [Fact]
        public void InferName()
        {
            // arrange
            // act
            DirectiveTypeDescriptor<CustomDirective> descriptor =
                DirectiveTypeDescriptor.New<CustomDirective>(Context);

            // assert
            DirectiveTypeDefinition description =
                descriptor.CreateDefinition();
            Assert.Equal("CustomDirective", description.Name);
        }

        [Fact]
        public void OverrideName()
        {
            // arrange
            DirectiveTypeDescriptor<CustomDirective> descriptor =
                DirectiveTypeDescriptor.New<CustomDirective>(Context);

            // act
            descriptor.Name("Foo");

            // assert
            DirectiveTypeDefinition description =
                descriptor.CreateDefinition();
            Assert.Equal("Foo", description.Name);
        }

        [Fact]
        public void DeclareNullName()
        {
            // arrange
            DirectiveTypeDescriptor descriptor =
                DirectiveTypeDescriptor.New(Context, "Foo");

            // act
            Action a = () => descriptor.Name(null);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void DeclareEmptyName()
        {
            // arrange
            DirectiveTypeDescriptor descriptor =
                DirectiveTypeDescriptor.New(Context, "Foo");

            // act
            Action a = () => descriptor.Name(string.Empty);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void DeclareDescription()
        {
            // arrange
            DirectiveTypeDescriptor descriptor =
                DirectiveTypeDescriptor.New(Context, "Foo");

            // act
            descriptor.Description("Desc");

            // assert
            Assert.Equal("Desc", descriptor.CreateDefinition().Description);
        }

        [Fact]
        public void DeclareArgument()
        {
            // arrange
            DirectiveTypeDescriptor descriptor =
                DirectiveTypeDescriptor.New(Context, "Foo");

            // act
            descriptor.Argument("arg").Type<BooleanType>(); ;

            // assert
            DirectiveTypeDefinition description = descriptor.CreateDefinition();
            Assert.Equal("arg", description.Arguments.Single().Name);
        }

        [Fact]
        public void DeclareArgumentWithProperty()
        {
            // arrange
            // act
            DirectiveTypeDescriptor<CustomDirective> descriptor =
                DirectiveTypeDescriptor.New<CustomDirective>(Context);

            // assert
            DirectiveTypeDefinition description =
                descriptor.CreateDefinition();
            Assert.Collection(description.Arguments,
                t => Assert.Equal("fieldA", t.Name),
                t => Assert.Equal("fieldB", t.Name));
        }

        [Fact]
        public void DeclareExplicitArgumentBinding()
        {
            // arrange
            DirectiveTypeDescriptor<CustomDirective> descriptor =
                DirectiveTypeDescriptor.New<CustomDirective>(Context);

            // act
            descriptor.BindArguments(BindingBehavior.Explicit);
            descriptor.Argument(t => t.FieldA);

            // assert
            DirectiveTypeDefinition description =
                descriptor.CreateDefinition();
            Assert.Collection(description.Arguments,
                t => Assert.Equal("fieldA", t.Name));
        }

        [Fact]
        public void DeclareArgumentAndSpecifyType()
        {
            // arrange
            DirectiveTypeDescriptor<CustomDirective> descriptor =
                DirectiveTypeDescriptor.New<CustomDirective>(Context);

            // act
            descriptor.Argument(t => t.FieldA).Type<NonNullType<StringType>>();

            // assert
            DirectiveTypeDefinition description =
                descriptor.CreateDefinition();
            Assert.Collection(description.Arguments,
                t => Assert.Equal(
                    typeof(NonNullType<StringType>),
                    Assert.IsType<ClrTypeReference>(t.Type).Type),
                t => Assert.Equal(
                    typeof(string),
                    Assert.IsType<ClrTypeReference>(t.Type).Type));
        }

        [Fact]
        public void IgnoreArgumentBinding()
        {
            // arrange
            DirectiveTypeDescriptor<CustomDirective> descriptor =
                DirectiveTypeDescriptor.New<CustomDirective>(Context);

            // act
            descriptor.Argument(t => t.FieldA).Ignore();

            // assert
            DirectiveTypeDefinition description =
                descriptor.CreateDefinition();
            Assert.Collection(description.Arguments,
                t => Assert.Equal("fieldB", t.Name));
        }

        [Fact]
        public void MethodsAreNotAllowedAsArguments()
        {
            // arrange
            DirectiveTypeDescriptor<CustomDirective> descriptor =
                DirectiveTypeDescriptor.New<CustomDirective>(Context);

            // act
            Action action = () => descriptor.Argument(t => t.Foo()).Ignore();

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void DeclareLocation()
        {
            // arrange
            DirectiveTypeDescriptor descriptor =
                DirectiveTypeDescriptor.New(Context, "Foo");

            // act
            descriptor.Location(DirectiveLocation.Enum);
            descriptor.Location(DirectiveLocation.Enum);
            descriptor.Location(DirectiveLocation.EnumValue);

            // assert
            DirectiveTypeDefinition description =
                descriptor.CreateDefinition();
            Assert.Collection(description.Locations,
                t => Assert.Equal(DirectiveLocation.Enum, t),
                t => Assert.Equal(DirectiveLocation.EnumValue, t));
        }

        public class CustomDirective
        {
            public string FieldA { get; }
            public string FieldB { get; }
            public string Foo() => throw new NotSupportedException();
        }
    }
}
