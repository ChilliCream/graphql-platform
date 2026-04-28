using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresCircular.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Post</c> entity in
/// subgraph <c>b</c>. Owns <c>author</c> and <c>byNovice</c>.
/// <c>byNovice</c> requires <c>author { yearsOfExperience }</c>.
/// </summary>
public sealed class PostType : ObjectType<Post>
{
    protected override void Configure(IObjectTypeDescriptor<Post> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field(p => p.Author)
            .Type<NonNullType<AuthorType>>()
            .Resolve(ctx =>
            {
                var post = ctx.Parent<Post>();
                return post.Author ?? PostData.GetAuthorForPost(post.Id);
            });

        descriptor
            .Field("byNovice")
            .Type<NonNullType<BooleanType>>()
            .Requires("author { yearsOfExperience }")
            .Resolve(ctx =>
            {
                var post = ctx.Parent<Post>();
                if (post.Author?.YearsOfExperience is not int yearsOfExperience)
                {
                    throw new InvalidOperationException(
                        "byNovice requires author.yearsOfExperience on the parent entity.");
                }
                return yearsOfExperience < 10;
            });
    }

    private static Post? ResolveById(string id)
        => PostData.ById.Contains(id)
            ? new Post { Id = id, Author = PostData.GetAuthorForPost(id) }
            : null;
}
