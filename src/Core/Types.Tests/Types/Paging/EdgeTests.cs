using System;
using Xunit;

namespace HotChocolate.Types.Paging
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
            var edge = new Edge<string>(cursor, node);

            // assert
            Assert.Equal(cursor, edge.Cursor);
            Assert.Equal(node, edge.Node);
        }

        [Fact]
        public void CreateEdge_CursorIsNull_ArgumentNullException()
        {
            // arrange
            // act
            Action a = () => new Edge<string>(null, "abc");

            // assert
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void CreateEdge_CursorIsEmpty_ArgumentNullException()
        {
            // arrange
            // act
            Action a = () => new Edge<string>(string.Empty, "abc");

            // assert
            Assert.Throws<ArgumentException>(a);
        }
    }
}
