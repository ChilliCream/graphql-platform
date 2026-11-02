using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithFragments.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Qux</c> type in subgraph <c>b</c>.
/// Shareable.
/// </summary>
public sealed class QuxType : ObjectType<Qux>
{
    protected override void Configure(IObjectTypeDescriptor<Qux> descriptor)
    {
        descriptor
            .Implements<FooInterfaceType>()
            .Implements<BarInterfaceType>()
            .Shareable();

        descriptor.Field(q => q.Foo).Type<NonNullType<StringType>>();
        descriptor.Field(q => q.Bar).Type<NonNullType<StringType>>();
        descriptor.Field(q => q.QuxValue).Name("qux").Type<NonNullType<StringType>>();
    }
}
