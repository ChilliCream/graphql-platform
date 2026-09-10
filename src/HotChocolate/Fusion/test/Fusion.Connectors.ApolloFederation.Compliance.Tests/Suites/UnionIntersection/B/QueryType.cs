using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionIntersection.B;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("media")
            .Type<MediaUnionType>()
            .Shareable()
            .Resolve(_ => BData.Media);

        descriptor
            .Field("bMedia")
            .Type<MediaUnionType>()
            .Shareable()
            .Resolve(_ => BData.Media);

        descriptor
            .Field("book")
            .Type<MediaUnionType>()
            .Shareable()
            .Resolve(_ => BData.Media);

        descriptor
            .Field("viewer")
            .Type<ViewerType>()
            .Shareable()
            .Resolve(_ => new Viewer
            {
                Media = BData.Media,
                BMedia = BData.MovieData,
                Book = BData.Media
            });
    }
}
