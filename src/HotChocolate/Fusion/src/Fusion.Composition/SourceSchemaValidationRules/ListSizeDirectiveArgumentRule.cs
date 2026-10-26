using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Reports invalid argument shapes and negative integer values on compatible <c>@listSize</c> directives.
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

        ValidateNonNegativeIntArgument(DirectiveNames.ListSize.Arguments.AssumedSize);
        ValidateStringListArgument(DirectiveNames.ListSize.Arguments.SlicingArguments);
        ValidateStringListArgument(DirectiveNames.ListSize.Arguments.SizedFields);
        ValidateBooleanArgument(DirectiveNames.ListSize.Arguments.RequireOneSlicingArgument);
        ValidateNonNegativeIntArgument(DirectiveNames.ListSize.Arguments.SlicingArgumentDefaultValue);

        void ValidateNonNegativeIntArgument(string argumentName)
        {
            if (!directive.Arguments.TryGetValue(argumentName, out var value) || value is NullValueNode)
            {
                return;
            }

            if (value is not IntValueNode intValue)
            {
                context.Log.Write(InvalidListSizeArgumentType(argumentName, value, field, schema));
                return;
            }

            if (intValue.ToDouble() < 0.0)
            {
                context.Log.Write(InvalidListSizeArgumentNegativeValue(argumentName, value, field, schema));
            }
        }

        void ValidateBooleanArgument(string argumentName)
        {
            if (directive.Arguments.TryGetValue(argumentName, out var value)
                && value is not NullValueNode
                && value is not BooleanValueNode)
            {
                context.Log.Write(InvalidListSizeArgumentType(argumentName, value, field, schema));
            }
        }

        // GraphQL permits a single string wherever a list of strings is expected.
        void ValidateStringListArgument(string argumentName)
        {
            if (!directive.Arguments.TryGetValue(argumentName, out var value)
                || value is NullValueNode
                || value is StringValueNode
                || (value is ListValueNode listValue && listValue.Items.All(item => item is StringValueNode)))
            {
                return;
            }

            context.Log.Write(InvalidListSizeArgumentType(argumentName, value, field, schema));
        }
    }
}
