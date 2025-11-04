using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Composite;

public static class SerializeAsTests
{
    [Fact]
    public static async Task SerializeAs_Is_Not_Added()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    [Fact]
    public static async Task SerializeAs_Is_Added()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<Query>()
                .ModifyOptions(o => o.ApplySerializeAsToScalars = true)
                .BuildSchemaAsync();

        schema.MatchSnapshot();
    }

    public class Query
    {
        [GraphQLType<NonNullType<CustomString>>]
        public string GetFoo() => "foo";
    }

    [SerializeAs(ScalarSerializationType.String)]
    public class CustomString : StringType
    {
        public CustomString() : base("Custom")
        {
        }
    }
}
