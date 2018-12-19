using System;
using System.Linq;
using Xunit;

namespace HotChocolate.Types
{
    public class DirectiveDescriptorTests
    {
        [Fact]
        public void DeclareName()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor();

            // act
            IDirectiveTypeDescriptor desc = descriptor;
            desc.Name("Foo");

            // assert
            Assert.Equal("Foo", descriptor.CreateDescription().Name);
        }

        [Fact]
        public void InferName()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor<CustomDirective>();

            // act
            IDirectiveTypeDescriptor desc = descriptor;

            // assert
            DirectiveTypeDescription description =
                descriptor.CreateDescription();
            Assert.Equal("CustomDirective", description.Name);
        }

        [Fact]
        public void OverrideName()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor<CustomDirective>();

            // act
            IDirectiveTypeDescriptor desc = descriptor;
            desc.Name("Foo");

            // assert
            DirectiveTypeDescription description =
                descriptor.CreateDescription();
            Assert.Equal("Foo", description.Name);
        }

        [Fact]
        public void DeclareNullName()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor();

            // act
            IDirectiveTypeDescriptor desc = descriptor;
            Action a = () => desc.Name(null);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void DeclareEmptyName()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor();

            // act
            IDirectiveTypeDescriptor desc = descriptor;
            Action a = () => desc.Name(string.Empty);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void CreateDescriptionWithEmptyName()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor();

            // act
            Action a = () => descriptor.CreateDescription();

            // assert
            Assert.Throws<InvalidOperationException>(a);
        }

        [Fact]
        public void DeclareDescription()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor();

            // act
            IDirectiveTypeDescriptor desc = descriptor;
            desc.Name("Foo");
            desc.Description("Desc");

            // assert
            Assert.Equal("Desc", descriptor.CreateDescription().Description);
        }

        [Fact]
        public void DeclareArgument()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor();

            // act
            IDirectiveTypeDescriptor desc = descriptor;
            desc.Name("Foo");
            desc.Argument("arg").Type<BooleanType>(); ;

            // assert
            DirectiveTypeDescription description =
                descriptor.CreateDescription();
            Assert.Equal("arg", description.Arguments.Single().Name);
        }

        [Fact]
        public void DeclareArgumentWithProperty()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor<CustomDirective>();

            // act
            IDirectiveTypeDescriptor<CustomDirective> desc = descriptor;

            // assert
            DirectiveTypeDescription description =
                descriptor.CreateDescription();
            Assert.Collection(description.Arguments,
                t => Assert.Equal("fieldA", t.Name),
                t => Assert.Equal("fieldB", t.Name));
        }

        [Fact]
        public void DeclareExplicitArgumentBinding()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor<CustomDirective>();

            // act
            IDirectiveTypeDescriptor<CustomDirective> desc = descriptor;
            desc.BindArguments(BindingBehavior.Explicit);
            desc.Argument(t => t.FieldA);

            // assert
            DirectiveTypeDescription description =
                descriptor.CreateDescription();
            Assert.Collection(description.Arguments,
                t => Assert.Equal("fieldA", t.Name));
        }

        [Fact]
        public void DeclareArgumentAndSpecifyType()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor<CustomDirective>();

            // act
            IDirectiveTypeDescriptor<CustomDirective> desc = descriptor;
            desc.Argument(t => t.FieldA).Type<NonNullType<StringType>>();

            // assert
            DirectiveTypeDescription description =
                descriptor.CreateDescription();
            Assert.Collection(description.Arguments,
                t => Assert.Equal(
                    typeof(NonNullType<StringType>),
                    t.TypeReference.ClrType),
                t => Assert.Equal(
                    typeof(string),
                    t.TypeReference.ClrType));
        }

        [Fact]
        public void IgnoreArgumentBinding()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor<CustomDirective>();

            // act
            IDirectiveTypeDescriptor<CustomDirective> desc = descriptor;
            desc.Argument(t => t.FieldA).Ignore();

            // assert
            DirectiveTypeDescription description =
                descriptor.CreateDescription();
            Assert.Collection(description.Arguments,
                t => Assert.Equal("fieldB", t.Name));
        }

        [Fact]
        public void MethodsAreNotAllowedAsArguments()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor<CustomDirective>();

            // act
            IDirectiveTypeDescriptor<CustomDirective> desc = descriptor;
            Action action = () => desc.Argument(t => t.Foo()).Ignore();

            // assert
            Assert.Throws<ArgumentException>(action);
        }

        [Fact]
        public void DeclareLocation()
        {
            // arrange
            var descriptor = new DirectiveTypeDescriptor();

            // act
            IDirectiveTypeDescriptor desc = descriptor;
            desc.Name("Foo");
            desc.Location(DirectiveLocation.Enum);
            desc.Location(DirectiveLocation.Enum);
            desc.Location(DirectiveLocation.EnumValue);

            // assert
            DirectiveTypeDescription description =
                descriptor.CreateDescription();
            Assert.Collection(description.Locations,
                t => Assert.Equal(DirectiveLocation.Enum, t),
                t => Assert.Equal(DirectiveLocation.EnumValue, t));
        }

        public class CustomDirective
        {
            public string FieldA { get; }
            public string FieldB { get; }
            public string Foo() => throw new NotImplementedException();
        }
    }
}
