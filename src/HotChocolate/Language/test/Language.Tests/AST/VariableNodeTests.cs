using System;
using Xunit;

namespace HotChocolate.Language
{
    public class VariableNodeTests
    {
        [Fact]
        public void Create_Name_Foo_NameIsPassed()
        {
            // arrange
            var name = new NameNode("foo");

            // act
            var node = new VariableNode(name);

            // assert
            Assert.Equal(name, node.Name);
        }

        [Fact]
        public void Create_Name_NullFoo_NameIsPassed()
        {
            // arrange
            var name = new NameNode("foo");

            // act
            var node = new VariableNode(null, name);

            // assert
            Assert.Equal(name, node.Name);
        }

        [Fact]
        public void Create_Name_LocationFoo_LocationAndNameIsPassed()
        {
            // arrange
            var name = new NameNode("foo");
            Location location = AstTestHelper.CreateDummyLocation();

            // act
            var node = new VariableNode(location, name);

            // assert
            Assert.Equal(location, node.Location);
            Assert.Equal(name, node.Name);
        }

        [Fact]
        public void Create_Name_Null_ArgumentNullException()
        {
            // arrange
            // act
            Action action = () => new VariableNode((NameNode)null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void Create_Name_LocationNull_ArgumentNullException()
        {
            // arrange
            Location location = AstTestHelper.CreateDummyLocation();

            // act
            Action action = () => new VariableNode(location, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void WithName_Bar_NewObjectHasNewName()
        {
            // arrange
            var foo = new NameNode("foo");
            var bar = new NameNode("bar");
            var node = new VariableNode(foo);

            // act
            node = node.WithName(bar);

            // assert
            Assert.Equal(bar, node.Name);
        }

        [Fact]
        public void WithLocation_Location_NewObjectHasNewLocation()
        {
            // arrange
            var foo = new NameNode("foo");
            var node = new VariableNode(foo);
            Location location = AstTestHelper.CreateDummyLocation();

            // act
            node = node.WithLocation(location);

            // assert
            Assert.Equal(location, node.Location);
        }
    }
}
