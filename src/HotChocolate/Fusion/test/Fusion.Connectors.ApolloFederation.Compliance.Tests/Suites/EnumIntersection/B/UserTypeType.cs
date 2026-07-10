using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.EnumIntersection.B;

/// <summary>
/// Descriptor for the <c>UserType</c> enum in subgraph <c>b</c>:
/// declares both <c>ANONYMOUS</c> (marked <c>@inaccessible</c>) and
/// <c>REGULAR</c>.
/// </summary>
public sealed class UserTypeType : EnumType<UserTypeEnum>
{
    protected override void Configure(IEnumTypeDescriptor<UserTypeEnum> descriptor)
    {
        descriptor.Name("UserType");

        descriptor.Value(UserTypeEnum.ANONYMOUS).Inaccessible();
        descriptor.Value(UserTypeEnum.REGULAR);
    }
}
