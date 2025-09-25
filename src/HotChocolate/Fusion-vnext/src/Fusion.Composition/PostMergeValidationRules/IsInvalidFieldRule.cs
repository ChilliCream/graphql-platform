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
/// Even if the field selection map for <c>@is(field: "…")</c> is syntactically valid, its contents
/// must also be valid within the composed schema. Fields must exist on the parent type for them to
/// be referenced by <c>@is</c>. In addition, fields referencing unknown fields break the valid
/// usage of <c>@is</c>, leading to an <c>IS_INVALID_FIELD</c> error.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Is-Invalid-Field">
/// Specification
/// </seealso>
internal sealed class IsInvalidFieldRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;

        var sourceArgumentGroup = context.SchemaDefinitions
            .SelectMany(s => s.Types.OfType<MutableObjectTypeDefinition>(), (s, o) => (s, o))
            .SelectMany(x => x.o.Fields.AsEnumerable(), (x, f) => (x.s, x.o, f))
            .SelectMany(
                x => x.f.Arguments.AsEnumerable().Where(a => a.HasIsDirective()),
                (x, a) => new FieldArgumentInfo(a, x.f, x.o, x.s));

        var validator = new FieldSelectionMapValidator(schema);

        foreach (var (sourceArgument, sourceField, sourceType, sourceSchema) in sourceArgumentGroup)
        {
            var isDirective = sourceArgument.Directives[Is].First();
            var fieldArgumentValue = (string)isDirective.Arguments[Field].Value!;
            var fieldSelectionMapParser = new FieldSelectionMapParser(fieldArgumentValue);
            var fieldSelectionMap = fieldSelectionMapParser.Parse();
            var inputTypeNode = sourceArgument.Type.ToTypeNode();
            var inputTypeDefinition = schema.Types[sourceArgument.Type.AsTypeDefinition().Name];
            var inputType = inputTypeNode.RewriteToType(inputTypeDefinition);
            var outputTypeNode = sourceField.Type.ToTypeNode();
            var outputTypeDefinition = schema.Types[sourceField.Type.AsTypeDefinition().Name];
            var outputType = outputTypeNode.RewriteToType(outputTypeDefinition);

            var errors = validator.Validate(fieldSelectionMap, inputType, outputType);

            if (errors.Any())
            {
                context.Log.Write(
                    IsInvalidField(
                        isDirective,
                        sourceArgument.Name,
                        sourceField.Name,
                        sourceType.Name,
                        sourceSchema,
                        errors));
            }
        }
    }
}
