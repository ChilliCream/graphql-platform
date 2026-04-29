using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Fed2ExternalExtension.B;

/// <summary>
/// Root <c>Query</c> type for the <c>b</c> subgraph. Exposes
/// <c>userById(id: ID): User</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("userById")
            .Argument("id", a => a.Type<IdType>())
            .Type<UserType>()
            .Resolve(ctx =>
            {
                var id = ctx.ArgumentValue<string?>("id");
                return id is not null && BData.ById.TryGetValue(id, out var user)
                    ? user
                    : null;
            });
    }
}
