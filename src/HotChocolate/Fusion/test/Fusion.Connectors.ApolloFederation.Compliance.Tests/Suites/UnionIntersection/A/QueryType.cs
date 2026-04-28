using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionIntersection.A;

public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("media")
            .Type<MediaUnionType>()
            .Shareable()
            .Resolve(_ => AData.Media);

        descriptor
            .Field("aMedia")
            .Type<MediaUnionType>()
            .Shareable()
            .Resolve(_ => AData.Media);

        descriptor
            .Field("book")
            .Type<MediaUnionType>()
            .Shareable()
            .Resolve(_ => AData.Media);

        descriptor
            .Field("song")
            .Type<MediaUnionType>()
            .Shareable()
            .Resolve(_ => AData.SongData);

        descriptor
            .Field("viewer")
            .Type<ViewerType>()
            .Shareable()
            .Resolve(_ => new Viewer
            {
                Media = AData.Media,
                AMedia = AData.Media,
                Book = AData.Media,
                Song = AData.SongData
            });
    }
}
