using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    [Obsolete]
    public class QueryableObjectSortVisitorTests
        : TypeTestBase
    {
        [Fact]
        public void Ctor_InitialTypeNull_ShouldThrowArgumentNullException()
        {
            // arrange

            // act
            Func<QueryableSortVisitorContext> createVisitor
                = () => new QueryableSortVisitorContext(
                    null, typeof(Foo), false);

            // assert
            Assert.Throws<ArgumentNullException>(createVisitor);
        }

        [Fact]
        public void Ctor_SourceTypeNull_ShouldThrowArgumentNullException()
        {
            // arrange
            FooSortType sortType = CreateType(new FooSortType());

            // act
            Func<QueryableSortVisitorContext> createVisitor
                = () => new QueryableSortVisitorContext(
                    sortType, null, false);

            // assert
            Assert.Throws<ArgumentNullException>(createVisitor);
        }

        [Fact]
        public void Sort_ComparableAsc_ShouldSortByStringNullableAsc()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new ObjectValueNode(
                        new ObjectFieldNode("baz",
                    new EnumValueNode(SortOperationKind.Asc)
                    )
                )
            ));

            FooSortType sortType = CreateType(new FooSortType());

            IQueryable<Foo> a = new[]
            {
                new Foo {Bar =new Bar{Baz= "b" } },
                new Foo {Bar =new Bar{Baz= null}},
                new Foo {Bar =new Bar{Baz= "c"}}
            }.AsQueryable();

            // act
            var context = new QueryableSortVisitorContext(sortType, typeof(Foo), false);
            QueryableSortVisitor.Default.Visit(value, context);
            ICollection<Foo> aFiltered = context.Sort(a).ToList();

            // assert
            Assert.Collection(aFiltered,
                foo => Assert.Null(foo.Bar.Baz),
                foo => Assert.Equal("b", foo.Bar.Baz),
                foo => Assert.Equal("c", foo.Bar.Baz)
            );
        }

        [Fact]
        public void Sort_ComparableAsc_ShouldSortByStringNullableObjectAsc()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new ObjectValueNode(
                        new ObjectFieldNode("baz",
                    new EnumValueNode(SortOperationKind.Asc)
                    )
                )
            ));

            FooSortType sortType = CreateType(new FooSortType());

            IQueryable<Foo> a = new[]
            {
                new Foo {Bar =new Bar{Baz= "b" } },
                new Foo {},
                new Foo {Bar =new Bar{Baz= "c"}}
            }.AsQueryable();

            // act
            var context = new QueryableSortVisitorContext(sortType, typeof(Foo), true);
            QueryableSortVisitor.Default.Visit(value, context);
            ICollection<Foo> aFiltered = context.Sort(a).ToList();

            // assert
            Assert.Collection(aFiltered,
                foo => Assert.Null(foo.Bar),
                foo => Assert.Equal("b", foo.Bar.Baz),
                foo => Assert.Equal("c", foo.Bar.Baz)
            );
        }

        [Fact]
        public void Sort_ComparableAsc_ShouldSortByStringWithNullableObjectInRootAsc()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new ObjectValueNode(
                        new ObjectFieldNode("baz",
                    new EnumValueNode(SortOperationKind.Asc)
                    )
                )
            ));

            FooSortType sortType = CreateType(new FooSortType());

            IQueryable<Foo> a = new[]
            {
                new Foo {Bar =new Bar{Baz= "c"}},
                new Foo {Bar =new Bar{Baz= "b" }},
                null
            }.AsQueryable();

            // act
            var context = new QueryableSortVisitorContext(sortType, typeof(Foo), true);
            QueryableSortVisitor.Default.Visit(value, context);
            ICollection<Foo> aFiltered = context.Sort(a).ToList();

            // assert
            Assert.Collection(aFiltered,
                foo => Assert.Null(foo),
                foo => Assert.Equal("b", foo.Bar.Baz),
                foo => Assert.Equal("c", foo.Bar.Baz)
            );
        }

        [Fact]
        public void Sort_ComparableAsc_ShouldSortByStringAsc()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new ObjectValueNode(
                        new ObjectFieldNode("baz",
                    new EnumValueNode(SortOperationKind.Asc)
                    )
                )
            ));

            FooSortType sortType = CreateType(new FooSortType());

            IQueryable<Foo> a = new[]
            {
                new Foo {Bar =new Bar{Baz= "b" } },
                new Foo {Bar =new Bar{Baz= "a"}},
                new Foo {Bar =new Bar{Baz= "c"}}
            }.AsQueryable();

            // act
            var context = new QueryableSortVisitorContext(sortType, typeof(Foo), false);
            QueryableSortVisitor.Default.Visit(value, context);
            ICollection<Foo> aFiltered = context.Sort(a).ToList();

            // assert
            Assert.Collection(aFiltered,
                foo => Assert.Equal("a", foo.Bar.Baz),
                foo => Assert.Equal("b", foo.Bar.Baz),
                foo => Assert.Equal("c", foo.Bar.Baz)
            );
        }

        [Fact]
        public void Sort_ComparableDesc_ShouldSortByStringAsc()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("bar",
                    new ObjectValueNode(
                        new ObjectFieldNode("baz",
                    new EnumValueNode(SortOperationKind.Desc)
                    )
                )
            ));

            FooSortType sortType = CreateType(new FooSortType());

            IQueryable<Foo> a = new[]
            {
                new Foo {Bar =new Bar{Baz= "b"}},
                new Foo {Bar =new Bar{Baz= "a"}},
                new Foo {Bar =new Bar{Baz= "c"}}
            }.AsQueryable();

            // act
            var context = new QueryableSortVisitorContext(sortType, typeof(Foo), false);
            QueryableSortVisitor.Default.Visit(value, context);
            ICollection<Foo> aFiltered = context.Sort(a).ToList();

            // assert
            Assert.Collection(aFiltered,
                foo => Assert.Equal("c", foo.Bar.Baz),
                foo => Assert.Equal("b", foo.Bar.Baz),
                foo => Assert.Equal("a", foo.Bar.Baz)
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
                new Foo {Bar =new Bar{Baz= "b"}},
                new Foo {Bar =new Bar{Baz= "a"}},
                new Foo {Bar =new Bar{Baz= "c"}}
            }.AsQueryable();

            // act
            var context = new QueryableSortVisitorContext(sortType, typeof(Foo), false);
            QueryableSortVisitor.Default.Visit(value, context);
            IQueryable<Foo> aFiltered = context.Sort(a);

            // assert
            Assert.Same(a, aFiltered);
            Assert.Collection(aFiltered,
                foo => Assert.Equal("b", foo.Bar.Baz),
                foo => Assert.Equal("a", foo.Bar.Baz),
                foo => Assert.Equal("c", foo.Bar.Baz)
            );
        }

        public class FooSortType
            : SortInputType<Foo>
        {
            protected override void Configure(
                ISortInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.SortableObject(t => t.Bar);
            }
        }

        public class Foo
        {
            public Bar Bar { get; set; }
        }

#nullable enable
        public class Bar
        {
            public string? Baz { get; set; }
        }
    }
}
