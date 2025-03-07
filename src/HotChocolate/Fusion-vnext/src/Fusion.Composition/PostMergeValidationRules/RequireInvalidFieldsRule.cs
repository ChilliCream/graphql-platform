using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Validators;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// Even if the selection map for <c>@require(field: "…")</c> is syntactically valid, its contents
/// must also be valid within the composed schema. Fields must exist on the parent type for them to
/// be referenced by <c>@require</c>. In addition, fields requiring unknown fields break the valid
/// usage of <c>@require</c>, leading to a <c>REQUIRE_INVALID_FIELDS</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Require-Invalid-Fields">
/// Specification
/// </seealso>
internal sealed class RequireInvalidFieldsRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (field.HasFusionInaccessibleDirective()
            || type.HasFusionInaccessibleDirective()
            || !field.HasFusionRequiresDirective())
        {
            return;
        }

        var validator = new FieldSelectionMapValidator(schema);
        var fusionRequiresDirectives = field.Directives[FusionRequires];

        foreach (var fusionRequiresDirective in fusionRequiresDirectives)
        {
            var sourceSchemaName = (string)fusionRequiresDirective.Arguments[Schema].Value!;
            var sourceSchema = context.SchemaDefinitionsByName[sourceSchemaName];

            if (!(sourceSchema.Types.TryGetType(type.Name, out var sourceType)
                && sourceType is MutableComplexTypeDefinition sourceComplexType))
            {
                return;
            }

            var arguments = sourceComplexType.Fields[field.Name].Arguments;
            var map = (ListValueNode)fusionRequiresDirective.Arguments[Map];

            for (var i = 0; i < arguments.Count; i++)
            {
                var selectionMap = (string?)map.Items[i].Value;

                if (selectionMap is null)
                {
                    continue;
                }

                var fieldSelectionMapParser = new FieldSelectionMapParser(selectionMap);
                var fieldSelectionMap = fieldSelectionMapParser.Parse();
                var argument = arguments.AsEnumerable().ElementAt(i);
                var inputType = schema.Types[argument.Type.AsTypeDefinition().Name];
                var errors = validator.Validate(fieldSelectionMap, inputType, type);

                if (errors.Any())
                {
                    context.Log.Write(
                        RequireInvalidFields(
                            fusionRequiresDirective,
                            argument.Name,
                            field.Name,
                            type.Name,
                            sourceSchemaName,
                            schema,
                            errors));
                }
            }
        }
    }
}
