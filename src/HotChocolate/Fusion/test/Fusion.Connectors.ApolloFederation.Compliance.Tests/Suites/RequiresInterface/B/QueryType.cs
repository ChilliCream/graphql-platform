using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresInterface.B;

/// <summary>
/// Root <c>Query</c> for subgraph <c>b</c>. Exposes <c>b: User</c>
/// which returns user u2 (with WorkAddress).
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("b")
            .Type<UserType>()
            .Resolve(_ => BData.Users[1]);
    }
}
