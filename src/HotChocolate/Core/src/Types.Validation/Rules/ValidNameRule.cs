using HotChocolate.Events;
using HotChocolate.Events.Contracts;
using HotChocolate.Types;
using static HotChocolate.Logging.LogEntryHelper;

namespace HotChocolate.Rules;

/// <summary>
/// Any <a href="https://spec.graphql.org/September2025/#Name">Name</a> within a GraphQL type system
/// must not start with two underscores <c>"__"</c> unless it is part of the
/// <a href="https://spec.graphql.org/September2025/#sec-Introspection">introspection system</a> as
/// defined by the <a href="https://spec.graphql.org/September2025/">specification</a>.
/// </summary>
/// <seealso href="https://spec.graphql.org/September2025/#sec-Names.Reserved-Names">
/// Specification
/// </seealso>
public sealed class ValidNameRule : IValidationEventHandler<NamedMemberEvent>
{
    /// <summary>
    /// Checks that named type system members do not start with two underscores.
    /// </summary>
    public void Handle(NamedMemberEvent @event, ValidationContext context)
    {
        var namedMember = @event.NamedMember;

        var isIntrospectionMember =
            namedMember
                is ITypeDefinition { IsIntrospectionType: true }
                or IFieldDefinition { IsIntrospectionField: true };

        if (!isIntrospectionMember && namedMember.Name.StartsWith("__"))
        {
            context.Log.Write(InvalidMemberName(namedMember));
        }
    }
}
