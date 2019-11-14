using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    public class QueryableObjectSortVisitorTests
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
            var filter = new QueryableSortVisitor(
                sortType, typeof(Foo));
            value.Accept(filter);
            ICollection<Foo> aFiltered = filter.Sort(a).ToList();

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
            var filter = new QueryableSortVisitor(
                sortType, typeof(Foo));
            value.Accept(filter);
            ICollection<Foo> aFiltered = filter.Sort(a).ToList();

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
            var filter = new QueryableSortVisitor(
                sortType, typeof(Foo));
            value.Accept(filter);
            IQueryable<Foo> aFiltered = filter.Sort(a);

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
        public class Bar
        {
            public string Baz { get; set; }
        }
    }
}
