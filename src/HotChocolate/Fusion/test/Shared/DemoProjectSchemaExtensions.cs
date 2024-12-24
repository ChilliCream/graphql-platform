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

    public const string AccountsExtensionWithCostSdl =
        """
        extend type Query {
          userById(id: ID! @is(field: "id")): User! @cost(weight: "1.0")
          usersById(ids: [ID!]! @is(field: "id")): [User!]!
        }
        """;

    public const string AccountsExtensionWithTagSdl =
        """
        extend type Query {
          userById(id: ID! @is(field: "id")): User!
          usersById(ids: [ID!]! @is(field: "id")): [User!]!
          someTypeById(id: ID! @is(field: "id")): SomeType!
        }

        type SomeType @tag(name: "internal") {
          id: ID!
        }

        extend type User {
          birthdate: Date! @tag(name: "internal")
        }

        input AddUserInput {
          birthdate: Date! @tag(name: "internal")
          name: String!
          username: String!
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

    public const string ReviewsExtensionWithCostSdl =
        """
        extend type Query {
          authorById(id: ID! @is(field: "id")): Author @cost(weight: "2.0")
          productById(id: ID! @is(field: "id")): Product
        }

        schema
            @rename(coordinate: "Query.authorById", newName: "userById")
            @rename(coordinate: "Author", newName: "User") {
        }
        """;

    public const string ReviewsExtensionWithTagSdl =
        """
        extend type Query {
          authorById(id: ID! @is(field: "id")): Author
          productById(id: ID! @is(field: "id")): Product
        }

        schema
            @tag(name: "review")
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

    public const string Reviews2ExtensionWithCostSdl =
        """
        extend type Query {
          authorById(id: ID! @is(field: "id")): User @cost(weight: "2.0")
          productById(id: ID! @is(field: "id")): Product @cost(weight: "1.0")
        }

        extend type User {
            reviews: [Review!]! @listSize(assumedSize: 10)
        }

        schema
            @rename(coordinate: "Query.authorById", newName: "userById") {
        }

        directive @listSize(
            assumedSize: Int
            slicingArguments: [String!]
            slicingArgumentDefaultValue: Int
            sizedFields: [String!]
            requireOneSlicingArgument: Boolean! = true) on FIELD_DEFINITION
        """;

    public const string ProductsExtensionSdl =
        """
        extend type Query {
          productById(id: ID! @is(field: "id")): Product
        }

        extend type User {
            productConfigurationByUsername(username: String! @require(field: "username")): ProductConfiguration
            productBookmarkByUsername(username: String! @require(field: "username")): ProductBookmark
        }
        """;

    public const string ShippingExtensionSdl =
        """
        extend type Query {
          productById(id: ID! @is(field: "id")): Product
        }

        extend type Product {
          deliveryEstimate(
            size: Int! @require(field: "dimension { size }"),
            weight: Int! @require(field: "dimension { weight }"),
            zip: String!): DeliveryEstimate!
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
