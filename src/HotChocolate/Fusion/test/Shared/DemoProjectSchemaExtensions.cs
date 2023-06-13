namespace HotChocolate.Fusion.Shared;

public static class DemoProjectSchemaExtensions
{
    public const string AccountsExtensionSdl =
        """
        extend type Query {
          userById(id: ID! @is(field: "id")): User!
          usersById(ids: [ID!]! @is(field: "id")): [User!]!
        }
        """;

    public const string ReviewsExtensionSdl =
        """
        extend type Query {
          authorById(id: ID! @is(field: "id")): Author
          productById(id: ID! @is(field: "id")): Product
        }

        schema
            @rename(coordinate: "Query.authorById", newName: "userById")
            @rename(coordinate: "Author", newName: "User") {
        }
        """;

    public const string Reviews2ExtensionSdl =
        """
        extend type Query {
          authorById(id: ID! @is(field: "id")): User
          productById(id: ID! @is(field: "id")): Product
        }

        schema
            @rename(coordinate: "Query.authorById", newName: "userById") {
        }
        """;

    public const string ProductsExtensionSdl =
        """
        extend type Query {
          productById(id: ID! @is(field: "id")): Product
        }
        """;

    public const string ProductsExtensionSdl2 =
        """
        extend type Query {
          productById(id: ID! @is(field: "id")): Product
        }

        extend type Product {
          delivery(
              zip: String!
              size: Int @is(field: "dimension { size }")
              weight: Int @is(field: "dimension { weight }")
            ): DeliveryEstimates
        }

        type DeliveryEstimates {
            days: Int!
        }
        """;

    public const string ShippingExtensionSdl =
        """
        extend type Query {
          productDimensionByProductId(productId: ID! @is(field: "productId")): ProductDimension
        }

        extend type ProductDimension {
          productId: ID! @is(coordinate: "Product.id") @internal
        }

        extend type Product {
          dimension: ProductDimension
        }
        """;

    public const string ShippingExtensionSdl2 =
        """
        extend type Product {
          dimension: ProductDimension
            @declare(variable: "productId" select: "id")
            @map(select: "productDimensionByProductId(id: $productId)")
        }
        """;
}
