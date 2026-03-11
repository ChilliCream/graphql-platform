using HotChocolate.Execution;

namespace HotChocolate.Types;

public class InputObjectTypeDictionaryTests
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

    [Fact]
    public void Dictionary_Input_With_Nullable_Value_Is_Correctly_Detected()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<NullableDictionaryInputQuery>()
            .Create();

        // assert
        var fooInputType = schema.Types.GetType<InputObjectType>("NullableDictionaryInput");
        var keyValuePairType = schema.Types.GetType<InputObjectType>(
            fooInputType.Fields["contextData"].Type.TypeName());

        keyValuePairType
            .ToString()
            .MatchInlineSnapshot(
                """
                input KeyValuePairOfStringAndNullableStringInput {
                  key: String!
                  value: String
                }
                """);
    }

    [Fact]
    public void Dictionary_Output_With_Nullable_Value_Is_Correctly_Detected()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<NullableDictionaryOutputQuery>()
            .Create();

        // assert
        var queryType = schema.Types.GetType<ObjectType>("NullableDictionaryOutputQuery");
        var keyValuePairType = schema.Types.GetType<ObjectType>(queryType.Fields["contextData"].Type.TypeName());

        keyValuePairType
            .ToString()
            .MatchInlineSnapshot(
                """
                type KeyValuePairOfStringAndNullableString {
                  key: String!
                  value: String
                }
                """);
    }

    [Fact]
    public void Dictionary_Output_With_Nullable_Reference_Value_Uses_Nullable_Name_Prefix()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<NullableDictionaryOutputQuery>()
            .Create();

        // assert
        var queryType = schema.Types.GetType<ObjectType>("NullableDictionaryOutputQuery");
        var keyValuePairType = queryType.Fields["contextData"].Type.NamedType();

        keyValuePairType
            .ToString()
            .MatchInlineSnapshot(
                """
                type KeyValuePairOfStringAndNullableString {
                  key: String!
                  value: String
                }
                """);
    }

    [Fact]
    public void Dictionary_Input_With_Nullable_Reference_Value_Is_Disambiguated_From_NonNull_Reference_Value()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<ReferenceNullabilityDisambiguationInputQuery>()
            .Create();

        // assert
        var nonNullInputType = schema.Types.GetType<InputObjectType>("NonNullReferenceDictionaryInput");
        var nullableInputType = schema.Types.GetType<InputObjectType>("NullableReferenceDictionaryInput");
        var nonNullType = nonNullInputType.Fields["contextData"].Type.NamedType();
        var nullableType = nullableInputType.Fields["contextData"].Type.NamedType();

        Snapshot.Create()
            .Add(nonNullType.ToString(), "nonNull")
            .Add(nullableType.ToString(), "nullable")
            .MatchInline(
                """
                nonNull
                ---------------
                input KeyValuePairOfStringAndStringInput {
                  key: String!
                  value: String!
                }
                ---------------

                nullable
                ---------------
                input KeyValuePairOfStringAndNullableStringInput {
                  key: String!
                  value: String
                }
                ---------------

                """);
    }

    [Fact]
    public void Dictionary_Output_With_Nullable_ValueType_Value_Uses_Nullable_Name_Prefix()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<NullableValueTypeDictionaryOutputQuery>()
            .Create();

        // assert
        var queryType = schema.Types.GetType<ObjectType>("NullableValueTypeDictionaryOutputQuery");
        var keyValuePairType = schema.Types.GetType<ObjectType>(queryType.Fields["contextData"].Type.TypeName());

        keyValuePairType
            .ToString()
            .MatchInlineSnapshot(
                """
                type KeyValuePairOfStringAndNullableInt32 {
                  key: String!
                  value: Int
                }
                """);
    }

    [Fact]
    public void Dictionary_Output_With_IList_Value_Uses_Valid_KeyValuePair_Name()
    {
        // arrange
        // act
        var schema = SchemaBuilder.New()
            .AddQueryType<DictionaryWithListOutputQuery>()
            .Create();

        // assert
        var outputType = schema.Types.GetType<ObjectType>("DictionaryWithListOutput");
        var keyValuePairType = schema.Types.GetType<ObjectType>(outputType.Fields["highlights"].Type.TypeName());

        keyValuePairType
            .ToString()
            .MatchInlineSnapshot(
                """
                type KeyValuePairOfStringAndIListOfString {
                  key: String!
                  value: [String!]!
                }
                """);
    }

    public class Query
    {
        public string GetFoo(FooInput input)
        {
            if (input.ContextData1 is { Count: 1 })
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

    [Fact]
    public void Explicit_ObjectType_For_KeyValuePair_Overrides_Inferred_Type()
    {
        // arrange & act
        var schema = SchemaBuilder.New()
            .AddQueryType<CustomKvpOutputQuery>()
            .AddType<CustomKeyValuePairType>()
            .Create();

        // assert — the custom type name and field names must appear in the schema,
        // proving the explicit ObjectType<T> definition is used instead of the
        // auto-inferred KeyValuePairOfStringAndString.
        schema.MatchSnapshot();
    }

    public class NullableDictionaryInput
    {
        public Dictionary<string, string?>? ContextData { get; set; }
    }

    public class NullableDictionaryInputQuery
    {
        public string GetFoo(NullableDictionaryInput input) => "ok";
    }

    public class NullableDictionaryOutputQuery
    {
        public Dictionary<string, string?>? GetContextData() => null;
    }

    public class NullableValueTypeDictionaryOutputQuery
    {
        public Dictionary<string, int?>? GetContextData() => null;
    }

    public class ReferenceNullabilityDisambiguationInputQuery
    {
        public string GetFoo(NonNullReferenceDictionaryInput input) => "ok";

        public string GetBar(NullableReferenceDictionaryInput input) => "ok";
    }

    public class NonNullReferenceDictionaryInput
    {
        public Dictionary<string, string>? ContextData { get; set; }
    }

    public class NullableReferenceDictionaryInput
    {
        public Dictionary<string, string?>? ContextData { get; set; }
    }

    public class DictionaryWithListOutputQuery
    {
        public DictionaryWithListOutput GetResult() => new();
    }

    public class DictionaryWithListOutput
    {
        public IDictionary<string, IList<string>> Highlights { get; internal set; } =
            new Dictionary<string, IList<string>>();
    }

    public class CustomKeyValuePairType : ObjectType<KeyValuePair<string, string>>
    {
        protected override void Configure(IObjectTypeDescriptor<KeyValuePair<string, string>> descriptor)
        {
            descriptor.Name("StringPair");
            descriptor.Field(x => x.Key).Name("first");
            descriptor.Field(x => x.Value).Name("second");
        }
    }

    public class CustomKvpOutputQuery
    {
        public Dictionary<string, string>? GetItems() => null;
    }
}
