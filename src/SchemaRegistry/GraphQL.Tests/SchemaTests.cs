using HotChocolate;
using MarshmallowPie.GraphQL;
using Snapshooter.Xunit;
using Xunit;

namespace GraphQL.Tests
{
    public class SchemaTests
    {
        [Fact]
        public void Schema_Changed()
        {
            SchemaBuilder.New()
                .AddSchemaRegistry()
                .Create()
                .ToString()
                .MatchSnapshot();
        }
    }
}
