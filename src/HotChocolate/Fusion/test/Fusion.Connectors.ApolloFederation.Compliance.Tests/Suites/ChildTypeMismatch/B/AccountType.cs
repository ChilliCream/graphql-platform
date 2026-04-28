using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ChildTypeMismatch.B;

/// <summary>
/// Union type <c>Account = User | Admin</c> in the <c>b</c> subgraph.
/// </summary>
public sealed class AccountType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("Account");
        descriptor.Type<UserType>();
        descriptor.Type<AdminType>();
    }
}
