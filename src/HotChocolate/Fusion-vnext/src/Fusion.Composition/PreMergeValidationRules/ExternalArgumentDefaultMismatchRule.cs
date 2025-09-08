using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using static HotChocolate.Language.SyntaxComparer;

namespace HotChocolate.Fusion.PreMergeValidationRules;

/// <summary>
/// This rule ensures that arguments on fields marked as <c>@external</c> have default values
/// compatible with the corresponding arguments on fields from other source schemas where the field
/// is defined (non-<c>@external</c>).
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-External-Argument-Default-Mismatch">
/// Specification
/// </seealso>
internal sealed class ExternalArgumentDefaultMismatchRule : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var (fieldName, fieldGroup, typeName) = @event;

        var externalFieldGroup =
            fieldGroup.Where(i => i.Field.HasExternalDirective()).ToImmutableHashSet();

        if (externalFieldGroup.IsEmpty)
        {
            return;
        }

        var argumentNames = fieldGroup
            .SelectMany(i => i.Field.Arguments.AsEnumerable(), (_, arg) => arg.Name)
            .ToImmutableHashSet();

        foreach (var argumentName in argumentNames)
        {
            var argumentGroup =
                fieldGroup
                    .SelectMany(
                        i => i.Field.Arguments.AsEnumerable().Where(a => a.Name == argumentName),
                        (i, a) => new FieldArgumentInfo(a, i.Field, i.Type, i.Schema))
                    .ToImmutableHashSet();

            var externalArgumentGroup =
                externalFieldGroup
                    .SelectMany(
                        i => i.Field.Arguments.AsEnumerable().Where(a => a.Name == argumentName),
                        (i, a) => new FieldArgumentInfo(a, i.Field, i.Type, i.Schema));

            foreach (var (externalArgument, _, _, externalSchema) in externalArgumentGroup)
            {
                foreach (var (argument, _, _, schema) in argumentGroup)
                {
                    if (!BySyntax.Equals(argument.DefaultValue, externalArgument.DefaultValue))
                    {
                        context.Log.Write(
                            ExternalArgumentDefaultMismatch(
                                externalArgument.DefaultValue,
                                externalArgument,
                                fieldName,
                                typeName,
                                externalSchema,
                                argument.DefaultValue,
                                schema.Name));
                    }
                }
            }
        }
    }
}
