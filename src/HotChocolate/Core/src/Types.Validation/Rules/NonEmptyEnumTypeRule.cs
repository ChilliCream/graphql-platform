using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// An Enum type must define one or more enum values.
/// </summary>
/// <seealso href="https://spec.graphql.org/draft/#sec-Enums.Type-Validation">
/// Specification
/// </seealso>
public sealed class NonEmptyEnumTypeRule : IValidationEventHandler<EnumTypeEvent>
{
    /// <summary>
    /// Checks that the Enum type defines one or more values.
    /// </summary>
    public void Handle(EnumTypeEvent @event, ValidationContext context)
    {
        var enumType = @event.EnumType;

        if (!enumType.Values.Any())
        {
            context.Log.Write(EmptyEnumType(enumType));
        }
    }
}
