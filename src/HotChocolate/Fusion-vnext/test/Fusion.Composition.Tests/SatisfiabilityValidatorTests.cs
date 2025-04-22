using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion;

public sealed class SatisfiabilityValidatorTests
{
    [Fact]
    public void ExtendingATypeWithNoLookup()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Profile Schema
                type Query {
                    profileById(id: ID!): Profile
                }

                type Profile {
                    id: ID!
                    user: User
                }

                type User {
                    id: ID!
                    name: String
                }
                """,
                """
                # Order Schema
                type Query {
                    orders: [Order]
                }

                type Order {
                    id: ID!
                    user: User
                }

                type User {
                    id: ID!
                    membershipStatus: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            """
            Unable to access the field 'User.membershipStatus' on path 'A:Query.profileById -> A:Profile.user'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:User.membershipStatus'.
                No lookups found for type 'User' in schema 'B'.

            Unable to access the field 'User.name' on path 'B:Query.orders -> B:Order.user'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:User.name'.
                No lookups found for type 'User' in schema 'A'.
            """,
            string.Join("\n\n", log.Select(e => e.Message)));
    }

    [Fact]
    public void CompositeKeyFieldMissingInTheSecondSchema()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema Product
                type Query {
                    # A composite lookup that uses both `id` and `sku`
                    productByIdSku(id: ID!, sku: String!): Product @lookup
                }

                type Product {
                    id: ID!
                    sku: String!
                    name: String
                }
                """,
                """
                # Schema Inventory
                type Query {
                    # A lookup that uses only `id`
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    stock: Int
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            """
            Unable to access the field 'Product.sku' on path 'B:Query.productById'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.sku'.
                Unable to satisfy the requirement 'sku' for lookup 'A:Query.productByIdSku'.
                  Unable to access the required field 'Product.sku'.
                    No other schemas contain this field.

            Unable to access the field 'Product.name' on path 'B:Query.productById'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.name'.
                Unable to satisfy the requirement 'sku' for lookup 'A:Query.productByIdSku'.
                  Unable to access the required field 'Product.sku'.
                    No other schemas contain this field.
            """,
            string.Join("\n\n", log.Select(e => e.Message)));
    }

    [Fact]
    public void KeyMismatchBetweenSourceSchemas()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema Products
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    description: String
                }
                """,
                """
                # Schema Inventory
                type Query {
                    productBySku(sku: String!): Product @lookup
                }

                type Product {
                    sku: String!
                    stock: Int!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            """
            Unable to access the field 'Product.sku' on path 'A:Query.productById'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.sku'.
                Unable to satisfy the requirement 'sku' for lookup 'B:Query.productBySku'.
                  Unable to access the required field 'Product.sku'.
                    No other schemas contain this field.

            Unable to access the field 'Product.stock' on path 'A:Query.productById'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.stock'.
                Unable to satisfy the requirement 'sku' for lookup 'B:Query.productBySku'.
                  Unable to access the required field 'Product.sku'.
                    No other schemas contain this field.

            Unable to access the field 'Product.id' on path 'B:Query.productBySku'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.id'.
                Unable to satisfy the requirement 'id' for lookup 'A:Query.productById'.
                  Unable to access the required field 'Product.id'.
                    No other schemas contain this field.

            Unable to access the field 'Product.description' on path 'B:Query.productBySku'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Product.description'.
                Unable to satisfy the requirement 'id' for lookup 'A:Query.productById'.
                  Unable to access the required field 'Product.id'.
                    No other schemas contain this field.
            """,
            string.Join("\n\n", log.Select(e => e.Message)));
    }

    [Fact]
    public void KeyMismatchBetweenSourceSchemas_Fixed()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema Products
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    description: String
                }
                """,
                """
                # Schema Inventory
                type Query {
                    productBySku(sku: String!): Product @lookup
                }

                type Product {
                    sku: String!
                    stock: Int!
                }
                """,
                """
                # Schema ProductIndex
                type Query {
                    productById(id: ID!): Product @lookup
                    productBySku(sku: String!): Product @lookup
                }

                type Product {
                    id: ID!
                    sku: String!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void MissingFieldInASharedValueType()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema User
                type Query {
                    userById(id: ID!): User @lookup
                }

                type User {
                    id: ID!
                    address: Address
                }

                type Address {
                    street: String
                    city: String
                }
                """,
                """
                # Schema Orders
                type Query {
                    orderById(id: ID!): Order @lookup
                }

                type Order {
                    id: ID!
                    shippingAddress: Address
                }

                type Address {
                    street: String
                    city: String
                    country: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            """
            Unable to access the field 'Address.country' on path 'A:Query.userById -> A:User.address'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Address.country'.
                No lookups found for type 'Address' in schema 'B'.
            """,
            string.Join("\n\n", log.Select(e => e.Message)));
    }

    [Fact]
    public void EntityVsValueTypeConflict()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema Products
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    # Category is just a nested value type here (no @lookup, no id).
                    category: Category
                }

                type Category {
                    name: String
                }
                """,
                """
                # Schema Categories
                type Query {
                    # Category is treated as an entity with its own lookup
                    categoryById(id: ID!): Category @lookup
                }

                type Category {
                    id: ID!
                    description: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            """
            Unable to access the field 'Category.id' on path 'A:Query.productById -> A:Product.category'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Category.id'.
                Unable to satisfy the requirement 'id' for lookup 'B:Query.categoryById'.
                  Unable to access the required field 'Category.id'.
                    No other schemas contain this field.

            Unable to access the field 'Category.description' on path 'A:Query.productById -> A:Product.category'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Category.description'.
                Unable to satisfy the requirement 'id' for lookup 'B:Query.categoryById'.
                  Unable to access the required field 'Category.id'.
                    No other schemas contain this field.

            Unable to access the field 'Category.name' on path 'B:Query.categoryById'.
              Unable to transition between schemas 'B' and 'A' for access to field 'A:Category.name'.
                No lookups found for type 'Category' in schema 'A'.
            """,
            string.Join("\n\n", log.Select(e => e.Message)));
    }

    [Fact]
    public void SharedEntityWithoutALookup()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema Order
                type Query {
                    orderById(id: ID!): Order @lookup
                }

                type Order {
                    id: ID! @shareable
                    user: User
                }

                type User @key(fields: "id") {
                    id: ID!
                    name: String
                }
                """,
                """
                # Schema User
                type Query {
                    userById(id: ID!): User @lookup
                }

                type User @key(fields: "id") {
                    id: ID!
                    name: String
                    email: String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    // Additional tests (not included in the specification).

    [Fact]
    public void CycleInType()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product
                }

                type Product
                {
                    id: ID!
                    relatedProduct: Product!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void UnsatisfiableRequirements()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    title(description: String @require(field: "{ name price }")): String
                }
                """,
                """
                # Schema B
                type Product {
                    id: ID!
                    name: String
                    price: Float!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            """
            Unable to access the field 'Product.title' on path 'A:Query.productById'.
              Unable to satisfy the requirement '{ name price }' on field 'A:Product.title'.
                Unable to access the required field 'Product.name'.
                  Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.name'.
                    No lookups found for type 'Product' in schema 'B'.
                Unable to access the required field 'Product.price'.
                  Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.price'.
                    No lookups found for type 'Product' in schema 'B'.

            Unable to access the field 'Product.name' on path 'A:Query.productById'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.name'.
                No lookups found for type 'Product' in schema 'B'.

            Unable to access the field 'Product.price' on path 'A:Query.productById'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Product.price'.
                No lookups found for type 'Product' in schema 'B'.
            """,
            string.Join("\n\n", log.Select(e => e.Message)));
    }

    [Fact]
    public void UnsatisfiableRequirementsNested()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    title(
                        input: String @require(field: "{ a: category.name b: section.name }")
                    ): String
                }
                """,
                """
                # Schema B
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    category: Category
                    section: Section
                }

                type Category {
                    id: ID!
                }

                type Section {
                    id: ID!
                }
                """,
                """
                # Schema C
                type Query {
                    categoryById(id: ID!): Category @lookup
                    sectionById(id: ID!): Section @lookup
                }

                type Category {
                    id: ID!
                    name: String!
                }

                type Section {
                    id: ID!
                    name: String!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            """
            Unable to access the field 'Product.title' on path 'A:Query.productById'.
              Unable to satisfy the requirement '{ a: category.name b: section.name }' on field 'A:Product.title'.
                Unable to access the required field 'Category.name'.
                  Unable to transition between schemas 'A' and 'C' for access to field 'C:Category.name'.
                    Unable to satisfy the requirement 'id' for lookup 'C:Query.categoryById'.
                      Unable to access the required field 'Category.id'.
                        Unable to transition between schemas 'A' and 'B' for access to field 'B:Category.id'.
                          No lookups found for type 'Category' in schema 'B'.
                Unable to access the required field 'Section.name'.
                  Unable to transition between schemas 'A' and 'C' for access to field 'C:Section.name'.
                    Unable to satisfy the requirement 'id' for lookup 'C:Query.sectionById'.
                      Unable to access the required field 'Section.id'.
                        Unable to transition between schemas 'A' and 'B' for access to field 'B:Section.id'.
                          No lookups found for type 'Section' in schema 'B'.
            """,
            string.Join("\n\n", log.Select(e => e.Message)));
    }

    // See Q at https://chillicream-hq.slack.com/archives/C08269A5YV6/p1745308341040919.
    [Fact]
    public void CycleInRequireDirectives()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    name: String!
                    sku(description: String @require(field: "description")): String
                }
                """,
                """
                # Schema B
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    description(sku: String @require(field: "sku")): String
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            """
            Unable to access the field 'Product.sku' on path 'A:Query.productById'.
              Unable to satisfy the requirement 'description' on field 'A:Product.sku'.
                Unable to access the required field 'Product.description'.
                  Unable to satisfy the requirement 'sku' on field 'B:Product.description'.
                    Unable to access the required field 'Product.sku'.
                      Unable to satisfy the requirement 'description' on field 'A:Product.sku'.
                        Unable to access the required field 'Product.description'.
                          Cycle detected: B:Product.description -> A:Product.sku -> B:Product.description.

            Unable to access the field 'Product.description' on path 'A:Query.productById'.
              Unable to satisfy the requirement 'sku' on field 'B:Product.description'.
                Unable to access the required field 'Product.sku'.
                  Unable to satisfy the requirement 'description' on field 'A:Product.sku'.
                    Unable to access the required field 'Product.description'.
                      Unable to satisfy the requirement 'sku' on field 'B:Product.description'.
                        Unable to access the required field 'Product.sku'.
                          Cycle detected: A:Product.sku -> B:Product.description -> A:Product.sku.

            Unable to access the field 'Product.sku' on path 'B:Query.productById'.
              Unable to satisfy the requirement 'description' on field 'A:Product.sku'.
                Unable to access the required field 'Product.description'.
                  Unable to satisfy the requirement 'sku' on field 'B:Product.description'.
                    Unable to access the required field 'Product.sku'.
                      Unable to satisfy the requirement 'description' on field 'A:Product.sku'.
                        Unable to access the required field 'Product.description'.
                          Cycle detected: B:Product.description -> A:Product.sku -> B:Product.description.

            Unable to access the field 'Product.description' on path 'B:Query.productById'.
              Unable to satisfy the requirement 'sku' on field 'B:Product.description'.
                Unable to access the required field 'Product.sku'.
                  Unable to satisfy the requirement 'description' on field 'A:Product.sku'.
                    Unable to access the required field 'Product.description'.
                      Unable to satisfy the requirement 'sku' on field 'B:Product.description'.
                        Unable to access the required field 'Product.sku'.
                          Cycle detected: A:Product.sku -> B:Product.description -> A:Product.sku.
            """,
            string.Join("\n\n", log.Select(e => e.Message)));
    }

    // Not working ... starting from schema A and getting specialField should work, but I don't yet
    // keep the context of having come via schema A and therefore having had access to the "id"
    // field earlier.
    [Fact]
    public void X()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    keyField1: Int!
                }
                """,
                """
                # Schema C
                type Query {
                    productByKeyField1(keyField1: Int!): Product @lookup
                }

                type Product {
                    keyField2: Int!
                }
                """,
                """
                # Schema D
                type Query {
                    productByKey(keyField1: Int!, keyField2: Int!): Product @lookup
                }

                type Product {
                    keyField1: Int!
                    keyField2: Int!
                    specialField: Int!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            """
            .
            """,
            string.Join("\n\n", log.Select(e => e.Message)));
    }
}
