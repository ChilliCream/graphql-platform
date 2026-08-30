using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Language.Utilities;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// A source schema that declares a directive definition named <c>policy</c> must declare it with
/// the exact shape composition expects: the same name and repeatability, the same arguments, and
/// locations that are a subset of the canonical locations. Composition merges every source
/// schema's <c>@policy</c> applications under a single directive definition, so a source schema
/// declaring an incompatible shape would otherwise cause every <c>@policy</c> application in
/// every source schema to be silently dropped from the composed schema. Declaring the directive
/// implies intent to use it, so this rule fails composition even when the declaring schema has no
/// <c>@policy</c> applications of its own.
/// </summary>
internal sealed class PolicyDefinitionInvalidRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;

        if (!schema.DirectiveDefinitions.TryGetDirective(
            WellKnownDirectiveNames.Policy,
            out var directiveDefinition))
        {
            return;
        }

        var canonicalNode = PolicyMutableDirectiveDefinition.Create(schema).ToSyntaxNode();

        if (!DirectiveDefinitionCompatibility.IsSourceCompatibleWithCanonical(
            directiveDefinition.ToSyntaxNode(),
            canonicalNode))
        {
            context.Log.Write(PolicyDefinitionInvalid(schema, canonicalNode.Print(indented: false)));
        }
    }
}
