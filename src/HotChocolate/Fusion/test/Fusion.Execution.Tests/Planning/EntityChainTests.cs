using System.Text;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class EntityChainTests : FusionTestBase
{
    [Fact]
    public void Complex_Entity_Call()
    {
        // arrange
        var schema = CreateComplexEntityCallSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              topProducts {
                products {
                  id
                  price {
                    price
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Complex_Entity_Call_Nested_List_Key_Depends_On_Producing_Hop()
    {
        // arrange
        // The 'list' subgraph owns 'selected' and identifies ProductList through the
        // nested-list key products{id pid}. 'id' comes from the 'products' root fetch,
        // but 'pid' is buried in the list sub-selection and is produced by a separate
        // 'link' hop. The lookup that consumes the key must depend on that pid hop.
        var schema = CreateComplexEntityCallWithListSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              topProducts {
                products {
                  id
                }
                selected {
                  id
                }
              }
            }
            """);

        // assert
        // node 2 (the 'list' ProductList lookup) depends on BOTH node 1 (products.id)
        // and node 3 (the 'link' hop that produces the buried products.pid leaf).
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  {
                    topProducts {
                      products {
                        id
                      }
                      products @fusion__requirement {
                        id @fusion__requirement
                        pid @fusion__requirement
                      }
                      selected {
                        id
                      }
                    }
                  }
                hash: 123456789101112
                searchSpace: 1
                expandedNodes: 2
            nodes:
              - id: 1
                type: OperationBatch
                schema: products
                operation: |
                  query Op_123456789101112_1 {
                    topProducts {
                      products {
                        id
                      }
                    }
                  }
                targets:
                  - $
                  - $
                batchingGroupId: 1
              - id: 2
                type: Operation
                schema: list
                operation: |
                  query Op_123456789101112_2($__fusion_1_key: ProductListInput!) {
                    productListByProducts(key: $__fusion_1_key) {
                      selected {
                        id
                      }
                    }
                  }
                source: $.productListByProducts
                target: $.topProducts
                requirements:
                  - name: __fusion_1_key
                    selectionMap: >-
                      {
                        products: products[{
                          id
                          pid
                        }]
                      }
                dependencies:
                  - id: 1
                  - id: 3
              - id: 3
                type: Operation
                schema: link
                operation: |
                  query Op_123456789101112_3($__fusion_2_id: ID!) {
                    productById(id: $__fusion_2_id) {
                      pid
                    }
                  }
                source: $.productById
                target: $.topProducts.products
                requirements:
                  - name: __fusion_2_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }

    // A nested-list '@key' (products{id pid}) resolves through a lookup whose '@is' map
    // reads a list field. The Composite Schema Spec disallows list-typed '@key' directives
    // at the type level, so key inference from lookups is disabled per source schema, mirroring
    // how the Apollo Federation connector composes these subgraphs. Composing directly (rather
    // than via ComposeSchema) keeps that option local to this fixture.
    private static FusionSchemaDefinition CreateComplexEntityCallWithListSchema()
    {
        var sources = new List<SourceSchemaText>
        {
            new("products", """
                type Query { topProducts: ProductList! }
                type ProductList { products: [Product!]! @shareable }
                type Product @key(fields: "id") { id: ID! }
                """),
            new("link", """
                type Query { productById(id: ID! @is(field: "id")): Product @lookup @internal }
                type Product @key(fields: "id") { id: ID! pid: ID! @shareable }
                """),
            new("list", """
                type Query {
                  productListByProducts(
                    key: ProductListInput! @is(field: "{ products: products[{ id pid }] }")
                  ): ProductList @lookup @internal
                }
                type ProductList { products: [Product!]! @shareable selected: Product @shareable }
                type Product @key(fields: "id pid") { id: ID! pid: ID! @shareable }
                input ProductListInput { products: [ProductListInput_Products!]! }
                input ProductListInput_Products { id: ID! pid: ID! }
                """)
        };

        var options = new SchemaComposerOptions();
        foreach (var source in sources)
        {
            options.SourceSchemas[source.Name] = new SourceSchemaOptions
            {
                Preprocessor = new SourceSchemaPreprocessorOptions
                {
                    InferKeysFromLookups = false
                }
            };
        }

        var log = new CompositionLog();
        var composer = new SchemaComposer(sources, options, log);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            var errors = new StringBuilder();
            foreach (var entry in log)
            {
                errors.AppendLine($"[{entry.Severity}] {entry.Code}: {entry.Message}");
            }

            throw new InvalidOperationException(errors.ToString());
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }

    [Fact]
    public void Parent_Entity_Call_Complex()
    {
        // arrange
        var schema = CreateParentEntityCallComplexSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              productFromD(id: "1") {
                id
                name
                category {
                  id
                  name
                  details
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateComplexEntityCallSchema()
    {
        return ComposeSchema(
            """
            # name: products
            schema {
              query: Query
            }

            type Query {
              topProducts: ProductList!
            }

            type ProductList {
              products: [Product!]!
            }

            type Product @key(fields: "id") {
              id: ID!
              category: Category! @shareable
            }

            type Category {
              id: ID! @shareable
              tag: String @shareable
            }
            """,
            """
            # name: link
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              pid: ID! @shareable
            }
            """,
            """
            # name: price
            schema {
              query: Query
            }

            type Query {
              productByIdPidAndCategory(
                id: ID! @is(field: "id")
                pid: ID! @is(field: "pid")
                categoryId: ID! @is(field: "category.id")
                categoryTag: String @is(field: "category.tag")): Product @lookup @internal
            }

            type Product @key(fields: "id pid category { id tag }") {
              id: ID!
              pid: ID! @shareable
              category: Category! @shareable
              price: Price
            }

            type Category {
              id: ID! @shareable
              tag: String @shareable
            }

            type Price {
              price: Float!
            }
            """);
    }

    private static FusionSchemaDefinition CreateParentEntityCallComplexSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              category: Category @shareable
            }

            type Category {
              details: String
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              category: Category @shareable
            }

            type Category @key(fields: "id") {
              id: ID!
            }
            """,
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              categoryById(id: ID! @is(field: "id")): Category @lookup @internal
            }

            type Category @key(fields: "id") {
              id: ID!
              name: String
            }
            """,
            """
            # name: d
            schema {
              query: Query
            }

            type Query {
              productFromD(id: ID!): Product
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }
            """);
    }
}
