using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class QueryableConnectionTests
    {
        [Fact]
        public async Task TakeFirst()
        {
            // arrange
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new PagingDetails
            {
                First = 2
            };

            var connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), pagingDetails);

            // act
            Connection<string> connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

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
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new PagingDetails
            {
                Last = 2
            };

            var connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), pagingDetails);

            // act
            Connection<string> connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

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
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), new PagingDetails { First = 1 });

            Connection<string> connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

            var pagingDetails = new PagingDetails
            {
                After = connection.PageInfo.StartCursor,
                First = 2
            };

            connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), pagingDetails);

            // act
            connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

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
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            // 1. Page
            var connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), new PagingDetails { First = 2 });

            Connection<string> connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

            //2. Page
            var pagingDetails = new PagingDetails
            {
                After = connection.PageInfo.EndCursor,
                First = 2
            };

            connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), pagingDetails);

            connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

            //3. Page
            pagingDetails = new PagingDetails
            {
                After = connection.PageInfo.EndCursor,
                First = 2
            };

            connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), pagingDetails);

            // act
            connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

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
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), new PagingDetails { First = 5 });

            Connection<string> connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

            var pagingDetails = new PagingDetails
            {
                Before = connection.PageInfo.EndCursor,
                Last = 2
            };

            connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), pagingDetails);

            // act
            connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

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
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new PagingDetails
            {
                First = 5
            };

            var connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), pagingDetails);

            // act
            Connection<string> connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

            // assert
            Assert.True(connection.PageInfo.HasNextPage);
        }

        [Fact]
        public async Task HasNextPage_False()
        {
            // arrange
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new PagingDetails
            {
                First = 7
            };

            var connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), pagingDetails);

            // act
            Connection<string> connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

            // assert
            Assert.False(connection.PageInfo.HasNextPage);
        }

        [Fact]
        public async Task HasPrevious_True()
        {
            // arrange
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), new PagingDetails { First = 1 });

            Connection<string> connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

            var pagingDetails = new PagingDetails
            {
                After = connection.PageInfo.StartCursor,
                First = 2
            };

            connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), pagingDetails);

            // act
            connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

            // assert
            Assert.True(connection.PageInfo.HasPreviousPage);
        }

        [Fact]
        public async Task HasPrevious_False()
        {
            // arrange
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new PagingDetails();

            var connectionFactory = new QueryableConnectionResolver<string>(
                list.AsQueryable(), pagingDetails);

            // act
            Connection<string> connection = await connectionFactory.ResolveAsync(
                CancellationToken.None);

            // assert
            Assert.False(connection.PageInfo.HasPreviousPage);
        }

        private int GetPositionFromCursor(string cursor)
        {
            Dictionary<string, object> properties = Base64Serializer
                .Deserialize<Dictionary<string, object>>(cursor);
            return Convert.ToInt32(properties["__position"]);
        }
    }
}
