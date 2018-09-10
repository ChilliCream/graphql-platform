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
            var descriptor = new DirectiveDescriptor();

            // act
            IDirectiveTypeDescriptor desc = descriptor;
            desc.Name("Foo");

            // assert
            Assert.Equal("Foo", descriptor.CreateDescription().Name);
        }

        [Fact]
        public void DeclareNullName()
        {
            // arrange
            var descriptor = new DirectiveDescriptor();

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
            var descriptor = new DirectiveDescriptor();

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
            var descriptor = new DirectiveDescriptor();

            // act
            Action a = () => descriptor.CreateDescription();

            // assert
            Assert.Throws<InvalidOperationException>(a);
        }

        [Fact]
        public void DeclareDescription()
        {
            // arrange
            var descriptor = new DirectiveDescriptor();

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
            var descriptor = new DirectiveDescriptor();

            // act
            IDirectiveTypeDescriptor desc = descriptor;
            desc.Name("Foo");
            desc.Argument("arg").Type<BooleanType>(); ;

            // assert
            DirectiveDescription description = descriptor.CreateDescription();
            Assert.Equal("arg", description.Arguments.Single().Name);
        }

        [Fact]
        public void DeclareLocation()
        {
            // arrange
            var descriptor = new DirectiveDescriptor();

            // act
            IDirectiveTypeDescriptor desc = descriptor;
            desc.Name("Foo");
            desc.Location(DirectiveLocation.Enum);
            desc.Location(DirectiveLocation.Enum);
            desc.Location(DirectiveLocation.EnumValue);

            // assert
            DirectiveDescription description = descriptor.CreateDescription();
            Assert.Collection(description.Locations,
                t => Assert.Equal(DirectiveLocation.Enum, t),
                t => Assert.Equal(DirectiveLocation.EnumValue, t));
        }
    }
}
