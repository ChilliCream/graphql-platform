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
                _ => throw new NotSupportedException());

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
            void Action() => new Connection<string>(
                edges,
                null!,
                _ => throw new NotSupportedException());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void CreateConnection_EdgesNull_ArgumentNullException()
        {
            // arrange
            var pageInfo = new ConnectionPageInfo(true, true, "a", "b", null);

            // act
            void Action() => new Connection<string>(
                null!,
                pageInfo,
                _ => throw new NotSupportedException());

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void CreateConnection_CountIsNull_ArgumentNullException()
        {
            // arrange
            var pageInfo = new ConnectionPageInfo(true, true, "a", "b", null);
            var edges = new List<Edge<string>>();

            // act
            void Action() => new Connection<string>(
                edges,
                pageInfo,
                null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }
    }
}
