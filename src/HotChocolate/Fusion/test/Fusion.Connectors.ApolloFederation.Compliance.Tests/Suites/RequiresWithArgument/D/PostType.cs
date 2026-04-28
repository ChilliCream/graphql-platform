using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.D;

/// <summary>
/// Apollo Federation descriptor for the <c>Post</c> entity in the
/// <c>d</c> subgraph. Extends <c>Post</c> with <c>author</c> (which
/// requires <c>comments(limit: 3) { authorId }</c>) and
/// <c>comments(limit: Int!)</c>.
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
            .Field("author")
            .Type<AuthorType>()
            .Requires("""comments(limit: 3) { authorId }""")
            .Resolve(ctx =>
            {
                var post = ctx.Parent<Post>();
                if (post.Comments is not { Count: 3 } comments)
                {
                    return null;
                }

                var authorId = comments[2].AuthorId;
                if (authorId is null)
                {
                    return null;
                }

                return DData.AuthorsById.TryGetValue(authorId, out var author)
                    ? author
                    : null;
            });

        descriptor
            .Field("comments")
            .Type<ListType<CommentType>>()
            .Argument("limit", a => a.Type<NonNullType<IntType>>())
            .Resolve(ctx =>
            {
                var post = ctx.Parent<Post>();
                var limit = ctx.ArgumentValue<int>("limit");

                if (!DData.CommentsByPostId.TryGetValue(post.Id, out var comments))
                {
                    return Array.Empty<Comment>();
                }

                return comments
                    .Take(limit)
                    .Select(static c => new Comment { Id = c.Id })
                    .ToList();
            });
    }

    private static Post? ResolveById(string id)
        => DData.PostsById.ContainsKey(id)
            ? new Post { Id = id }
            : null;
}
