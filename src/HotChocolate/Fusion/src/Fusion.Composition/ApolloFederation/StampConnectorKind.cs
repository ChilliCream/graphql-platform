using HotChocolate.Fusion.Definitions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Stamps the <c>@fusion__connector(kind: "Apollo")</c> directive onto the source schema
/// so the composer can lift the kind onto the corresponding <c>fusion__Schema</c> enum value
/// in the merged execution schema.
/// </summary>
internal static class StampConnectorKind
{
    private const string ApolloKind = "Apollo";

    /// <summary>
    /// Applies the connector kind stamp to the schema. Idempotent: the directive
    /// definition is registered only when missing, and a duplicate
    /// <c>@fusion__connector(kind: "Apollo")</c> instance is not added when one
    /// is already present.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to stamp in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        if (!schema.DirectiveDefinitions.TryGetDirective(
            WellKnownDirectiveNames.FusionConnector,
            out var connectorDefinition))
        {
            connectorDefinition = new ConnectorMutableDirectiveDefinition(BuiltIns.String.Create());
            schema.DirectiveDefinitions.Add(connectorDefinition);
        }

        foreach (var existing in schema.Directives[WellKnownDirectiveNames.FusionConnector])
        {
            if (existing.Arguments.TryGetValue(WellKnownArgumentNames.Kind, out var kindValue)
                && kindValue is StringValueNode { Value: ApolloKind })
            {
                return;
            }
        }

        schema.Directives.Add(
            new Directive(
                connectorDefinition,
                new ArgumentAssignment(WellKnownArgumentNames.Kind, ApolloKind)));
    }
}
