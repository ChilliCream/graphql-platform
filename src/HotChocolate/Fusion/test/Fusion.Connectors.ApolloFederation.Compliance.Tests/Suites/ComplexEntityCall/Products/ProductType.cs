using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

/// <summary>
/// Apollo Federation descriptor for the <c>Product</c> entity in the
/// <c>products</c> subgraph. The subgraph extends the federated <c>Product</c>
/// type (<c>@extends</c> in the audit SDL) by adding a local <c>category</c>
/// field while keeping the <c>id</c> field external.
/// </summary>
public sealed class ProductType : ObjectType<Product>
{
    protected override void Configure(IObjectTypeDescriptor<Product> descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        // Apollo Federation would mark this as '@external' (the field is
        // owned by the link/list/price subgraphs). In the Fusion composite
        // schema we model the same intent with '@shareable': the field
        // travels with Product instances returned from this subgraph's root
        // queries so the planner can use it as the entity-routing key.
        descriptor.Field(p => p.Id)
            .Shareable()
            .Type<NonNullType<StringType>>();

        descriptor.Field("category")
            .Type<CategoryType>()
            .Shareable()
            .Resolve(ctx =>
            {
                var product = ctx.Parent<Product>();
                return ProductsData.CategoriesById.TryGetValue(product.CategoryId, out var category)
                    ? category
                    : null;
            });

        // Ignore the CategoryId property: it is not part of the public schema.
        descriptor.Ignore(p => p.CategoryId);
    }

    private static Product? ResolveById(string id)
        => ProductsData.ById.TryGetValue(id, out var p) ? p : null;
}
