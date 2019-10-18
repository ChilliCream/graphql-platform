using System;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorComparableTests
        : TypeTestBase
    {

        [Fact]
        public void Create_ShortEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort",
                    new IntValueNode(12)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 12 };
            Assert.True(func(a));

            var b = new Foo { BarShort = 13 };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ShortNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not",
                    new IntValueNode(12)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 13 };
            Assert.True(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.False(func(b));
        }


        [Fact]
        public void Create_ShortGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gt",
                    new IntValueNode(12)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 11 };
            Assert.False(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.False(func(b));

            var c = new Foo { BarShort = 13 };
            Assert.True(func(c));
        }

        [Fact]
        public void Create_ShortNotGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gt",
                    new IntValueNode(12)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 11 };
            Assert.True(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.True(func(b));

            var c = new Foo { BarShort = 13 };
            Assert.False(func(c));
        }


        [Fact]
        public void Create_ShortGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gte",
                    new IntValueNode(12)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 11 };
            Assert.False(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.True(func(b));

            var c = new Foo { BarShort = 13 };
            Assert.True(func(c));
        }

        [Fact]
        public void Create_ShortNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gte",
                    new IntValueNode(12)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 11 };
            Assert.True(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.False(func(b));

            var c = new Foo { BarShort = 13 };
            Assert.False(func(c));
        }



        [Fact]
        public void Create_ShortLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lt",
                    new IntValueNode(12)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 11 };
            Assert.True(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.False(func(b));

            var c = new Foo { BarShort = 13 };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_ShortNotLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lt",
                    new IntValueNode(12)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 11 };
            Assert.False(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.True(func(b));

            var c = new Foo { BarShort = 13 };
            Assert.True(func(c));
        }


        [Fact]
        public void Create_ShortLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lte",
                    new IntValueNode(12)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 11 };
            Assert.True(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.True(func(b));

            var c = new Foo { BarShort = 13 };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_ShortNotLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lte",
                    new IntValueNode(12)));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 11 };
            Assert.False(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.False(func(b));

            var c = new Foo { BarShort = 13 };
            Assert.True(func(c));
        }

        [Fact]
        public void Create_ShortIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_in",
                new ListValueNode(new[]
                {
                    new IntValueNode(13),
                    new IntValueNode(14)
                }))
            );

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 13 };
            Assert.True(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ShortNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_in",
                new ListValueNode(new[] { new IntValueNode(13), new IntValueNode(14) }
                ))
            );

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(fooType, typeof(Foo), TypeConversion.Default);
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 12 };
            Assert.True(func(a));

            var b = new Foo { BarShort = 13 };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_NullableShortEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort",
                    new IntValueNode(12)));

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 12 };
            Assert.True(func(a));

            var b = new FooNullable { BarShort = 13 };
            Assert.False(func(b));

            var c = new FooNullable { BarShort = null };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_NullableShortNotEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not",
                    new IntValueNode(12)));

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 13 };
            Assert.True(func(a));

            var b = new FooNullable { BarShort = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarShort = null };
            Assert.True(func(c));
        }


        [Fact]
        public void Create_NullableShortGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gt",
                    new IntValueNode(12)));

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 11 };
            Assert.False(func(a));

            var b = new FooNullable { BarShort = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarShort = 13 };
            Assert.True(func(c));

            var d = new FooNullable { BarShort = null };
            Assert.False(func(d));
        }

        [Fact]
        public void Create_NullableShortNotGreaterThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gt",
                    new IntValueNode(12)));

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 11 };
            Assert.True(func(a));

            var b = new FooNullable { BarShort = 12 };
            Assert.True(func(b));

            var c = new FooNullable { BarShort = 13 };
            Assert.False(func(c));

            var d = new FooNullable { BarShort = null };
            Assert.True(func(d));
        }


        [Fact]
        public void Create_NullableShortGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_gte",
                    new IntValueNode(12)));

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 11 };
            Assert.False(func(a));

            var b = new FooNullable { BarShort = 12 };
            Assert.True(func(b));

            var c = new FooNullable { BarShort = 13 };
            Assert.True(func(c));

            var d = new FooNullable { BarShort = null };
            Assert.False(func(d));
        }

        [Fact]
        public void Create_NullableShortNotGreaterThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_gte",
                    new IntValueNode(12)));

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 11 };
            Assert.True(func(a));

            var b = new FooNullable { BarShort = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarShort = 13 };
            Assert.False(func(c));

            var d = new FooNullable { BarShort = null };
            Assert.True(func(d));
        }



        [Fact]
        public void Create_NullableShortLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lt",
                    new IntValueNode(12)));

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 11 };
            Assert.True(func(a));

            var b = new FooNullable { BarShort = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarShort = 13 };
            Assert.False(func(c));

            var d = new FooNullable { BarShort = null };
            Assert.False(func(d));
        }

        [Fact]
        public void Create_NullableShortNotLowerThan_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lt",
                    new IntValueNode(12)));

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 11 };
            Assert.False(func(a));

            var b = new FooNullable { BarShort = 12 };
            Assert.True(func(b));

            var c = new FooNullable { BarShort = 13 };
            Assert.True(func(c));

            var d = new FooNullable { BarShort = null };
            Assert.True(func(d));
        }


        [Fact]
        public void Create_NullableShortLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_lte",
                    new IntValueNode(12)));

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 11 };
            Assert.True(func(a));

            var b = new FooNullable { BarShort = 12 };
            Assert.True(func(b));

            var c = new FooNullable { BarShort = 13 };
            Assert.False(func(c));

            var d = new FooNullable { BarShort = null };
            Assert.False(func(d));
        }

        [Fact]
        public void Create_NullableShortNotLowerThanOrEquals_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_lte",
                    new IntValueNode(12)));

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 11 };
            Assert.False(func(a));

            var b = new FooNullable { BarShort = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarShort = 13 };
            Assert.True(func(c));

            var d = new FooNullable { BarShort = null };
            Assert.True(func(d));
        }

        [Fact]
        public void Create_NullableShortIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_in",
                new ListValueNode(new[]
                {
                    new IntValueNode(13),
                    new IntValueNode(14)
                }))
            );

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(
                fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 13 };
            Assert.True(func(a));

            var b = new FooNullable { BarShort = 12 };
            Assert.False(func(b));

            var c = new FooNullable { BarShort = null };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_NullableShortNotIn_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort_not_in",
                new ListValueNode(new[] { new IntValueNode(13), new IntValueNode(14) }
                ))
            );

            var fooNullableType = CreateType(new FooNullableFilterType());

            // act
            var filter = new QueryableFilterVisitor(fooNullableType, typeof(FooNullable), TypeConversion.Default);
            value.Accept(filter);
            Func<FooNullable, bool> func = filter.CreateFilter<FooNullable>().Compile();

            // assert
            var a = new FooNullable { BarShort = 12 };
            Assert.True(func(a));

            var b = new FooNullable { BarShort = 13 };
            Assert.False(func(b));

            var c = new FooNullable { BarShort = null };
            Assert.True(func(c));
        }

        public class Foo
        {
            public short BarShort { get; set; }
            public int BarInt { get; set; }
            public long BarLong { get; set; }
            public float BarFloat { get; set; }
            public double BarDouble { get; set; }
            public decimal BarDecimal { get; set; }
        }

        public class FooNullable
        {
            public short? BarShort { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(x => x.BarShort);
                descriptor.Filter(x => x.BarInt);
                descriptor.Filter(x => x.BarLong);
                descriptor.Filter(x => x.BarFloat);
                descriptor.Filter(x => x.BarDouble);
                descriptor.Filter(x => x.BarDecimal);
            }
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooNullable> descriptor)
            {
                descriptor.Filter(x => x.BarShort);
            }
        }
    }
}
