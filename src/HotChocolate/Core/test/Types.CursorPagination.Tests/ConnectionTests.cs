using System;
using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Types.Pagination
{
    public class ConnectionTests
    {
        [Fact]
        public void CreateConnection_PageInfoAndEdges_PassedCorrectly()
        {
            // arrange
            var pageInfo = new ConnectionPageInfo(true, true, "a", "b", null);
            var edges = new List<Edge<string>>();

            // act
            var connection = new Connection(
                edges,
                pageInfo,
                ct => throw new NotSupportedException());

            // assert
            Assert.Equal(pageInfo, connection.Info);
            Assert.Equal(edges, connection.Edges);
        }

        [Fact]
        public void CreateConnection_PageInfoNull_ArgumentNullException()
        {
            // arrange
            var edges = new List<Edge<string>>();

            // act
            Action a = () => new Connection<string>(
                edges, null, ct => throw new NotSupportedException());

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CreateConnection_EdgesNull_ArgumentNullException()
        {
            // arrange
            var pageInfo = new ConnectionPageInfo(true, true, "a", "b", null);

            // act
            Action a = () => new Connection<string>(
                null, pageInfo, ct => throw new NotSupportedException());

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CreateConnection_CountIsNull_ArgumentNullException()
        {
            // arrange
            var pageInfo = new ConnectionPageInfo(true, true, "a", "b", null);
            var edges = new List<Edge<string>>();

            // act
            Action a = () => new Connection<string>(
                edges, pageInfo, null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }
    }
}
