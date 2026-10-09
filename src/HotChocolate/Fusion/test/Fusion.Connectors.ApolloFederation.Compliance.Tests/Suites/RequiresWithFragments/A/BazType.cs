using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Baz</c> type in subgraph <c>a</c>.
/// Implements <c>Foo</c> and <c>Bar</c>, shareable.
/// </summary>
public sealed class BazType : ObjectType<Baz>
{
    protected override void Configure(IObjectTypeDescriptor<Baz> descriptor)
    {
        descriptor
            .Implements<FooInterfaceType>()
            .Implements<BarInterfaceType>()
            .Shareable();

        descriptor.Field(b => b.Foo).Type<NonNullType<StringType>>();
        descriptor.Field(b => b.Bar).Type<NonNullType<StringType>>();
        descriptor.Field(b => b.BazValue).Name("baz").Type<NonNullType<StringType>>();
    }
}
