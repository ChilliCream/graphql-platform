using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionIntersection.A;

public sealed class MediaUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("Media");
        descriptor.Type<BookType>();
        descriptor.Type<SongType>();
    }
}
