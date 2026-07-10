using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionIntersection.A;

public sealed class ViewerType : ObjectType<Viewer>
{
    protected override void Configure(IObjectTypeDescriptor<Viewer> descriptor)
    {
        descriptor
            .Field(v => v.Media)
            .Type<ViewerMediaUnionType>()
            .Shareable();

        descriptor
            .Field(v => v.AMedia)
            .Type<ViewerMediaUnionType>();

        descriptor
            .Field(v => v.Book)
            .Type<ViewerMediaUnionType>()
            .Shareable();

        descriptor
            .Field(v => v.Song)
            .Type<ViewerMediaUnionType>()
            .Shareable();
    }
}
