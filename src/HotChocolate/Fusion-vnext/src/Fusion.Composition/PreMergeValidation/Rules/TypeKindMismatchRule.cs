using HotChocolate.Fusion.Events;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidation.Rules;

/// <summary>
/// <para>
/// Each named type must represent the <b>same</b> kind of GraphQL type across all source schemas.
/// For instance, a type named <c>User</c> must consistently be an object type, or consistently be
/// an interface, and so forth. If one schema defines <c>User</c> as an object type, while another
/// schema declares <c>User</c> as an interface (or input object, union, etc.), the schema
/// composition process cannot merge these definitions coherently.
/// </para>
/// <para>
/// This rule ensures semantic consistency: a single type name cannot serve multiple, incompatible
/// purposes in the final composed schema.
/// </para>
/// </summary>
/// <seealso href="https://graphql.github.io/composite-schemas-spec/draft/#sec-Type-Kind-Mismatch">
/// Specification
/// </seealso>
internal sealed class TypeKindMismatchRule : IEventHandler<TypeGroupEvent>
{
    public void Handle(TypeGroupEvent @event, CompositionContext context)
    {
        var (_, typeGroup) = @event;

        for (var i = 0; i < typeGroup.Length - 1; i++)
        {
            var typeInfoA = typeGroup[i];
            var typeInfoB = typeGroup[i + 1];
            var typeKindA = typeInfoA.Type.Kind;
            var typeKindB = typeInfoB.Type.Kind;

            if (typeKindA != typeKindB)
            {
                context.Log.Write(
                    TypeKindMismatch(
                        typeInfoA.Type,
                        typeInfoA.Schema,
                        typeKindA.ToString(),
                        typeInfoB.Schema,
                        typeKindB.ToString()));
            }
        }
    }
}
