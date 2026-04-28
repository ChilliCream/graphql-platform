using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionIntersection.B;

public sealed class MediaUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("Media");
        descriptor.Type<BookType>();
        descriptor.Type<MovieType>();
    }
}
