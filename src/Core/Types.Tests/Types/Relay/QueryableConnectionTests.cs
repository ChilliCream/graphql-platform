using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using Moq;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class QueryableConnectionTests
    {
        [Fact]
        public async Task TakeFirst()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };
            var connectionFactory = new QueryableConnectionResolver<string>();

            // act
            IConnection connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(2),
                true);

            // assert
            Assert.Collection(connection.Edges,
                t =>
                {
                    Assert.Equal("a", t.Node);
                    Assert.Equal(0, GetPositionFromCursor(t.Cursor));
                },
                t =>
                {
                    Assert.Equal("b", t.Node);
                    Assert.Equal(1, GetPositionFromCursor(t.Cursor));
                });

            Assert.False(
                connection.PageInfo.HasPreviousPage,
                "HasPreviousPage");

            Assert.True(
                connection.PageInfo.HasNextPage,
                "HasNextPage");
        }

        [Fact]
        public async Task TakeLast()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };
            var connectionFactory = new QueryableConnectionResolver<string>();

            // act
            IConnection connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(last: 2),
                true);

            // assert
            Assert.Collection(connection.Edges,
                t =>
                {
                    Assert.Equal("f", t.Node);
                    Assert.Equal(5, GetPositionFromCursor(t.Cursor));
                },
                t =>
                {
                    Assert.Equal("g", t.Node);
                    Assert.Equal(6, GetPositionFromCursor(t.Cursor));
                });

            Assert.True(
                connection.PageInfo.HasPreviousPage,
                "HasPreviousPage");

            Assert.False(
                connection.PageInfo.HasNextPage,
                "HasNextPage");
        }

        [Fact]
        public async Task TakeFirstAfter()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };
            var connectionFactory = new QueryableConnectionResolver<string>();

            IConnection connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(1),
                true);

            // act
            connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(2, after: connection.PageInfo.StartCursor),
                true);

            // assert
            Assert.Collection(connection.Edges,
                t =>
                {
                    Assert.Equal("b", t.Node);
                    Assert.Equal(1, GetPositionFromCursor(t.Cursor));
                },
                t =>
                {
                    Assert.Equal("c", t.Node);
                    Assert.Equal(2, GetPositionFromCursor(t.Cursor));
                });

            Assert.True(
                connection.PageInfo.HasPreviousPage,
                "HasPreviousPage");

            Assert.True(
                connection.PageInfo.HasNextPage,
                "HasNextPage");
        }

        [Fact]
        public async Task TakeTwoAfterSecondTime()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };
            var connectionFactory = new QueryableConnectionResolver<string>();

            IConnection connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(2),
                true);

            connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(2, after: connection.PageInfo.EndCursor),
                true);

            // act
            connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(2, after: connection.PageInfo.EndCursor),
                true);

            // assert
            Assert.Collection(connection.Edges,
                t =>
                {
                    Assert.Equal("e", t.Node);
                    Assert.Equal(4, GetPositionFromCursor(t.Cursor));
                },
                t =>
                {
                    Assert.Equal("f", t.Node);
                    Assert.Equal(5, GetPositionFromCursor(t.Cursor));
                });

            Assert.True(
                connection.PageInfo.HasPreviousPage,
                "HasPreviousPage");

            Assert.True(
                connection.PageInfo.HasNextPage,
                "HasNextPage");
        }

        [Fact]
        public async Task TakeLastBefore()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };
            var connectionFactory = new QueryableConnectionResolver<string>();

            IConnection connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(5),
                true);

            // act
            connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(last: 2, before: connection.PageInfo.EndCursor),
                true);

            // assert
            Assert.Collection(connection.Edges,
                t =>
                {
                    Assert.Equal("c", t.Node);
                    Assert.Equal(2, GetPositionFromCursor(t.Cursor));
                },
                t =>
                {
                    Assert.Equal("d", t.Node);
                    Assert.Equal(3, GetPositionFromCursor(t.Cursor));
                });

            Assert.True(
                connection.PageInfo.HasPreviousPage,
                "HasPreviousPage");

            Assert.True(
                connection.PageInfo.HasNextPage,
                "HasNextPage");
        }

        [Fact]
        public async Task HasNextPage_True()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };
            var connectionFactory = new QueryableConnectionResolver<string>();

            // act
            IConnection connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(5),
                true);

            // assert
            Assert.True(connection.PageInfo.HasNextPage);
        }

        [Fact]
        public async Task HasNextPage_False()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };
            var connectionFactory = new QueryableConnectionResolver<string>();

            // act
            IConnection connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(7),
                true);

            // assert
            Assert.False(connection.PageInfo.HasNextPage);
        }

        [Fact]
        public async Task HasPrevious_True()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };
            var connectionFactory = new QueryableConnectionResolver<string>();

            IConnection connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(1),
                true);

            // act
            connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(first: 2, after: connection.PageInfo.StartCursor),
                true);

            // assert
            Assert.True(connection.PageInfo.HasPreviousPage);
        }

        [Fact]
        public async Task HasPrevious_False()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };
            var connectionFactory = new QueryableConnectionResolver<string>();

            // act
            IConnection connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(1),
                true);

            // assert
            Assert.False(connection.PageInfo.HasPreviousPage);
        }

        [Fact]
        public async Task TotalCount()
        {
            // arrange
            var context = new Mock<IMiddlewareContext>();
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };
            var connectionFactory = new QueryableConnectionResolver<string>();

            // act
            IConnection connection = await connectionFactory.ResolveAsync(
                context.Object,
                list.AsQueryable(),
                new ConnectionArguments(1),
                true);

            // assert
            Assert.Equal(7, connection.PageInfo.TotalCount);
        }

        private int GetPositionFromCursor(string cursor)
        {
            return int.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(cursor)));
        }
    }
}
