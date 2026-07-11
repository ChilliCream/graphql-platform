using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Reviews;

/// <summary>
/// Apollo Federation descriptor for the <c>User</c> entity as extended by
/// the <c>reviews</c> subgraph. Owns <c>reviews</c>; <c>username</c> is
/// external. The reference resolver returns just the key, the optional
/// <c>username</c> is supplied by the federation external setter when the
/// gateway dispatches the entity reference along the
/// <c>@provides(fields: "username")</c> path.
/// </summary>
public sealed class UserType : ObjectType<User>
{
    protected override void Configure(IObjectTypeDescriptor<User> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(u => u.Id).Type<NonNullType<IdType>>();
        descriptor.Field(u => u.Username).External().Type<StringType>();

        descriptor
            .Field("reviews")
            .Type<ListType<ReviewType>>()
            .Resolve(ctx =>
            {
                var user = ctx.Parent<User>();
                return ReviewsData.Reviews
                    .Where(r => string.Equals(r.AuthorId, user.Id, StringComparison.Ordinal))
                    .ToArray();
            });
    }

    private static User ResolveById(string id) => new() { Id = id };
}
