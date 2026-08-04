using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Baz</c> type in subgraph <c>b</c>.
/// Shareable and inaccessible.
/// </summary>
public sealed class BazType : ObjectType<Baz>
{
    protected override void Configure(IObjectTypeDescriptor<Baz> descriptor)
    {
        descriptor
            .Implements<FooInterfaceType>()
            .Implements<BarInterfaceType>()
            .Shareable()
            .Directive(InaccessibleDirective.Default);

        descriptor.Field(b => b.Foo).Type<NonNullType<StringType>>();
        descriptor.Field(b => b.Bar).Type<NonNullType<StringType>>();
        descriptor.Field(b => b.BazValue).Name("baz").Type<NonNullType<StringType>>();
    }
}
