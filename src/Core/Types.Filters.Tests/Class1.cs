using System;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Filters
{
    public class FilterInputTypeTests
        : TypeTestBase
    {
        [Fact]
        public void TestMew()
        {
            var schema = CreateSchema(builder =>
                builder.AddType(
                    new FilterInputType<Foo>(c => c.Filter(t => t.Bar).AllowEquals().And().AllowContains())));

            schema.ToString().MatchSnapshot();

        }


        public class Foo
        {
            public string Bar { get; set; }
        }

    }
}
