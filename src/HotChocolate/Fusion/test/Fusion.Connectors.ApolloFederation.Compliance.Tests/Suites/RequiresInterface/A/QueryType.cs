using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresInterface.A;

/// <summary>
/// Root <c>Query</c> for subgraph <c>a</c>. Exposes <c>a: User</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("a")
            .Type<UserType>()
            .Resolve(_ =>
            {
                var record = AData.Users[0];
                return new User { Id = record.Id, Name = record.Name };
            });
    }
}
