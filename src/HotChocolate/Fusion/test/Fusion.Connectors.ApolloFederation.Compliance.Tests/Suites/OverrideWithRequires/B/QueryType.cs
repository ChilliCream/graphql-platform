using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideWithRequires.B;

/// <summary>
/// Root <c>Query</c> for subgraph <c>b</c>. Exposes <c>userInB: User</c>
/// returning the second seeded user.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("userInB")
            .Type<UserType>()
            .Resolve(_ => BData.Users[1]);
    }
}
