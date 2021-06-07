using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace HotChocolate.Types.Pagination
{
    public class EnumerableCursorPagingExtensionsTests
    {
        [Fact]
        public async Task ApplyPagination_After_2_First_2()
        {
            // arrange
            Foo[] data = Enumerable.Range(0, 10).Select(Foo.Create).ToArray();

            // act
            Connection result =
                await data.ApplyCursorPaginationAsync(after: ToBase64(1), first: 2);

            // assert
            Assert.Equal(2, ToFoo(result).First().Index);
            Assert.Equal(3, ToFoo(result).Last().Index);
            Assert.True(result.Info.HasNextPage);
            Assert.True(result.Info.HasPreviousPage);
            Assert.Equal(ToBase64(2), result.Info.StartCursor);
            Assert.Equal(ToBase64(3), result.Info.EndCursor);
            Assert.Equal(10, await result.GetTotalCountAsync(default));
        }

        private static string ToBase64(int i) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(i.ToString()));

        private static IEnumerable<Foo> ToFoo(Connection connection)
        {
            return connection.Edges.Select(x => x.Node).OfType<Foo>();
        }

        public class Foo
        {
            public Foo(int index)
            {
                Index = index;
            }

            public int Index { get; }

            public static Foo Create(int index) => new(index);
        }
    }
}
