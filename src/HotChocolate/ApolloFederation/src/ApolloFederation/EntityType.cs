using HotChocolate.Types;
using static HotChocolate.ApolloFederation.WellKnownTypeNames;

namespace HotChocolate.ApolloFederation
{
    /// <summary>
    /// A union called _Entity which is a union of all types that use the @key directive,
    /// including both types native to the schema and extended types.
    /// </summary>
    public sealed class EntityType : UnionType
    {
        protected override void Configure(IUnionTypeDescriptor descriptor)
        {
            descriptor.Name(Entity);
        }
    }
}
