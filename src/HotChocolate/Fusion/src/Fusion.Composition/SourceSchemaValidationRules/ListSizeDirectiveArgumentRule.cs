using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Reports negative integer values for the non-negative arguments on compatible
/// <c>@listSize</c> directives.
/// </summary>
internal sealed class ListSizeDirectiveArgumentRule : IEventHandler<OutputFieldEvent>
{
    private static readonly DirectiveDefinitionNode s_canonicalDefinition =
        new ListSizeMutableDirectiveDefinition(
            BuiltIns.Int.Create(), BuiltIns.String.Create(), BuiltIns.Boolean.Create()).ToSyntaxNode();

    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, _, schema) = @event;
        var directive = field.Directives.FirstOrDefault(DirectiveNames.ListSize.Name);

        if (directive is null)
        {
            return;
        }

        if (schema.DirectiveDefinitions.TryGetDirective(DirectiveNames.ListSize.Name, out var definition)
            && !DirectiveDefinitionCompatibility.IsSourceCompatibleWithCanonical(
                definition.ToSyntaxNode(), s_canonicalDefinition, allowArgumentSubset: true))
        {
            return;
        }

        ValidateArgument(DirectiveNames.ListSize.Arguments.AssumedSize);
        ValidateArgument(DirectiveNames.ListSize.Arguments.SlicingArgumentDefaultValue);

        void ValidateArgument(string argumentName)
        {
            if (directive.Arguments.TryGetValue(argumentName, out var value)
                && value is IntValueNode intValue
                && intValue.ToDouble() < 0.0)
            {
                context.Log.Write(InvalidListSizeArgument(argumentName, value, field, schema));
            }
        }
    }
}
