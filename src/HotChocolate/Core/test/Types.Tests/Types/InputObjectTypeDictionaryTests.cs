using HotChocolate.Execution;

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
            """
            query {
                foo(
                    input: {
                        contextData1: [{ key: "abc", value: "abc" }]
                        contextData2: [{ key: "abc", value: "abc" }]
                        contextData3: [{ key: "abc", value: "abc" }]
                    }
                )
            }
            """);

        // assert
        result.ToJson().MatchSnapshot();
    }

    public class Query
    {
        public string GetFoo(FooInput input)
        {
            if (input.ContextData1 is { Count: 1, })
            {
                return input.ContextData1.First().Value;
            }
            return "nothing";
        }
    }

    public class FooInput
    {
        public Dictionary<string, string>? ContextData1 { get; set; }

        public IDictionary<string, string>? ContextData2 { get; set; }

        public IReadOnlyDictionary<string, string>? ContextData3 { get; set; }
    }
}
