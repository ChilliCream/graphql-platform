using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Regressions;

public class Issue7919ProbeTests
{
    [Fact]
    public async Task Dictionary_Derived_Object_Uses_Metadata_As_Resolver_Parent()
    {
        // arrange
        var executor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .AddType<MetadataType>()
                .AddType<MetadataEntryType>()
                .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            query {
              metadata {
                all {
                  key
                  value
                }
                key1: value(key: "key1")
                key2: value(key: "key2")
                key3: value(key: "key3")
              }
            }
            """);

        // assert
        var operationResult = result.ExpectOperationResult();
        Assert.Empty(operationResult.Errors ?? []);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "metadata": {
                  "all": [
                    {
                      "key": "key1",
                      "value": "value1"
                    },
                    {
                      "key": "key2",
                      "value": "value2"
                    }
                  ],
                  "key1": "value1",
                  "key2": "value2",
                  "key3": null
                }
              }
            }
            """);
    }

    public sealed class Query
    {
        public Metadata GetMetadata()
            => new()
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            };
    }

    public sealed class Metadata : Dictionary<string, object>;

    public sealed class MetadataType : ObjectType<Metadata>
    {
        protected override void Configure(IObjectTypeDescriptor<Metadata> descriptor)
        {
            descriptor.BindFieldsExplicitly();

            descriptor
                .Field("all")
                .Type<NonNullType<ListType<NonNullType<MetadataEntryType>>>>()
                .Resolve(
                    ctx => ctx.Parent<Metadata>()
                        .Select(e => new MetadataEntry(e.Key, e.Value.ToString()!)));

            descriptor
                .Field("value")
                .Argument("key", a => a.Type<NonNullType<StringType>>())
                .Type<StringType>()
                .Resolve(
                    ctx => ctx.Parent<Metadata>()
                        .TryGetValue(ctx.ArgumentValue<string>("key"), out var value)
                        ? value?.ToString()
                        : null);
        }
    }

    public sealed record MetadataEntry(string Key, string Value);

    public sealed class MetadataEntryType : ObjectType<MetadataEntry>
    {
        protected override void Configure(IObjectTypeDescriptor<MetadataEntry> descriptor)
        {
            descriptor.BindFieldsExplicitly();
            descriptor.Field(t => t.Key);
            descriptor.Field(t => t.Value);
        }
    }
}
