namespace HotChocolate.Fusion;

public static class DemoProjectSchemaExtensions
{
    public const string AccountsExtensionSdl =
        """
        extend type Query {
          userById(id: Int! @is(field: "id")): User!
          usersById(ids: [Int!]! @is(field: "id")): [User!]!
        }
        """;

    public const string ReviewsExtensionSdl =
        """
        extend type Query {
          authorById(id: Int! @is(field: "id")): Author
          productById(upc: Int! @is(field: "upc")): Product
        }

        schema
            @rename(coordinate: "Query.authorById", newName: "userById")
            @rename(coordinate: "Author", newName: "User") {
        }
        """;

    public const string ProductsExtensionSdl =
        """
        extend type Query {
          productById(upc: Int! @is(field: "upc")): Product
        }
        """;
}
