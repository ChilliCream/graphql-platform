using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Lodash
{
    public class LodashTestBase
    {
        private static Bar[] _bars =
        {
            new Bar { Baz = "bar_baz_1" },
            new Bar { Baz = "bar_baz_2" },
            new Bar { Baz = "bar_baz_3" },
            new Bar { Baz = "bar_baz_4" },
        };

        private static Foo[] _foos =
        {
            new Foo
            {
                Id = 1,
                Baz = "foo_baz_1",
                Bar = _bars[0],
                BarList = _bars,
                CountBy = "a",
                Num = 1,
                DateTime = DateTime.Parse("2012-12-12T12:00:00Z")
            },
            new Foo
            {
                Id = 2,
                Baz = "foo_baz_2",
                Bar = _bars[1],
                BarList = _bars,
                CountBy = "a",
                Num = 2,
                DateTime = DateTime.Parse("2013-12-12T12:00:00Z")
            },
            new Foo
            {
                Id = 3,
                Baz = "foo_baz_3",
                Bar = _bars[2],
                BarList = _bars,
                CountBy = "b",
                Num = 1,
                DateTime = DateTime.Parse("2014-12-12T12:00:00Z")
            },
            new Foo
            {
                Id = 4,
                Baz = "foo_baz_4",
                Bar = _bars[3],
                BarList = _bars,
                CountBy = "c",
                Num = 4,
                DateTime = DateTime.Parse("2014-12-12T12:00:00Z")
            },
        };

        private static Bar?[] _nullableBars =
        {
            new Bar { Baz = "bar_baz_1" },
            new Bar { },
            new Bar { Baz = "bar_baz_2" },
            new Bar { },
            null,
        };

        private static Foo?[] _nullableFoos =
        {
            new Foo { Bar = _bars[0], BarList = _nullableBars, CountBy = "a" },
            new Foo { Baz = "foo_baz_1", BarList = _nullableBars, CountBy = "a" },
            new Foo { Baz = "foo_baz_2", Bar = _nullableBars[2], CountBy = null },
            new Foo
            {
                Baz = "foo_baz_3",
                Bar = _nullableBars[3],
                BarList = _nullableBars,
                CountBy = "c"
            },
            null,
        };

        protected ValueTask<IRequestExecutor> CreateExecutor()
        {
            return new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildRequestExecutorAsync();
        }

        public class Query
        {
            public int[] Scalars => new int[]
            {
                1,
                1,
                2,
                2,
                3,
                3,
                4,
                4
            };
            public Foo Single => _foos[0];
            public Foo[] List => _foos;
            public Foo[][] NestedList => _foos.Select(x => _foos).ToArray();
            public Foo? Nullable => null;
            public Foo?[] NullableList => _nullableFoos;

            public Foo?[]?[] NullableNestedList =>
                _nullableFoos.Select(x => x is null ? null : _nullableFoos).ToArray();
        }

        public class Foo
        {
            public int Id { get; set; }

            public string? Baz { get; set; }

            public string? CountBy { get; set; }

            public int? Num { get; set; }

            public DateTime? DateTime { get; set; }

            public Bar? Bar { get; set; }

            public Bar?[]? BarList { get; set; }
        }

        public class Bar
        {
            public string? Baz { get; set; }

            public Foo?[]? Foos => _foos;
        }
    }
}
