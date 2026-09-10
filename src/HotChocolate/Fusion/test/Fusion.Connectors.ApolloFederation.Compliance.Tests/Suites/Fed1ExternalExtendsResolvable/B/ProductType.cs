using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.B;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the <c>b</c>
/// subgraph. The subgraph extends the federated <c>Product</c> type
/// (<c>@extends</c> in the audit SDL) by adding the local <c>price</c> field and
/// resolves the entity by the composite <c>id name</c> key and by <c>upc</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor.ExtendServiceType();

        descriptor
            .Key("id name")
            .ResolveReferenceWith(_ => ResolveByIdAndName(default!, default!));

        descriptor
            .Key("upc")
            .ResolveReferenceWith(_ => ResolveByUpc(default!));

        // Apollo Federation marks 'id', 'name' and 'upc' as '@external' (the
        // fields are owned by subgraph 'a'). In the Fusion composite schema we
        // model the same intent with '@shareable': they travel with Product
        // instances so the planner can use them as entity-routing keys.
        descriptor.Field(p => p.Id).Shareable().Type<StringType>();
        descriptor.Field(p => p.Name).Shareable().Type<StringType>();
        descriptor.Field(p => p.Upc).Shareable().Type<StringType>();
        descriptor.Field(p => p.Price).Type<NonNullType<FloatType>>();
    }

    private static Product? ResolveByIdAndName(string id, string name)
        => BData.Items.FirstOrDefault(
            p => string.Equals(p.Id, id, StringComparison.Ordinal)
                && string.Equals(p.Name, name, StringComparison.Ordinal));

    private static Product? ResolveByUpc(string upc)
        => BData.Items.FirstOrDefault(
            p => string.Equals(p.Upc, upc, StringComparison.Ordinal));
}
