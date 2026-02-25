using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class Issue6197ReproTests
{
    [Fact]
    public async Task AnyType_Should_Use_Custom_Type_Converter_For_Reference_Type()
    {
        // arrange
        // act
        var result = await new ServiceCollection()
            .AddGraphQLServer()
            .AddJsonTypeConverter()
            .AddTypeConverter<TimeZoneInfo, string>(value => value.Id)
            .AddQueryType<Query>()
            .ExecuteRequestAsync(
            """
            {
              value
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "value": "UTC"
              }
            }
            """);
    }

    public class Query
    {
        [GraphQLType<AnyType>]
        public object Value => TimeZoneInfo.Utc;
    }
}
