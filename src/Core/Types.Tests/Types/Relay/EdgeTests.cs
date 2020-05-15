using System;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class EdgeTests
    {
        [InlineData("abc", "cde")]
        [InlineData("cde", null)]
        [Theory]
        public void CreateEdge_ArgumentsArePassedCorrectly(
            string cursor, string node)
        {
            // arrange
            // act
            var edge = new Edge<string>(node, cursor);

            // assert
            Assert.Equal(cursor, edge.Cursor);
            Assert.Equal(node, edge.Node);
        }

        [Fact]
        public void CreateEdge_CursorIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action a = () => new Edge<string>("abc", null);

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void CreateEdge_CursorIsEmpty_ArgumentNullException()
        {
            // arrange
            // act
            Action a = () => new Edge<string>("abc", string.Empty);

            // assert
            Assert.Throws<ArgumentException>(a);
        }
    }
}
