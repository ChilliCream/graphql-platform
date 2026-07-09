using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// A Union type must define one or more member types.
/// </summary>
/// <seealso href="https://spec.graphql.org/draft/#sec-Unions.Type-Validation">
/// Specification
/// </seealso>
public sealed class NonEmptyUnionTypeRule : IValidationEventHandler<UnionTypeEvent>
{
    /// <summary>
    /// Checks that the Union type defines one or more member types.
    /// </summary>
    public void Handle(UnionTypeEvent @event, ValidationContext context)
    {
        var unionType = @event.UnionType;

        if (!unionType.Types.Any())
        {
            context.Log.Write(EmptyUnionType(unionType));
        }
    }
}
