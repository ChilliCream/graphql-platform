using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Validators;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Language.Utf8GraphQLParser.Syntax;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// Even if the selection set for <c>@key(fields: "…")</c> is syntactically valid, field references
/// within that selection set must also refer to <b>actual</b> fields on the annotated type. This
/// includes nested selections, which must appear on the corresponding return type. If any
/// referenced field is missing or incorrectly named, composition fails with a
/// <c>KEY_INVALID_FIELDS</c> error because the entity key cannot be resolved correctly.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Key-Invalid-Fields">
/// Specification
/// </seealso>
internal sealed class KeyInvalidFieldsRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;

        var sourceKeyDirectives = context.SchemaDefinitions
            .SelectMany(s => s.Types.OfType<MutableComplexTypeDefinition>(), (s, o) => (s, o))
            .SelectMany(
                x =>
                    x.o.Directives.AsEnumerable().Where(d => d.Name == DirectiveNames.Key),
                    (x, d) => (d, x.o, x.s));

        var validator = new SelectionSetValidator(schema);

        foreach (var (sourceKeyDirective, sourceComplexType, sourceSchema) in sourceKeyDirectives)
        {
            var fieldsArgument = ((StringValueNode)sourceKeyDirective.Arguments[ArgumentNames.Fields]).Value;
            var selectionSet = ParseSelectionSet($"{{ {fieldsArgument} }}");
            var typeDefinition = schema.Types[sourceComplexType.Name];
            var errors = validator.Validate(selectionSet, typeDefinition);

            if (errors.Any())
            {
                context.Log.Write(
                    KeyInvalidFields(
                        sourceKeyDirective,
                        sourceComplexType.Name,
                        sourceSchema,
                        errors));
            }
        }
    }
}
