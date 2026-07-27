using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.D;

/// <summary>
/// Apollo Federation descriptor for the <c>Comment</c> entity in the
/// <c>d</c> subgraph. The <c>authorId</c> field is <c>@external</c>,
/// populated from the federation entity representation when the
/// gateway resolves the <c>@requires</c> on <c>Post.author</c>.
/// </summary>
public sealed class CommentType : ObjectType<Comment>
{
    protected override void Configure(IObjectTypeDescriptor<Comment> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
        descriptor.Field(c => c.Date).Type<StringType>();
        descriptor.Field(c => c.AuthorId).External().Type<IdType>();
    }

    private static Comment? ResolveById(string id)
        => DData.CommentsById.ContainsKey(id)
            ? new Comment { Id = id }
            : null;
}
