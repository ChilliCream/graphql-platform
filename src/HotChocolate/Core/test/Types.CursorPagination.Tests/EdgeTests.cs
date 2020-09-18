using System;
using Xunit;

namespace HotChocolate.Types.Pagination
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
            void Action() => new Edge<string>("abc", null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void CreateEdge_CursorIsEmpty_ArgumentNullException()
        {
            // arrange
            // act
            void Action() => new Edge<string>("abc", string.Empty);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }
    }
}
