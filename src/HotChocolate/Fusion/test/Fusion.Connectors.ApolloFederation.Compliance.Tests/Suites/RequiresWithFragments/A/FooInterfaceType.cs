using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.A;

/// <summary>
/// Descriptor for the <c>Foo</c> interface in subgraph <c>a</c>.
/// </summary>
public sealed class FooInterfaceType : InterfaceType<IFoo>
{
    protected override void Configure(IInterfaceTypeDescriptor<IFoo> descriptor)
    {
        descriptor.Name("Foo");
        descriptor.Field(f => f.Foo).Type<NonNullType<StringType>>();
    }
}
