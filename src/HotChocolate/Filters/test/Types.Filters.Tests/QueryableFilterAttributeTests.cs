using System.Collections.Generic;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterAttributeTests
    {
        [Fact]
        public void Create_Schema_With_FilterType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query1>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Schema_With_FilterType_With_Fluent_API()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query2>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Query1
        {
            [UseFiltering]
            public IEnumerable<Foo> Foos { get; } = new[]
            {
                new Foo { Bar = "aa", Baz = 1, Qux = 1 },
                new Foo { Bar = "ba", Baz = 1 },
                new Foo { Bar = "ca", Baz = 2 },
                new Foo { Bar = "ab", Baz = 2 },
                new Foo { Bar = "ac", Baz = 2 },
                new Foo { Bar = "ad", Baz = 2 },
                new Foo { Bar = null, Baz = 0 }
            };
        }

        public class Query2
        {
            [UseFiltering(FilterType = typeof(FooFilterType))]
            public IEnumerable<Foo> Foos { get; } = new[]
            {
                new Foo { Bar = "aa", Baz = 1, Qux = 1 },
                new Foo { Bar = "ba", Baz = 1 },
                new Foo { Bar = "ca", Baz = 2 },
                new Foo { Bar = "ab", Baz = 2 },
                new Foo { Bar = "ac", Baz = 2 },
                new Foo { Bar = "ad", Baz = 2 },
                new Foo { Bar = null, Baz = 0 }
            };
        }

        public class FooFilterType : FilterInputType<Foo>
        {
            protected override void Configure(IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.BindFieldsExplicitly()
                    .Filter(m => m.Bar)
                    .BindFiltersExplicitly()
                    .AllowEquals();
            }
        }

        public class Foo
        {
            public string Bar { get; set; }

            [GraphQLType(typeof(NonNullType<IntType>))]
            public long Baz { get; set; }

            [GraphQLType(typeof(IntType))]
            public int? Qux { get; set; }
        }
    }
}
