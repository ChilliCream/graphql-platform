using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
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

        public class FooSortType
            : SortInputType<Foo>
        {
            protected override void Configure(
                ISortInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.SortField(t => t.Bar);
            }
        }

        public class Foo
        {
            public string Bar { get; set; }
        }
    }
}
