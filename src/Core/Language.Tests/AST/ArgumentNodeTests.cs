using System;
using Xunit;

namespace HotChocolate.Language
{
    public sealed class ArgumentNodeTests
    {
        [Fact]
        public void CreateArgumentWithLocation()
        {
            // arrange
            var source = new Source("foo");
            var start = new SyntaxToken(
                TokenKind.StartOfFile, 0, 0, 1, 1, null);
            var location = new Location(source, start, start);
            var name = new NameNode("foo");
            var value = new StringValueNode("bar");

            // act
            var argument = new ArgumentNode(location, name, value);

            // assert
            Assert.Equal(NodeKind.Argument, argument.Kind);
            Assert.Equal(location, argument.Location);
            Assert.Equal(name, argument.Name);
            Assert.Equal(value, argument.Value);
        }

        [Fact]
        public void CreateArgumentWithoutLocation()
        {
            // arrange
            var name = new NameNode("foo");
            var value = new StringValueNode("bar");

            // act
            var argument = new ArgumentNode(name, value);

            // assert
            Assert.Equal(NodeKind.Argument, argument.Kind);
            Assert.Null(argument.Location);
            Assert.Equal(name, argument.Name);
            Assert.Equal(value, argument.Value);
        }

        [Fact]
        public void CreateArgumentWithConvenienceConstructor()
        {
            // arrange
            var name = "foo";
            var value = new StringValueNode("bar");

            // act
            var argument = new ArgumentNode(name, value);

            // assert
            Assert.Equal(NodeKind.Argument, argument.Kind);
            Assert.Null(argument.Location);
            Assert.Equal(name, argument.Name.Value);
            Assert.Equal(value, argument.Value);
        }

        [Fact]
        public void CreateArgumentWithoutName()
        {
            // arrange
            var value = new StringValueNode("bar");

            // act
            Action action = () => new ArgumentNode(null, null, value);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void CreateArgumentWithoutValue()
        {
            // arrange
            var name = new NameNode("foo");

            // act
            Action action = () => new ArgumentNode(null, name, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void ArgumentNode_WithNewName_NewNameIsSet()
        {
            // arrange
            var argument = new ArgumentNode(
                "foo", new StringValueNode("bar"));

            // act
            argument = argument.WithName(new NameNode("bar"));

            // assert
            Assert.Equal("bar", argument.Name.Value);
        }

        [Fact]
        public void ArgumentNode_WithNewValue_NewValueIsSet()
        {
            // arrange
            var argument = new ArgumentNode(
                "foo", new StringValueNode("bar"));

            // act
            argument = argument.WithValue(new StringValueNode("foo"));

            // assert
            Assert.Equal("foo", ((StringValueNode)argument.Value).Value);
        }
    }
}
