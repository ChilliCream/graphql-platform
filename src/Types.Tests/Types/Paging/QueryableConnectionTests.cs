using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Paging
{
    public class QueryableConnectionTests
    {
        [Fact]
        public async Task TakeFirst()
        {
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new PagingDetails
            {
                First = 2
            };

            var connection = new QueryableConnection<string>(
                list.AsQueryable(), pagingDetails);

            ICollection<Edge<string>> edges = await connection
                .GetEdgesAsync(CancellationToken.None);

            Assert.Collection(edges,
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
        }

        [Fact]
        public async Task TakeLast()
        {
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var pagingDetails = new PagingDetails
            {
                Last = 2
            };

            var connection = new QueryableConnection<string>(
                list.AsQueryable(), pagingDetails);

            ICollection<Edge<string>> edges = await connection
                .GetEdgesAsync(CancellationToken.None);

            Assert.Collection(edges,
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
        }

        [Fact]
        public async Task TakeFirstAfter()
        {
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var connection = new QueryableConnection<string>(
                list.AsQueryable(), new PagingDetails { First = 1 });

            ICollection<Edge<string>> edges = await connection
                .GetEdgesAsync(CancellationToken.None);

            var pagingDetails = new PagingDetails
            {
                After = edges.First().Cursor,
                First = 2
            };

            connection = new QueryableConnection<string>(
                list.AsQueryable(), pagingDetails);

            edges = await connection
                .GetEdgesAsync(CancellationToken.None);

            Assert.Collection(edges,
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
        }

        [Fact]
        public async Task TakeLastBefore()
        {
            var list = new List<string> { "a", "b", "c", "d", "e", "f", "g", };

            var connection = new QueryableConnection<string>(
                list.AsQueryable(), new PagingDetails { First = 5 });

            ICollection<Edge<string>> edges = await connection
                .GetEdgesAsync(CancellationToken.None);

            var pagingDetails = new PagingDetails
            {
                Before = edges.Last().Cursor,
                Last = 2
            };

            connection = new QueryableConnection<string>(
                list.AsQueryable(), pagingDetails);

            edges = await connection
                .GetEdgesAsync(CancellationToken.None);

            Assert.Collection(edges,
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
        }

        private int GetPositionFromCursor(string cursor)
        {
            var properties = Base64Serializer
                .Deserialize<Dictionary<string, object>>(cursor);
            return Convert.ToInt32(properties["__position"]);
        }
    }
}
