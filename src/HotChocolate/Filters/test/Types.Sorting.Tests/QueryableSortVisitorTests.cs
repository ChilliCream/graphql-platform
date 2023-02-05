using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Sorting;

[Obsolete]
public class QueryableSortVisitorTests
    : TypeTestBase
{
    [Fact]
    public void Sort_ComparableMultiple_ShouldSortByStringAscThenByStringAsc()
    {
        // arrange
        var value = new ObjectValueNode(
            new ObjectFieldNode("baz",
                new EnumValueNode(SortOperationKind.Asc)),
            new ObjectFieldNode("bar",
                new EnumValueNode(SortOperationKind.Asc)));

        var sortType = CreateType(new FooSortType());

        var a = new[]
        {
            new Foo {Bar = "b",Baz = "b"},
            new Foo {Bar = "a",Baz = "b"},
            new Foo {Bar = "c",Baz = "a"}
        }.AsQueryable();

        // act
        var context = new QueryableSortVisitorContext(
            new(new DefaultTypeConverter()), sortType, typeof(Foo), false);
        QueryableSortVisitor.Default.Visit(value, context);
        var temp = context.Sort(a);
        ICollection<Foo> aFiltered = temp.ToList();

        // assert
        Assert.Collection(aFiltered,
            foo => Assert.Equal("c", foo.Bar),
            foo => Assert.Equal("a", foo.Bar),
            foo => Assert.Equal("b", foo.Bar));
    }

    [Fact]
    public void Sort_ObjectMultiple_ShouldSortByStringAscThenByStringAsc()
    {
        // arrange
        var value = new ObjectValueNode(new ObjectFieldNode("foo",
            new ObjectValueNode(
                new ObjectFieldNode("baz",
                    new EnumValueNode(SortOperationKind.Asc)),
                new ObjectFieldNode("bar",
                    new EnumValueNode(SortOperationKind.Asc)))));

        var sortType = CreateType(new FooNestedSortType());

        var a = new[]
        {
            new FooNested{Foo =new Foo {Bar = "b",Baz = "b"}},
            new FooNested{Foo =new Foo {Bar = "a",Baz = "b"}},
            new FooNested{Foo =new Foo {Bar = "c",Baz = "a"}}
        }.AsQueryable();

        // act
        var context = new QueryableSortVisitorContext(
            new(new DefaultTypeConverter()), sortType, typeof(FooNested), false);
        QueryableSortVisitor.Default.Visit(value, context);
        ICollection<FooNested> aFiltered = context.Sort(a).ToList();

        // assert
        Assert.Collection(aFiltered,
            foo => Assert.Equal("c", foo.Foo.Bar),
            foo => Assert.Equal("a", foo.Foo.Bar),
            foo => Assert.Equal("b", foo.Foo.Bar)
        );
    }

    [Fact(Skip = "Disabled")]
    public void Sort_ComparableAsc_PrefilterInResolver()
    {
        // arrange
        IQueryable<Foo> data = new[] {
            new Foo { Bar = "baz", Baz = "a" },
            new Foo { Bar = "aa", Baz = "b" },
            new Foo { Bar = "zz", Baz = "b" }
        }.AsQueryable().OrderBy(x => x.Baz);

        var schema = SchemaBuilder.New()
            .AddQueryType(ctx =>
            {
                ctx.Field("foo")
                    .Resolve(data)
                    .Type<NonNullType<ListType<NonNullType<ObjectType<Foo>>>>>()
                    .UseSorting();
            })
            .Create();

        var request =
            QueryRequestBuilder.New()
                .SetQuery("{ foo(order_by: { bar: DESC }) { bar } }")
                .Create();

        // act
        var result = schema.MakeExecutable().Execute(request);

        // assert
        result.MatchSnapshot();
    }


    [Fact]
    public void Sort_ComparableAsc_ShouldSortByStringAsc()
    {
        // arrange
        var value = new ObjectValueNode(
            new ObjectFieldNode("bar",
                new EnumValueNode(SortOperationKind.Asc))
        );

        var sortType = CreateType(new FooSortType());

        var a = new[]
        {
            new Foo {Bar = "b"}, new Foo {Bar = "a"}, new Foo {Bar = "c"}
        }.AsQueryable();

        // act
        var context = new QueryableSortVisitorContext(
            new(new DefaultTypeConverter()), sortType, typeof(Foo), false);
        QueryableSortVisitor.Default.Visit(value, context);
        ICollection<Foo> aFiltered = context.Sort(a).ToList();

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
                new EnumValueNode(SortOperationKind.Desc))
        );

        var sortType = CreateType(new FooSortType());

        var a = new[]
        {
            new Foo {Bar = "b"}, new Foo {Bar = "a"}, new Foo {Bar = "c"}
        }.AsQueryable();

        // act
        var context = new QueryableSortVisitorContext(
            new(new DefaultTypeConverter()), sortType, typeof(Foo), false);
        QueryableSortVisitor.Default.Visit(value, context);
        ICollection<Foo> aFiltered = context.Sort(a).ToList();

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

        var sortType = CreateType(new FooSortType());

        var a = new[]
        {
            new Foo {Bar = "b"}, new Foo {Bar = "a"}, new Foo {Bar = "c"}
        }.AsQueryable();

        // act
        var context = new QueryableSortVisitorContext(
            new(new DefaultTypeConverter()), sortType, typeof(Foo), false);
        QueryableSortVisitor.Default.Visit(value, context);
        var aFiltered = context.Sort(a);

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
                new EnumValueNode(SortOperationKind.Asc)));

        var sortType = CreateType(new FooSortType());

        var a = new[]
        {
            new Foo {Bar = "b"},
            new Foo {Bar = "c", NullableInt = 2},
            new Foo {Bar = "a", NullableInt = 1}
        }.AsQueryable();

        // act
        var context = new QueryableSortVisitorContext(
            new(new DefaultTypeConverter()), sortType, typeof(Foo), false);
        QueryableSortVisitor.Default.Visit(value, context);
        ICollection<Foo> aFiltered = context.Sort(a).ToList();

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
                new EnumValueNode(SortOperationKind.Desc)));

        var sortType = CreateType(new FooSortType());

        var a = new[]
        {
            new Foo {Bar = "b"},
            new Foo {Bar = "c", NullableInt = 2},
            new Foo {Bar = "a", NullableInt = 1}
        }.AsQueryable();

        // act
        var context = new QueryableSortVisitorContext(
            new(new DefaultTypeConverter()), sortType, typeof(Foo), false);
        QueryableSortVisitor.Default.Visit(value, context);
        ICollection<Foo> aFiltered = context.Sort(a).ToList();

        // assert
        Assert.Collection(aFiltered,
            foo => Assert.Equal("c", foo.Bar),
            foo => Assert.Equal("a", foo.Bar),
            foo => Assert.Equal("b", foo.Bar)
        );
    }

    [Fact]
    public void Sort_Inheritance_ShouldSortByStringAsc()
    {
        // arrange
        var value = new ObjectValueNode(
            new ObjectFieldNode("bar",
                new EnumValueNode(SortOperationKind.Desc)
            )
        );

        var sortType = CreateType(new SortInputType<FooInherited>());

        var a = new[]
        {
            new FooInherited {Bar = "b"},
            new FooInherited {Bar = "a"},
            new FooInherited {Bar = "c"}
        }.AsQueryable();

        // act
        var context = new QueryableSortVisitorContext(
            new(new DefaultTypeConverter()), sortType, typeof(FooInherited), false);
        QueryableSortVisitor.Default.Visit(value, context);
        ICollection<FooInherited> aFiltered = context.Sort(a).ToList();

        // assert
        Assert.Collection(aFiltered,
            foo => Assert.Equal("c", foo.Bar),
            foo => Assert.Equal("b", foo.Bar),
            foo => Assert.Equal("a", foo.Bar)
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

    public class FooNestedSortType
        : SortInputType<FooNested>
    {
        protected override void Configure(
            ISortInputTypeDescriptor<FooNested> descriptor)
        {
            descriptor.SortableObject(t => t.Foo);
        }
    }

    public class FooNested
    {
        public Foo Foo { get; set; }
    }

    public class Foo
    {
        public int? NullableInt { get; set; }
        public string Bar { get; set; }
        public string Baz { get; set; }
    }

    public class FooInherited : Foo
    {
        public string Qux { get; set; }
    }
}