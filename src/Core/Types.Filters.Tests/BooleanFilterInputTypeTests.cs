using System;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class BooleanFilterInputTypeTests
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
                descriptor.Filter(x => x.Bar).BindExplicitly().AllowEquals()
                .And().AllowNotEquals();

            }));

            // assert
            schema.ToString().MatchSnapshot();
        }


        public class Foo
        {
            public bool Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Filter(x => x.Bar);

            }
        }
    }
}
