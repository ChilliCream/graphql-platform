using HotChocolate;
using Snapshooter.Xunit;
using Xunit;

namespace MarshmallowPie.GraphQL
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
