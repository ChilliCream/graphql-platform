using System;
using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Types.Paging
{
    public class ConnectionTests
    {
        [Fact]
        public void CreateConnection_PageInfoAndEdges_PassedCorrectly()
        {
            // arrange
            var pageInfo = new PageInfo(true, true, "a", "b");
            var edges = new List<Edge<string>>();

            // act
            var connection = new Connection<string>(pageInfo, edges);

            // assert
            Assert.Equal(pageInfo, connection.PageInfo);
            Assert.Equal(edges, connection.Edges);
        }

        [Fact]
        public void CreateConnection_PageInfoNull_ArgumentNullException()
        {
            // arrange
            var edges = new List<Edge<string>>();

            // act
            Action a = () => new Connection<string>(null, edges);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CreateConnection_EdgesNull_ArgumentNullException()
        {
            // arrange
            var pageInfo = new PageInfo(true, true, "a", "b");

            // act
            Action a = () => new Connection<string>(pageInfo, null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }
    }
}
