using System;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class ComparableFilterInputTypeTests
        : TypeTestBase
    {
        [Fact]
        public void Create_Implicit_Filters()
        {
            // arrange
            // act
            var schema = CreateSchema(new FooFilterType());

            // assert
            schema.ToString().MatchSnapshot();
        }

        /// <summary>
        /// This test checks if the binding of all allow methods are correct
        /// </summary>
        [Fact]
        public void Create_Explitcit_Filters()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>(descriptor =>
            {
                descriptor.Filter(x => x.BarShort).BindExplicitly().AllowEquals()
                .And().AllowNotEquals()
                .And().AllowIn()
                .And().AllowNotIn()
                .And().AllowGreaterThan()
                .And().AllowNotGreaterThan()
                .And().AllowGreaterThanOrEquals()
                .And().AllowNotGreaterThanOrEquals()
                .And().AllowLowerThan()
                .And().AllowNotLowerThan()
                .And().AllowLowerThanOrEquals()
                .And().AllowNotLowerThanOrEquals();

            }));

            // assert
            schema.ToString().MatchSnapshot();
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
