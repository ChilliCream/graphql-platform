using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class ComparableOperationInputTests
    {
        [Fact]
        public void CreateIntOperationType()
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
                        .Argument("test", a => a.Type<ComparableOperationInput<int>>()))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }
    }
}