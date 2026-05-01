using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

/// <summary>
/// Descriptor for the <c>Post</c> interface in subgraph <c>b</c>.
/// </summary>
public sealed class PostInterfaceType : InterfaceType<IPost>
{
    protected override void Configure(IInterfaceTypeDescriptor<IPost> descriptor)
    {
        descriptor.Name("Post");
        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.CreatedAt).Type<NonNullType<StringType>>();
    }
}
