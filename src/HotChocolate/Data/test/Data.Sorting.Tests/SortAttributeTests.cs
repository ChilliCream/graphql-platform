using System.Collections.Generic;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Sorting
{
    public class SortAttributeTests
    {
        [Fact]
        public void Create_Schema_With_SortType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query1>()
                .AddSorting()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Create_Schema_With_SortType_With_Fluent_API()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Query2>()
                .AddSorting()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class Query1
        {
            [UseSorting]
            public IEnumerable<Foo> Foos { get; } = new[]
            {
                new Foo {Bar = "aa", Baz = 1, Qux = 1},
                new Foo {Bar = "ba", Baz = 1},
                new Foo {Bar = "ca", Baz = 2},
                new Foo {Bar = "ab", Baz = 2},
                new Foo {Bar = "ac", Baz = 2},
                new Foo {Bar = "ad", Baz = 2},
                new Foo {Bar = null!, Baz = 0}
            };
        }

        public class Query2
        {
            [UseSorting(Type = typeof(FooSortType))]
            public IEnumerable<Foo> Foos { get; } = new[]
            {
                new Foo {Bar = "aa", Baz = 1, Qux = 1},
                new Foo {Bar = "ba", Baz = 1},
                new Foo {Bar = "ca", Baz = 2},
                new Foo {Bar = "ab", Baz = 2},
                new Foo {Bar = "ac", Baz = 2},
                new Foo {Bar = "ad", Baz = 2},
                new Foo {Bar = null!, Baz = 0}
            };
        }

        public class FooSortType : SortInputType<Foo>
        {
            protected override void Configure(ISortInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.BindFieldsExplicitly().Field(m => m.Bar);
            }
        }

        public class Foo
        {
            public string Bar { get; set; } = default!;

            [GraphQLType(typeof(NonNullType<IntType>))]
            public long Baz { get; set; }

            [GraphQLType(typeof(IntType))] public int? Qux { get; set; }
        }
    }
}
