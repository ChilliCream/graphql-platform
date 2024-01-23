using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types;

public class InputObjectTypeNonNullTests
    : TypeTestBase
{
    [Fact]
    public void Nullable_Dictionary_Is_Correctly_Detected()
    {
        SchemaBuilder.New()
            .AddQueryType<Query>()
            .Create()
            .ToString()
            .MatchSnapshot();
    }

    [Fact]
    public async Task Dictionary_Is_Correctly_Deserialized()
    {
        // arrange
        var executor = SchemaBuilder.New()
            .AddQueryType<Query>()
            .Create()
            .MakeExecutable();

        // act
        var result = await executor.ExecuteAsync(
            "query { foo(input: { contextData: [ { key: \"abc\" value: \"abc\" } ] }) }");

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class Query
    {
        public string GetFoo(FooInput input)
        {
            if (input.ContextData is { Count: 1, })
            {
                return input.ContextData.First().Value;
            }
            return "nothing";
        }
    }

    public class FooInput
    {
        public Dictionary<string, string>? ContextData { get; set; }
    }
}
