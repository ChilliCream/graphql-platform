using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Validators;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.PostMergeValidationRules;

/// <summary>
/// Validates that the selection map of <c>@require(field: "…")</c> selects existing fields of the
/// composed schema and that the source schema declaring the requirement does not serve any field
/// at the first level of the selection map, reporting a <c>REQUIRE_INVALID_FIELDS</c> error otherwise.
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
                x => x.f.Arguments.AsEnumerable().Where(a => a.HasRequireDirective),
                (x, a) => new FieldArgumentInfo(a, x.f, x.o, x.s))
            .Where(info =>
                schema.Types.ContainsName(info.Argument.Type.AsTypeDefinition().Name)
                && schema.Types.ContainsName(info.Type.Name));

        var validator = new FieldSelectionMapValidator(
            schema,
            disallowNullableFieldsOnPathToNonNullInputType: true);

        foreach (var (sourceArgument, _, sourceType, sourceSchema) in sourceArgumentGroup)
        {
            var requireDirective = sourceArgument.Directives[Require].First();
            var fieldArgumentValue = (string)requireDirective.Arguments[Field].Value!;
            var fieldSelectionMapParser = new FieldSelectionMapParser(fieldArgumentValue);
            var fieldSelectionMap = fieldSelectionMapParser.Parse();
            var inputTypeNode = sourceArgument.Type.ToTypeNode();
            var inputTypeDefinition = schema.Types[sourceArgument.Type.AsTypeDefinition().Name];
            var inputType = inputTypeNode.RewriteToType(inputTypeDefinition);
            var outputTypeNode = sourceType.ToTypeNode();
            var outputTypeDefinition = schema.Types[sourceType.Name];
            var outputType = outputTypeNode.RewriteToType(outputTypeDefinition);
            var errors = validator.Validate(fieldSelectionMap, inputType, outputType);

            // A first-level field of the selection map must not be served by the requiring schema.
            if (outputTypeDefinition is MutableComplexTypeDefinition complexType)
            {
                var requiringSchemaName = StringUtilities.ToConstantCase(sourceSchema.Name);

                foreach (var field in GetFirstLevelFields(fieldSelectionMap, complexType))
                {
                    if (IsServedBy(field, requiringSchemaName))
                    {
                        errors =
                            errors.Add(
                                string.Format(
                                    RequireInvalidFieldsRule_RequiredFieldServedByRequiringSchema,
                                    field.Coordinate.ToString(),
                                    sourceSchema.Name));
                    }
                }
            }

            if (errors.Any())
            {
                context.Log.Write(
                    RequireInvalidFields(
                        requireDirective,
                        sourceArgument,
                        sourceSchema,
                        errors));
            }
        }
    }

    /// <summary>
    /// Gets the distinct fields of <paramref name="type"/> that the selection map selects at its
    /// first level, in selection order.
    /// </summary>
    private static List<MutableOutputFieldDefinition> GetFirstLevelFields(
        IValueSelectionNode fieldSelectionMap,
        MutableComplexTypeDefinition type)
    {
        var fields = new List<MutableOutputFieldDefinition>();
        var selectionSet = ValueSelectionToSelectionSetRewriter.Rewrite([fieldSelectionMap]);

        CollectFirstLevelFields(selectionSet, type, fields);

        return fields;
    }

    private static void CollectFirstLevelFields(
        SelectionSetNode selectionSet,
        MutableComplexTypeDefinition type,
        List<MutableOutputFieldDefinition> fields)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode
                    when type.Fields.TryGetField(fieldNode.Name.Value, out var field)
                        && !fields.Contains(field):
                    fields.Add(field);
                    break;

                case InlineFragmentNode inlineFragment:
                    CollectFirstLevelFields(inlineFragment.SelectionSet, type, fields);
                    break;
            }
        }
    }

    /// <summary>
    /// Determines whether the source schema contributes the merged field without declaring it
    /// external, either as a partial field or as an external key field it only echoes.
    /// </summary>
    private static bool IsServedBy(MutableOutputFieldDefinition field, string schemaName)
    {
        var fusionFieldDirective =
            field.Directives.AsEnumerable().FirstOrDefault(
                d => d.Name == FusionField && (string)d.Arguments[Schema].Value! == schemaName);

        if (fusionFieldDirective is null)
        {
            return false;
        }

        var isPartial =
            fusionFieldDirective.Arguments.TryGetValue(Partial, out var partial)
            && (bool)partial.Value!;
        var isSourceExternal =
            fusionFieldDirective.Arguments.TryGetValue(SourceExternal, out var sourceExternal)
            && (bool)sourceExternal.Value!;

        return !isPartial && !isSourceExternal;
    }
}
