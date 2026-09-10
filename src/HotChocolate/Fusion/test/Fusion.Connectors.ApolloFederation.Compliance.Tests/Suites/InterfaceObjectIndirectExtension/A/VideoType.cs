using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.InterfaceObjectIndirectExtension.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Video</c> entity in the
/// <c>a</c> subgraph. Implements <c>Media</c>, keyed by <c>id</c>.
/// </summary>
public sealed class VideoType : ObjectType<Video>
{
    protected override void Configure(IObjectTypeDescriptor<Video> descriptor)
    {
        descriptor
            .Implements<MediaInterfaceType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(v => v.Id).Type<NonNullType<IdType>>();
        descriptor.Field(v => v.Title).Type<StringType>();
        descriptor.Field(v => v.Duration).Type<IntType>();
    }

    private static Video ResolveById(string id) => AData.VideoById(id);
}
