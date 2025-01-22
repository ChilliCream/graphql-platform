using System.Collections.Immutable;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Extensions;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// When multiple schemas define the same field name on the same output type (e.g.,
/// <c>User.field</c>), these fields can be merged if their arguments are compatible. Compatibility
/// extends not only to the output field types themselves, but to each argument’s input type as
/// well. The schemas must agree on each argument’s name and have compatible types, so that the
/// composed schema can unify the definitions into a single consistent field specification.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Field-Argument-Types-Mergeable">
/// Specification
/// </seealso>
internal sealed class FieldArgumentTypesMergeableRule : IEventHandler<FieldArgumentGroupEvent>
{
    public void Handle(FieldArgumentGroupEvent @event, CompositionContext context)
    {
        var (_, argumentGroup, fieldName, typeName) = @event;

        // Filter out arguments in inaccessible/internal types and fields.
        var argumentGroupVisible = argumentGroup
            .Where(
                i =>
                    !i.Type.HasInaccessibleDirective()
                    && !i.Type.HasInternalDirective()
                    && !i.Field.HasInaccessibleDirective()
                    && !i.Field.HasInternalDirective())
            .ToImmutableArray();

        for (var i = 0; i < argumentGroupVisible.Length - 1; i++)
        {
            var argumentInfoA = argumentGroupVisible[i];
            var argumentInfoB = argumentGroupVisible[i + 1];
            var typeA = argumentInfoA.Argument.Type;
            var typeB = argumentInfoB.Argument.Type;

            if (!ValidationHelper.SameTypeShape(typeA, typeB))
            {
                context.Log.Write(
                    FieldArgumentTypesNotMergeable(
                        argumentInfoA.Argument,
                        fieldName,
                        typeName,
                        argumentInfoA.Schema,
                        argumentInfoB.Schema));
            }
        }
    }
}
