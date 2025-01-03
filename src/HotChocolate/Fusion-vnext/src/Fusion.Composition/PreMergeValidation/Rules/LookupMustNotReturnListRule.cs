using HotChocolate.Fusion.Events;
using HotChocolate.Skimmed;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// Fields annotated with the <c>@lookup</c> directive are intended to retrieve a single entity
/// based on provided arguments. To avoid ambiguity in entity resolution, such fields must return a
/// single object and not a list. This validation rule enforces that any field annotated with
/// <c>@lookup</c> must have a return type that is <b>NOT</b> a list.
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec--lookup-must-not-return-a-list">
/// Specification
/// </seealso>
internal sealed class LookupMustNotReturnListRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, type, schema) = @event;

        if (ValidationHelper.IsLookup(field) && field.Type.NullableType() is ListTypeDefinition)
        {
            context.Log.Write(LookupMustNotReturnList(field, type, schema));
        }
    }
}
