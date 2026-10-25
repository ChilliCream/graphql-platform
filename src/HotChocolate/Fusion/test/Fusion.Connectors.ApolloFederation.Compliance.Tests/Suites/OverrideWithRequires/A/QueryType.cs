using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideWithRequires.A;

/// <summary>
/// Root <c>Query</c> for subgraph <c>a</c>. Exposes <c>userInA: User</c>
/// returning the first seeded user.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("userInA")
            .Type<UserType>()
            .Resolve(_ => AData.Users[0]);
    }
}
