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
            var descriptor = DirectiveTypeDescriptor.New(Context);

            // act
            descriptor.Name("Foo");

            // assert
            Assert.Equal("Foo", descriptor.CreateDefinition().Name);
        }

        [Fact]
        public void InferName()
        {
            // arrange
            // act
            var descriptor =
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
            var descriptor =
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
            var descriptor = DirectiveTypeDescriptor.New(Context);

            // act
            Action a = () => descriptor.Name(null);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void DeclareEmptyName()
        {
            // arrange
            var descriptor = DirectiveTypeDescriptor.New(Context);

            // act
            Action a = () => descriptor.Name(string.Empty);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void DeclareDescription()
        {
            // arrange
            var descriptor = DirectiveTypeDescriptor.New(Context);

            // act
            descriptor.Description("Desc");

            // assert
            Assert.Equal("Desc", descriptor.CreateDefinition().Description);
        }

        [Fact]
        public void DeclareArgument()
        {
            // arrange
            var descriptor = DirectiveTypeDescriptor.New(Context);

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
            var descriptor = DirectiveTypeDescriptor.New<CustomDirective>(Context);

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
            var descriptor =
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
            var descriptor = DirectiveTypeDescriptor.New<CustomDirective>(Context);

            // act
            descriptor.Argument(t => t.FieldA).Type<NonNullType<StringType>>();

            // assert
            DirectiveTypeDefinition description = descriptor.CreateDefinition();
            Assert.Collection(description.Arguments,
                t => Assert.Equal(
                    typeof(NonNullType<StringType>),
                    Assert.IsType<ExtendedTypeReference>(t.Type).Type.Source),
                t => Assert.Equal(
                    typeof(string),
                    Assert.IsType<ExtendedTypeReference>(t.Type).Type.Source));
        }

        [Fact]
        public void DeclareArgumentAndSpecifyClrType()
        {
            // arrange
            var descriptor = DirectiveTypeDescriptor.New<CustomDirective>(Context);

            // act
            descriptor.Argument(t => t.FieldA).Type(typeof(NonNullType<StringType>));

            // assert
            DirectiveTypeDefinition description = descriptor.CreateDefinition();
            Assert.Collection(description.Arguments,
                t => Assert.Equal(
                    typeof(NonNullType<StringType>),
                    Assert.IsType<ExtendedTypeReference>(t.Type).Type.Source),
                t => Assert.Equal(
                    typeof(string),
                    Assert.IsType<ExtendedTypeReference>(t.Type).Type.Source));
        }

        [Fact]
        public void IgnoreArgumentBinding()
        {
            // arrange
            var descriptor =
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
        public void UnignoreArgumentBinding()
        {
            // arrange
            var descriptor =
                DirectiveTypeDescriptor.New<CustomDirective>(Context);

            // act
            descriptor.Argument(t => t.FieldA).Ignore();
            descriptor.Argument(t => t.FieldA).Ignore(false);

            // assert
            DirectiveTypeDefinition description =
                descriptor.CreateDefinition();
            Assert.Collection(description.Arguments,
                t => Assert.Equal("fieldA", t.Name),
                t => Assert.Equal("fieldB", t.Name));
        }

        [Fact]
        public void MethodsAreNotAllowedAsArguments()
        {
            // arrange
            var descriptor =
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
            var descriptor = DirectiveTypeDescriptor.New(Context);

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
