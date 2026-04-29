using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.EnumIntersection.A;

/// <summary>
/// Descriptor for the <c>UserType</c> enum in subgraph <c>a</c>: only
/// <c>REGULAR</c> is declared.
/// </summary>
public sealed class UserTypeType : EnumType<UserTypeEnum>
{
    protected override void Configure(IEnumTypeDescriptor<UserTypeEnum> descriptor)
    {
        descriptor.Name("UserType");
    }
}
