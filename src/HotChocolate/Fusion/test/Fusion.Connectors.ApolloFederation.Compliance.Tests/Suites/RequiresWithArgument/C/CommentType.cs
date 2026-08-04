using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.C;

/// <summary>
/// Apollo Federation descriptor for the <c>Comment</c> entity in the
/// <c>c</c> subgraph. Owns <c>authorId</c> and <c>body</c>.
/// </summary>
public sealed class CommentType : ObjectType<Comment>
{
    protected override void Configure(IObjectTypeDescriptor<Comment> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();
        descriptor.Field(c => c.AuthorId).Type<IdType>();
        descriptor.Field(c => c.Body).Type<NonNullType<StringType>>();
    }

    private static Comment? ResolveById(string id)
        => CData.CommentsById.TryGetValue(id, out var comment) ? comment : null;
}
