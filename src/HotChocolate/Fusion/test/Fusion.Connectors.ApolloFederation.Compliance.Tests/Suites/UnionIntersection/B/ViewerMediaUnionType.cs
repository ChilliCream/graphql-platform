using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionIntersection.B;

public sealed class ViewerMediaUnionType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("ViewerMedia");
        descriptor.Type<BookType>();
        descriptor.Type<MovieType>();
    }
}
