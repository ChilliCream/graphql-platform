using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Article</c> entity in the
/// <c>a</c> subgraph. Implements <c>Media</c>, keyed by <c>id</c>.
/// </summary>
public sealed class ArticleType : ObjectType<Article>
{
    protected override void Configure(IObjectTypeDescriptor<Article> descriptor)
    {
        descriptor
            .Implements<MediaInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.Title).Type<StringType>();
        descriptor.Field(a => a.WordCount).Type<IntType>();
    }

    private static Article ResolveById(string id) => AData.ArticleById(id);
}
