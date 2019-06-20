using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class QueryableFilterVisitorComparableTests
        : TypeTestBase
    {
        [Fact]
        public void Create_FloatEqual_Expression()
        {
            // arrange
            var value = new ObjectValueNode(
                new ObjectFieldNode("barShort",
                    new FloatValueNode("12")));

            var fooType = CreateType(new FooFilterType());

            // act
            var filter = new QueryableFilterVisitor(fooType, typeof(Foo));
            value.Accept(filter);
            Func<Foo, bool> func = filter.CreateFilter<Foo>().Compile();

            // assert
            var a = new Foo { BarShort = 12 };
            Assert.True(func(a));

            var b = new Foo { BarShort = 12 };
            Assert.False(func(b));
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
    }
}
