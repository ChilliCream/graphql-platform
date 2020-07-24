using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class StringOperationInputTests
    {
        [Fact]
        public void CreateStringOperationInput()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(
                    t => t
                        .Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("foo")
                        .Argument("test", a => a.Type<StringOperationInput>()))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }
    }
}