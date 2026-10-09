using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresCircular.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Post</c> entity in
/// subgraph <c>a</c>. <c>byNovice</c> is external;
/// <c>byExpert</c> requires <c>byNovice</c>.
/// </summary>
public sealed class PostType : ObjectType<Post>
{
    protected override void Configure(IObjectTypeDescriptor<Post> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.ByNovice).External().Type<NonNullType<BooleanType>>();

        descriptor
            .Field("byExpert")
            .Type<NonNullType<BooleanType>>()
            .Requires("byNovice")
            .Resolve(ctx =>
            {
                var post = ctx.Parent<Post>();
                if (post.ByNovice is not bool byNovice)
                {
                    throw new InvalidOperationException(
                        "byExpert requires byNovice on the parent entity.");
                }
                return !byNovice;
            });
    }

    private static Post? ResolveById(string id)
        => PostData.ById.TryGetValue(id, out _)
            ? new Post { Id = id }
            : null;
}
