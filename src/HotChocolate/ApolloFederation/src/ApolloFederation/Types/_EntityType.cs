using static HotChocolate.ApolloFederation.FederationTypeNames;
using static HotChocolate.ApolloFederation.Properties.FederationResources;

namespace HotChocolate.ApolloFederation.Types;

/// <summary>
/// A union called _Entity which is a union of all types that use the @key directive,
/// including both types native to the schema and extended types.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class _EntityType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor
            .Name(EntityType_Name)
            .Description(EntityType_Description);
    }
}
