using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// Descriptor for the <c>Bar</c> interface in subgraph <c>b</c>.
/// <c>Bar</c> implements <c>Foo</c>.
/// </summary>
public sealed class BarInterfaceType : InterfaceType<IBar>
{
    protected override void Configure(IInterfaceTypeDescriptor<IBar> descriptor)
    {
        descriptor.Name("Bar");
        descriptor.Implements<FooInterfaceType>();
        descriptor.Field(b => b.Foo).Type<NonNullType<StringType>>();
        descriptor.Field(b => b.Bar).Type<NonNullType<StringType>>();
    }
}
