using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Validators;
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
internal sealed class RequireInvalidFieldsRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;

        var sourceArgumentGroup = context.SchemaDefinitions
            .SelectMany(s => s.Types.OfType<MutableObjectTypeDefinition>(), (s, o) => (s, o))
            .SelectMany(x => x.o.Fields.AsEnumerable(), (x, f) => (x.s, x.o, f))
            .SelectMany(
                x => x.f.Arguments.AsEnumerable().Where(a => a.HasRequireDirective()),
                (x, a) => new FieldArgumentInfo(a, x.f, x.o, x.s));

        var validator = new FieldSelectionMapValidator(schema);

        foreach (var (sourceArgument, sourceField, sourceType, sourceSchema) in sourceArgumentGroup)
        {
            var requireDirective = sourceArgument.Directives[Require].First();
            var fieldArgumentValue = (string)requireDirective.Arguments[Field].Value!;
            var fieldSelectionMapParser = new FieldSelectionMapParser(fieldArgumentValue);
            var fieldSelectionMap = fieldSelectionMapParser.Parse();
            var inputType = schema.Types[sourceArgument.Type.AsTypeDefinition().Name];
            var outputType = schema.Types[sourceType.Name];
            var errors =
                validator.Validate(
                    fieldSelectionMap,
                    inputType,
                    outputType,
                    out var selectedFields);

            // A selected field is defined in the same schema as the `require` directive.
            var selectedFieldSameSchema =
                selectedFields.Any(f => f.GetSchemaNames().Contains(sourceSchema.Name));

            if (errors.Any() || selectedFieldSameSchema)
            {
                context.Log.Write(
                    RequireInvalidFields(
                        requireDirective,
                        sourceArgument.Name,
                        sourceField.Name,
                        sourceType.Name,
                        sourceSchema,
                        errors));
            }
        }
    }
}
