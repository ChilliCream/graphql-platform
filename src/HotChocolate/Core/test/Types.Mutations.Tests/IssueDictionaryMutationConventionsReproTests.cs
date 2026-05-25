using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class IssueDictionaryMutationConventionsReproTests
{
    [Fact]
    public async Task Mutation_Convention_Dictionary_Input_Should_Accept_Key_And_Value_When_Query_Has_Dictionary_Output()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddMutationConventions()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            mutation m {
              patchUserSettings(
                input: {
                  settings: [{ key: "open-workspace", value: "applications" }]
                }
              ) {
                keyValuePairOfStringAndString {
                  key
                  value
                }
              }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.True(operationResult.Errors is null or { Count: 0 }, operationResult.ToJson());
    }

    [Fact]
    public async Task Dictionary_String_Key_And_Value_Should_Be_NonNull_On_Input_And_Output_KeyValuePair_Types()
    {
        // arrange
        var schema = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddMutationConventions()
            .BuildSchemaAsync();

        // act
        var keyValuePairType = schema.Types.GetType<ObjectType>("KeyValuePairOfStringAndString");
        var keyValuePairInputType = schema.Types.GetType<InputObjectType>("KeyValuePairOfStringAndStringInput");

        // assert
        Assert.True(keyValuePairType.Fields["key"].Type.IsNonNullType());
        Assert.True(keyValuePairType.Fields["value"].Type.IsNonNullType());
        Assert.True(keyValuePairInputType.Fields["key"].Type.IsNonNullType());
        Assert.True(keyValuePairInputType.Fields["value"].Type.IsNonNullType());
    }

    public class Query
    {
        public async Task<Dictionary<string, string>> GetUserSettingsAsync(List<string> settingIdentifiers)
        {
            await Task.Yield();
            return [];
        }
    }

    public class Mutation
    {
        public async Task<Dictionary<string, string>?> PatchUserSettingsAsync(
            Dictionary<string, string> settings)
        {
            await Task.Yield();
            return settings;
        }
    }
}
