using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.A;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity owned by the
/// <c>a</c> subgraph. Resolves by <c>id</c> and exposes <c>name</c> and
/// <c>pid</c>.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(p => p.Id).Type<NonNullType<StringType>>();

        // Subgraph 'b' references 'name' as part of its '@key(fields: "id name")'
        // and models that external key field as '@shareable'. The composer
        // therefore requires the owning definition in subgraph 'a' to be
        // shareable as well, mirroring how Apollo Federation treats key fields.
        descriptor.Field(p => p.Name).Shareable().Type<StringType>();
        descriptor.Field(p => p.Pid).Type<StringType>();
    }

    private static Product? ResolveById(string id)
        => AData.ById.TryGetValue(id, out var p) ? p : null;
}
