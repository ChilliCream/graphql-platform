using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// An Object type must define one or more fields.
/// </summary>
/// <seealso href="https://spec.graphql.org/draft/#sec-Objects.Type-Validation">
/// Specification
/// </seealso>
public sealed class NonEmptyObjectTypeRule : IValidationEventHandler<ObjectTypeEvent>
{
    /// <summary>
    /// Checks that the Object type defines one or more fields.
    /// </summary>
    public void Handle(ObjectTypeEvent @event, ValidationContext context)
    {
        var objectType = @event.ObjectType;

        if (!objectType.Fields.Any())
        {
            context.Log.Write(EmptyObjectType(objectType));
        }
    }
}
