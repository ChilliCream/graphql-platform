using HotChocolate.Fusion.Suites.Typename.Shared;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Typename.B;

/// <summary>
/// Root <c>Query</c> for the <c>b</c> subgraph. Exposes
/// <c>users: [User]</c>, where <c>User</c> is the audit's
/// <c>@interfaceObject</c> declaration. The audit resolver returns rows
/// with only <c>id</c> populated to demonstrate that
/// <c>@interfaceObject</c> does not contribute the concrete type's
/// <c>__typename</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("users")
            .Type<ListType<UserType>>()
            .Resolve(_ => TypenameData.Users
                .Select(row => new User { Id = row.Id, Name = row.Name })
                .ToArray());
    }
}
