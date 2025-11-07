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
        [GraphQLType<NonNullType<CustomString1>>]
        public string GetFoo() => "foo";

        [GraphQLType<NonNullType<CustomString2>>]
        public string GetBar() => "foo";

        [GraphQLType<NonNullType<CustomString3>>]
        public string GetBaz() => "foo";
    }

    public class CustomString1 : StringType
    {
        public CustomString1() : base("Custom1")
        {
            SerializationType = ScalarSerializationType.String;
        }
    }

    public class CustomString2 : StringType
    {
        public CustomString2() : base("Custom2")
        {
            SerializationType = ScalarSerializationType.Any;
        }
    }

    public class CustomString3 : StringType
    {
        public CustomString3() : base("Custom3")
        {
            SerializationType = ScalarSerializationType.String;
            Pattern = "\\b\\d{3}\\b";
        }
    }
}
