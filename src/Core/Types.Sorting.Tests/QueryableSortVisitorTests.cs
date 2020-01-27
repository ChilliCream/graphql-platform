using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    public class QueryableSortVisitorTests
        : TypeTestBase
    {
        [Fact]
        public void Ctor_InitialTypeNull_ShouldThrowArgumentNullException()
        {
            // arrange

            // act
            Func<QueryableSortVisitor> createVisitor
                = () => new QueryableSortVisitor(
                    null, typeof(Foo));

            // assert
            Assert.Throws<ArgumentNullException>(createVisitor);
        }

        [Fact]
        public void Sort_ComparableAsc_PrefilterInResolver()
        {
            // arrange
            IQueryable<Foo> data = new[] {
                    new Foo { Bar = "baz", Baz = "a" },
                    new Foo { Bar = "aa", Baz = "b" },
                    new Foo { Bar = "zz", Baz = "b" }
                }.AsQueryable().OrderBy(x => x.Baz);

            ISchema schema = SchemaBuilder.New()
                .AddQueryType(ctx =>
                {
                    ctx.Field("foo")
                    .Resolver(data)
                    .Type<NonNullType<ListType<NonNullType<ObjectType<Foo>>>>>()
                    .UseSorting();
                })
                .Create();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery("{ foo(order_by: { bar: DESC }) { bar } }")
                    .Create();

            // act
            IExecutionResult result = schema.MakeExecutable().Execute(request);

            // assert
            result.MatchSnapshot();
        }


        [Fact]
        public void Sort_ComparableAsc_ShouldSortByStringAsc()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new EnumValueNode(SortOperationKind.Asc)
                    )
            );

            FooSortType sortType = CreateType(new FooSortType());

            IQueryable<Foo> a = new[]
            {
                new Foo {Bar = "b"}, new Foo {Bar = "a"}, new Foo {Bar = "c"}
            }.AsQueryable();

            // act
            var filter = new QueryableSortVisitor(
                sortType, typeof(Foo));
            value.Accept(filter);
            ICollection<Foo> aFiltered = filter.Sort(a).ToList();

            // assert
            Assert.Collection(aFiltered,
                foo => Assert.Equal("a", foo.Bar),
                foo => Assert.Equal("b", foo.Bar),
                foo => Assert.Equal("c", foo.Bar)
            );
        }

        [Fact]
        public void Sort_ComparableDesc_ShouldSortByStringAsc()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new EnumValueNode(SortOperationKind.Desc)
                    )
            );

            FooSortType sortType = CreateType(new FooSortType());

            IQueryable<Foo> a = new[]
            {
                new Foo {Bar = "b"}, new Foo {Bar = "a"}, new Foo {Bar = "c"}
            }.AsQueryable();

            // act
            var filter = new QueryableSortVisitor(
                sortType, typeof(Foo));
            value.Accept(filter);
            ICollection<Foo> aFiltered = filter.Sort(a).ToList();

            // assert
            Assert.Collection(aFiltered,
                foo => Assert.Equal("c", foo.Bar),
                foo => Assert.Equal("b", foo.Bar),
                foo => Assert.Equal("a", foo.Bar)
            );
        }

        [Fact]
        public void Sort_NoSortSpecified_ShouldReturnUnalteredSource()
        {
            // arrange
            var value = new ObjectValueNode();

            FooSortType sortType = CreateType(new FooSortType());

            IQueryable<Foo> a = new[]
            {
                new Foo {Bar = "b"}, new Foo {Bar = "a"}, new Foo {Bar = "c"}
            }.AsQueryable();

            // act
            var filter = new QueryableSortVisitor(
                sortType, typeof(Foo));
            value.Accept(filter);
            IQueryable<Foo> aFiltered = filter.Sort(a);

            // assert
            Assert.Same(a, aFiltered);
            Assert.Collection(aFiltered,
                foo => Assert.Equal("b", foo.Bar),
                foo => Assert.Equal("a", foo.Bar),
                foo => Assert.Equal("c", foo.Bar)
            );
        }

        [Fact]
        public void Sort_Nullable_ShouldSortNullableProperlyAsc()
        {
            // arrange 
            var value = new ObjectValueNode(
                new ObjectFieldNode("nullableInt",
                    new EnumValueNode(SortOperationKind.Asc)
                    )
            );

            FooSortType sortType = CreateType(new FooSortType());

            IQueryable<Foo> a = new[]
            {
                new Foo {Bar = "b"},
                new Foo {Bar = "c", NullableInt = 2},
                new Foo {Bar = "a", NullableInt = 1}
            }.AsQueryable();

            // act
            var filter = new QueryableSortVisitor(
                sortType, typeof(Foo));
            value.Accept(filter);
            IQueryable<Foo> aFiltered = filter.Sort(a);

            // assert 
            Assert.Collection(aFiltered,
                foo => Assert.Equal("b", foo.Bar),
                foo => Assert.Equal("a", foo.Bar),
                foo => Assert.Equal("c", foo.Bar)
            );
        }

        [Fact]
        public void Sort_Nullable_ShouldSortNullableProperlyDesc()
        {
            // arrange 
            var value = new ObjectValueNode(
                new ObjectFieldNode("nullableInt",
                    new EnumValueNode(SortOperationKind.Desc)
                    )
            );

            FooSortType sortType = CreateType(new FooSortType());

            IQueryable<Foo> a = new[]
            {
                new Foo {Bar = "b"},
                new Foo {Bar = "c", NullableInt = 2},
                new Foo {Bar = "a", NullableInt = 1}
            }.AsQueryable();

            // act
            var filter = new QueryableSortVisitor(
                sortType, typeof(Foo));
            value.Accept(filter);
            IQueryable<Foo> aFiltered = filter.Sort(a);

            // assert 
            Assert.Collection(aFiltered,
                foo => Assert.Equal("c", foo.Bar),
                foo => Assert.Equal("a", foo.Bar),
                foo => Assert.Equal("b", foo.Bar)
            );
        }

        public class FooSortType
            : SortInputType<Foo>
        {
            protected override void Configure(
                ISortInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Sortable(t => t.Bar);
            }
        }

        public class Foo
        {
            public int? NullableInt { get; set; }
            public string Bar { get; set; }
            public string Baz { get; set; }
        }
    }
}
