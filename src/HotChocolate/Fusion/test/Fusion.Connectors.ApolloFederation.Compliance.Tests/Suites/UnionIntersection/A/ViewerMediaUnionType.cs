using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionIntersection.A;

public sealed class ViewerMediaUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("ViewerMedia");
        descriptor.Type<BookType>();
        descriptor.Type<SongType>();
    }
}
