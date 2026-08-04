using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.OverrideTypeInterface.B;

/// <summary>
/// Descriptor for the <c>AnotherPost</c> interface in subgraph <c>b</c>.
/// </summary>
public sealed class AnotherPostInterfaceType : InterfaceType<IAnotherPost>
{
    protected override void Configure(IInterfaceTypeDescriptor<IAnotherPost> descriptor)
    {
        descriptor.Name("AnotherPost");
        descriptor.Field(p => p.Id).Type<NonNullType<IdType>>();
        descriptor.Field(p => p.CreatedAt).Type<NonNullType<StringType>>();
    }
}
