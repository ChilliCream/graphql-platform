using System;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeTests
        : TypeTestBase
    {
        [Fact]
        public void CreateImplictFilters()
        {
            // arrange
            // act
            var schema = CreateSchema(new FilterInputType<Foo>());

            // assert
            schema.ToString().MatchSnapshot();
        }


        public class Foo
        {
            public string Bar { get; set; }
        }

    }
}
