using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideWithRequires.C;

/// <summary>
/// Root <c>Query</c> for subgraph <c>c</c>. Exposes <c>userInC: User</c>
/// returning the third seeded user.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("userInC")
            .Type<UserType>()
            .Resolve(_ => CData.Users[2]);
    }
}
