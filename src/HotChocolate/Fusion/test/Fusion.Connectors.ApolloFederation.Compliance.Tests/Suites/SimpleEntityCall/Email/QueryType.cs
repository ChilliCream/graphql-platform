using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleEntityCall.Email;

/// <summary>
/// Root <c>Query</c> type for the <c>email</c> subgraph. Exposes a single
/// <c>user: User</c> field that returns the first seeded user.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);
        descriptor
            .Field("user")
            .Type<UserType>()
            .Resolve(_ => EmailData.Users[0]);
    }
}
