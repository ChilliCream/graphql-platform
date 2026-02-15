using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Metadata;

namespace HotChocolate.Fusion.Planning;

public class OperationPlannerTopologyCacheTests : FusionTestBase
{
    [Fact]
    public void TransitionLookup_Fallback_Without_TopologyCache_Is_Equivalent()
    {
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String!
              region: String!
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              price(region: String! @require(field: "region")): Float!
            }
            """);

        var productType = (FusionComplexTypeDefinition)schema.Types["Product"];
        var fromA = ImmutableHashSet.Create("a");
        var fromB = ImmutableHashSet.Create("b");

        var directWithCache = TryGetLookupSignature(schema, productType, fromA, "b");
        var impossibleWithCache = TryGetLookupSignature(schema, productType, fromB, "a");

        DisableTopologyCache(schema);

        var directWithoutCache = TryGetLookupSignature(schema, productType, fromA, "b");
        var impossibleWithoutCache = TryGetLookupSignature(schema, productType, fromB, "a");

        Assert.Equal(directWithCache, directWithoutCache);
        Assert.Equal(impossibleWithCache, impossibleWithoutCache);
    }

    [Fact]
    public void PlannerPlan_Fallback_Without_TopologyCache_Is_Equivalent()
    {
        var schema = ComposeSchema(
            """
            schema {
              query: Query
            }

            type Query {
              topProducts: [Product!]
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String!
              region: String!
            }
            """,
            """
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              price(region: String! @require(field: "region")): Float!
            }
            """);

        const string operation =
            """
            {
              topProducts {
                id
                name
                price
              }
            }
            """;

        var formatter = new YamlOperationPlanFormatter();
        var withCache = formatter.Format(PlanOperation(schema, operation));

        DisableTopologyCache(schema);

        var withoutCache = formatter.Format(PlanOperation(schema, operation));

        Assert.Equal(withCache, withoutCache);
    }

    private static (bool Found, string? Signature) TryGetLookupSignature(
        FusionSchemaDefinition schema,
        FusionComplexTypeDefinition type,
        ImmutableHashSet<string> fromSchemas,
        string toSchema)
    {
        if (schema.TryGetBestDirectLookup(type, fromSchemas, toSchema, out var lookup))
        {
            return (true, CreateLookupSignature(lookup));
        }

        return (false, null);
    }

    private static void DisableTopologyCache(FusionSchemaDefinition schema)
    {
        var cacheField = typeof(FusionSchemaDefinition)
            .GetField("_plannerTopologyCache", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Could not find topology cache field.");
        cacheField.SetValue(schema, null);

        var directLookupField = typeof(FusionSchemaDefinition)
            .GetField("_bestDirectLookup", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Could not find direct lookup cache field.");
        var clear = directLookupField.FieldType.GetMethod(nameof(ConcurrentDictionary<string, string>.Clear))
            ?? throw new InvalidOperationException("Could not clear direct lookup cache field.");
        clear.Invoke(directLookupField.GetValue(schema), []);
    }

    private static string CreateLookupSignature(Lookup lookup)
    {
        var path = lookup.Path.Length == 0
            ? string.Empty
            : string.Join('.', lookup.Path);

        return string.Concat(
            lookup.SchemaName,
            ":",
            lookup.FieldName,
            ":",
            path,
            ":",
            lookup.Arguments.Length.ToString(),
            ":",
            lookup.Fields.Length.ToString());
    }
}
