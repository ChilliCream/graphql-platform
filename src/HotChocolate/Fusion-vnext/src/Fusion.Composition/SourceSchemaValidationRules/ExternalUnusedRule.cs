using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Validators;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Fusion.WellKnownDirectiveNames;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// This rule ensures that every field marked as <c>@external</c> in a source schema is actually
/// used by that source schema in a <c>@provides</c> directive.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Unused">
/// Specification
/// </seealso>
internal sealed class ExternalUnusedRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (field.HasExternalDirective())
        {
            var providingFields =
                schema.Types
                    .OfType<MutableComplexTypeDefinition>()
                    .SelectMany(t => t.Fields.AsEnumerable())
                    .Where(f => f.HasProvidesDirective());

            var validator = new FieldInSelectionSetValidator(schema);
            var isProvided = false;

            foreach (var providingField in providingFields)
            {
                var providesDirective = providingField.Directives[Provides].First();
                var fieldsArgumentValueNode =
                    providesDirective.Arguments[WellKnownArgumentNames.Fields];

                var fieldsArgumentStringNode = (StringValueNode)fieldsArgumentValueNode;
                var selectionSet = ParseSelectionSet($"{{ {fieldsArgumentStringNode.Value} }}");
                var providingFieldType = providingField.Type.AsTypeDefinition();

                if (validator.Validate(selectionSet, providingFieldType, field, type))
                {
                    isProvided = true;
                    break;
                }
            }

            if (!isProvided)
            {
                context.Log.Write(ExternalUnused(field, type, schema));
            }
        }
    }
}
