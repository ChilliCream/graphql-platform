using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.EnumIntersection.A;

/// <summary>
/// Descriptor for the <c>UserType</c> enum in subgraph <c>a</c>: only
/// <c>REGULAR</c> is declared in the GraphQL schema. The runtime enum also
/// defines <c>ANONYMOUS</c>, which is intentionally not bound here so that
/// serializing it surfaces a subgraph error (mirroring the audit).
/// </summary>
public sealed class UserTypeType : EnumType<UserTypeEnum>
{
    protected override void Configure(IEnumTypeDescriptor<UserTypeEnum> descriptor)
    {
        descriptor.Name("UserType");
        descriptor.BindValuesExplicitly();
        descriptor.Value(UserTypeEnum.REGULAR);
    }
}
