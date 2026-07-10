using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Planning.Partitioners;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SupertypeNarrowingPlanningTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_Preserve_Interface_And_Union_Supertype_Directives_When_Parent_Is_Concrete_Object()
    {
        // arrange
        var schema = ComposeSchema(
            """
            # name: A
            type Query {
              book: Book
              publications: [Publication]
            }

            interface Media { id: ID! }
            union Publication = Book | Magazine

            type Book implements Media {
              id: ID!
              title: String
            }

            type Magazine implements Media {
              id: ID!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            query($includeMedia: Boolean!, $skipPublication: Boolean!) {
              book {
                ... on Media @include(if: $includeMedia) {
                  id
                }
                ... on Publication @skip(if: $skipPublication) {
                  ... on Book {
                    title
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_SpillWholeFieldToCoveringSchema_When_UnionNarrowingCannotCoverRequestedMember()
    {
        // arrange
        var schema = CreateFeaturedItemSchema();
        AssertSourceTypeName(schema, "featured", "B", "Product");

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              featured {
                ... on Product {
                  name
                }
                ... on Review {
                  rating
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_AllowNarrowingSource_When_UnionNarrowingCoversOnlyRequestedMember()
    {
        // arrange
        var schema = CreateFeaturedItemSchemaWithProductNameOnlyOnB();
        AssertSourceTypeName(schema, "featured", "B", "Product");

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              featured {
                ... on Product {
                  name
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_SpillNestedFieldToCoveringSchema_When_UnionNarrowingCannotCoverRequestedMember()
    {
        // arrange
        var schema = CreateCategoryFeaturedItemSchema();
        AssertSourceTypeName(schema, "Category", "featured", "B", "Product");

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              category {
                featured {
                  ... on Product {
                    name
                  }
                  ... on Review {
                    rating
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_AllowNestedNarrowingSource_When_UnionNarrowingCoversOnlyRequestedMember()
    {
        // arrange
        var schema = CreateCategoryFeaturedItemSchemaWithProductNameOnlyOnB();
        AssertSourceTypeName(schema, "Category", "featured", "B", "Product");

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              category {
                featured {
                  ... on Product {
                    name
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_SpillRootFieldToCoveringSchema_When_NarrowedFieldHasRequirementsAndFragmentIsUncovered()
    {
        // arrange
        var schema = CreateFeaturedItemSchemaWithRequirements();
        AssertSourceTypeName(schema, "featured", "B", "Product");

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              featured {
                ... on Product {
                  name
                }
                ... on Review {
                  rating
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_SpillNestedFieldToCoveringSchema_When_NarrowedFieldHasRequirementsAndFragmentIsUncovered()
    {
        // arrange
        var schema = CreateCategoryFeaturedItemSchemaWithRequirements();
        AssertSourceTypeName(schema, "Category", "featured", "B", "Product");

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              category {
                featured {
                  ... on Product {
                    name
                  }
                  ... on Review {
                    rating
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_AllowNarrowingSource_When_InterfaceFragmentsAreCoveredBySourceObject()
    {
        // arrange
        var schema = CreateInterfaceFragmentSchema(sourceImplementsAnimal: true);
        AssertSourceTypeName(schema, "a", "S", "B");

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              a {
                ... on Pet {
                  petField
                }
                ... on Animal {
                  animalField
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_SpillWholeFieldToCoveringSchema_When_InterfaceFragmentIsNotCoveredBySourceObject()
    {
        // arrange
        var schema = CreateInterfaceFragmentSchema(sourceImplementsAnimal: false);
        AssertSourceTypeName(schema, "a", "S", "B");

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              a {
                ... on Pet {
                  petField
                }
                ... on Animal {
                  animalField
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Partition_Should_ThrowNotSupportedException_When_SourceNarrowsToAbstractType()
    {
        // arrange
        var schema = CreateAbstractNarrowingSchema();
        AssertSourceTypeName(schema, "featured", "B", "ProductItem");
        var document = Utf8GraphQLParser.Parse(
            """
            query {
              featured {
                ... on Product {
                  name
                }
              }
            }
            """);

        var rewriter = new DocumentRewriter(schema);
        var operation = rewriter.RewriteDocument(document).Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
        var index = SelectionSetIndexer.Create(operation);
        var partitioner = new SelectionSetPartitioner(schema);

        // act
        var error = Assert.Throws<NotSupportedException>(
            () => partitioner.Partition(
                new SelectionSetPartitionerInput
                {
                    SchemaName = "B",
                    SelectionSet = new SelectionSet(
                        index.GetId(operation.SelectionSet),
                        operation.SelectionSet,
                        schema.QueryType,
                        SelectionPath.Root),
                    SelectionSetIndex = index
                }));

        // assert
        Assert.Contains("Query.featured", error.Message);
        Assert.Contains("B", error.Message);
        Assert.Contains("ProductItem", error.Message);
    }

    [Fact]
    public void Plan_Should_SpillWholeFieldToCoveringSchema_When_UncoveredFragmentHasConditionalDirective()
    {
        // arrange
        var schema = CreateFeaturedItemSchema();
        AssertSourceTypeName(schema, "featured", "B", "Product");

        // act
        var plan = PlanOperation(
            schema,
            """
            query($includeProduct: Boolean!, $includeReview: Boolean!) {
              featured {
                ... on Product @include(if: $includeProduct) {
                  name
                }
                ... on Review @include(if: $includeReview) {
                  rating
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_SpillWholeFieldToCoveringSchema_When_UncoveredFragmentIsNestedInConditionlessFragment()
    {
        // arrange
        var schema = CreateFeaturedItemSchema();
        AssertSourceTypeName(schema, "featured", "B", "Product");

        // act
        var plan = PlanOperation(
            schema,
            """
            query($includeReview: Boolean!) {
              featured {
                ... on Product {
                  name
                }
                ... @include(if: $includeReview) {
                  ... on Review {
                    rating
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Partition_Should_SpillNarrowedFieldBeforeEnqueuingRequirements_When_FragmentIsUncovered()
    {
        // arrange
        var schema = CreateExecutionSchema(
            """
            type Query
              @fusion__type(schema: A)
              @fusion__type(schema: B) {
              id: ID @fusion__field(schema: A)
              featured: FeaturedItem
                @fusion__field(schema: A)
                @fusion__field(schema: B, sourceType: "Product!")
                @fusion__requires(
                  schema: B
                  requirements: "id"
                  field: "featured(id: ID!): Product"
                  map: ["id"]
                )
            }

            type Product
              @fusion__type(schema: A)
              @fusion__type(schema: B) {
              name: String
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }

            type Review
              @fusion__type(schema: A) {
              rating: Int @fusion__field(schema: A)
            }

            union FeaturedItem
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Product")
              @fusion__unionMember(schema: A, member: "Review") = Product | Review
            """);

        var document = Utf8GraphQLParser.Parse(
            """
            query {
              featured {
                ... on Product {
                  name
                }
                ... on Review {
                  rating
                }
              }
            }
            """);

        var rewriter = new DocumentRewriter(schema);
        var operation = rewriter.RewriteDocument(document).Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
        var featuredNode = Assert.IsType<FieldNode>(Assert.Single(operation.SelectionSet.Selections));
        var typeConditions = featuredNode.SelectionSet!.Selections
            .OfType<InlineFragmentNode>()
            .Select(t => t.TypeCondition?.Name.Value)
            .ToArray();
        var index = SelectionSetIndexer.Create(operation);
        var partitioner = new SelectionSetPartitioner(schema);
        Assert.Contains("Review", typeConditions);

        // act
        var (resolvable, unresolvable, fieldsWithRequirements, _) = partitioner.Partition(
            new SelectionSetPartitionerInput
            {
                SchemaName = "B",
                SelectionSet = new SelectionSet(
                    index.GetId(operation.SelectionSet),
                    operation.SelectionSet,
                    schema.QueryType,
                    SelectionPath.Root),
                SelectionSetIndex = index
            });

        // assert
        Assert.Empty(resolvable?.Selections ?? []);
        Assert.False(unresolvable.IsEmpty);
        Assert.True(fieldsWithRequirements.IsEmpty);
    }

    [Fact]
    public void Partition_Should_EnqueueRequirements_When_RootNarrowedFieldCoversRequestedFragments()
    {
        // arrange
        var schema = CreateFeaturedItemSchemaWithRequirementsAndProductNameOnlyOnB();

        // act
        var (_, _, fieldsWithRequirements, _) = PartitionSchemaB(
            schema,
            """
            query {
              featured {
                ... on Product {
                  name
                }
              }
            }
            """);

        // assert
        var fieldWithRequirements = Assert.Single(fieldsWithRequirements);
        Assert.Equal("featured", fieldWithRequirements.FieldSelection.Node.Name.Value);
        Assert.Equal(SelectionPath.Root, fieldWithRequirements.FieldSelection.Path);
    }

    [Fact]
    public void Partition_Should_SpillNestedNarrowedFieldBeforeEnqueuingRequirements_When_FragmentIsUncovered()
    {
        // arrange
        var schema = CreateCategoryFeaturedItemSchemaWithRequirements();

        // act
        var (_, unresolvable, fieldsWithRequirements, _) = PartitionSchemaB(
            schema,
            """
            query {
              category {
                featured {
                  ... on Product {
                    name
                  }
                  ... on Review {
                    rating
                  }
                }
              }
            }
            """);

        // assert
        Assert.False(unresolvable.IsEmpty);
        Assert.True(fieldsWithRequirements.IsEmpty);
    }

    [Fact]
    public void Partition_Should_EnqueueRequirements_When_NestedNarrowedFieldCoversRequestedFragments()
    {
        // arrange
        var schema = CreateCategoryFeaturedItemSchemaWithRequirementsAndProductNameOnlyOnB();

        // act
        var (_, _, fieldsWithRequirements, _) = PartitionSchemaB(
            schema,
            """
            query {
              category {
                featured {
                  ... on Product {
                    name
                  }
                }
              }
            }
            """);

        // assert
        var fieldWithRequirements = Assert.Single(fieldsWithRequirements);
        Assert.Equal("featured", fieldWithRequirements.FieldSelection.Node.Name.Value);
        Assert.Equal(
            SelectionPath.Root.AppendField("category"),
            fieldWithRequirements.FieldSelection.Path);
    }

    [Fact]
    public void Partition_Should_Expand_InterfaceFragment_When_SourceMembershipIsNarrower()
    {
        // arrange
        var schema = CreateDistributedInterfaceMembershipSchema();

        // act
        var (resolvable, unresolvable, _, _) = PartitionSchemaA(
            schema,
            """
            query {
              products {
                ... on Node {
                  id
                }
              }
            }
            """);

        // assert
        resolvable.MatchInlineSnapshot(
            """
            {
              products {
                __typename
                ... {
                  ... on Oven {
                    id
                  }
                  ... on Toaster {
                    id
                  }
                }
              }
            }
            """);
        Assert.True(unresolvable.IsEmpty);
    }

    [Fact]
    public void Partition_Should_RouteNestedInterfaceFragment_When_ParentWasExpandedToConcreteTypes()
    {
        // arrange
        var schema = CreateDistributedInterfaceMembershipSchema();

        // act
        var (resolvable, unresolvable, _, _) = PartitionSchemaA(
            schema,
            """
            query {
              products {
                ... on Node {
                  ... on WithWarranty {
                    warranty
                  }
                }
              }
            }
            """);

        // assert
        resolvable.MatchInlineSnapshot(
            """
            {
              products {
                __typename
                ... {
                  ... on Oven {
                    ... on Oven {
                      __typename
                    }
                  }
                  ... on Toaster {
                    ... on Toaster {
                      warranty
                    }
                  }
                }
              }
            }
            """);
        var unresolved = Assert.Single(unresolvable);
        Assert.Equal("Oven", unresolved.SelectionSet.Type.Name);
        var ovenWarranty = schema.Types.GetType<FusionObjectTypeDefinition>("Oven")
            .Fields.GetField("warranty", allowInaccessibleFields: true);
        Assert.False(ovenWarranty.Sources.TryGetMember("A", out _));
        Assert.True(ovenWarranty.Sources.TryGetMember("B", out _));
        unresolved.SelectionSet.Node.MatchInlineSnapshot(
            """
            {
              warranty
            }
            """);
    }

    [Fact]
    public void Partition_Should_CloneNestedSelectionSets_When_ParentExpandsToConcreteTypes()
    {
        // arrange
        var schema = CreateDistributedInterfaceMembershipSchema();

        // act
        var (_, unresolvable, _, index) = PartitionSchemaA(
            schema,
            """
            query {
              products {
                ... on Node {
                  details {
                    __typename
                    warranty
                  }
                }
              }
            }
            """);

        // assert
        var unresolved = unresolvable.ToArray();
        Assert.Equal(2, unresolved.Length);
        Assert.All(unresolved, entry => Assert.Equal("Details", entry.SelectionSet.Type.Name));

        var firstId = unresolved[0].SelectionSet.Id;
        var secondId = unresolved[1].SelectionSet.Id;
        Assert.NotEqual(firstId, secondId);
        Assert.True(index.TryGetOriginalIdFromCloned(firstId, out var firstOriginalId));
        Assert.True(index.TryGetOriginalIdFromCloned(secondId, out var secondOriginalId));
        Assert.Equal(firstOriginalId, secondOriginalId);

        Assert.All(
            unresolved,
            entry => entry.SelectionSet.Node.MatchInlineSnapshot(
                """
                {
                  warranty
                }
                """));
    }

    [Fact]
    public void Plan_Should_FetchNestedInterfaceField_When_SyntheticBranchIsOwnedByOtherSource()
    {
        // arrange
        var schema = CreateDistributedInterfaceMembershipSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              products {
                ... on Node {
                  ... on WithWarranty {
                    warranty
                  }
                }
              }
            }
            """);

        // assert
        MatchInline(
            plan,
            """
            operation:
              - document: |
                  {
                    products {
                      __typename @fusion__requirement
                      ... on Node {
                        ... on WithWarranty {
                          warranty
                          id @fusion__requirement
                        }
                      }
                    }
                  }
                hash: 123456789101112
                searchSpace: 1
                expandedNodes: 2
            nodes:
              - id: 1
                type: Operation
                schema: A
                operation: |
                  query Op_123456789101112_1 {
                    products {
                      __typename
                      ... {
                        ... on Oven {
                          ... on Oven {
                            __typename
                            id
                          }
                        }
                        ... on Toaster {
                          ... on Toaster {
                            warranty
                          }
                        }
                      }
                    }
                  }
              - id: 2
                type: Operation
                schema: B
                operation: |
                  query Op_123456789101112_2($__fusion_1_id: ID!) {
                    ovenById(id: $__fusion_1_id) {
                      warranty
                    }
                  }
                source: $.ovenById
                target: $.products<Node><Oven><WithWarranty><Oven>
                requirements:
                  - name: __fusion_1_id
                    selectionMap: >-
                      id
                dependencies:
                  - id: 1
            """);
    }

    [Fact]
    public void Partition_Should_NotExpandInterfaceFragment_When_SourceFieldNarrowsRuntimeType()
    {
        // arrange
        var schema = CreateNarrowedDistributedInterfaceMembershipSchema();
        AssertSourceTypeName(schema, "item", "S", "B");

        // act
        var (resolvable, unresolvable, _, _) = PartitionSchema(
            schema,
            """
            query {
              item {
                ... on Node {
                  id
                }
              }
            }
            """,
            "S");

        // assert
        resolvable.MatchInlineSnapshot(
            """
            {
              item {
                __typename
                ... on Node {
                  __typename
                  id
                }
              }
            }
            """);
        Assert.True(unresolvable.IsEmpty);
    }

    [Fact]
    public void Partition_Should_PruneConcreteFragment_When_SourceParentCannotProduceType()
    {
        // arrange
        var schema = CreateDistributedInterfaceMembershipSchema();

        // act
        var (resolvable, unresolvable, _, _) = PartitionSchemaA(
            schema,
            """
            query {
              nodes {
                ... on Toaster {
                  warranty
                }
                ... on Oven {
                  id
                }
              }
            }
            """);

        // assert
        resolvable.MatchInlineSnapshot(
            """
            {
              nodes {
                __typename
                ... on Toaster {
                  warranty
                }
              }
            }
            """);
        Assert.True(unresolvable.IsEmpty);
    }

    private static FusionSchemaDefinition CreateFeaturedItemSchema()
        => CreateExecutionSchema(
            """
            type Query
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              featured: FeaturedItem
                @fusion__field(schema: B, sourceType: "Product!")
                @fusion__field(schema: A)
            }

            type Product
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: B)
                @fusion__field(schema: A)
              name: String!
                @fusion__field(schema: B)
                @fusion__field(schema: A)
            }

            type Review
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: A)
              rating: Int!
                @fusion__field(schema: A)
            }

            union FeaturedItem
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Product")
              @fusion__unionMember(schema: A, member: "Review") = Product | Review
            """);

    private static FusionSchemaDefinition CreateFeaturedItemSchemaWithRequirements()
        => CreateExecutionSchema(
            """
            type Query
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              id: ID
                @fusion__field(schema: B)
                @fusion__field(schema: A)
              featured: FeaturedItem
                @fusion__field(schema: B, sourceType: "Product!")
                @fusion__field(schema: A)
                @fusion__requires(
                  schema: B
                  requirements: "id"
                  field: "featured(id: ID!): Product"
                  map: ["id"]
                )
            }

            type Product
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: B)
                @fusion__field(schema: A)
              name: String!
                @fusion__field(schema: B)
                @fusion__field(schema: A)
            }

            type Review
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: A)
              rating: Int!
                @fusion__field(schema: A)
            }

            union FeaturedItem
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Product")
              @fusion__unionMember(schema: A, member: "Review") = Product | Review
            """);

    private static FusionSchemaDefinition CreateFeaturedItemSchemaWithProductNameOnlyOnB()
        => CreateExecutionSchema(
            """
            type Query
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              featured: FeaturedItem
                @fusion__field(schema: B, sourceType: "Product!")
                @fusion__field(schema: A)
            }

            type Product
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: B)
                @fusion__field(schema: A)
              name: String!
                @fusion__field(schema: B)
            }

            type Review
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: A)
              rating: Int!
                @fusion__field(schema: A)
            }

            union FeaturedItem
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Product")
              @fusion__unionMember(schema: A, member: "Review") = Product | Review
            """);

    private static FusionSchemaDefinition CreateFeaturedItemSchemaWithRequirementsAndProductNameOnlyOnB()
        => CreateExecutionSchema(
            """
            type Query
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              id: ID
                @fusion__field(schema: B)
                @fusion__field(schema: A)
              featured: FeaturedItem
                @fusion__field(schema: B, sourceType: "Product!")
                @fusion__field(schema: A)
                @fusion__requires(
                  schema: B
                  requirements: "id"
                  field: "featured(id: ID!): Product"
                  map: ["id"]
                )
            }

            type Product
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: B)
                @fusion__field(schema: A)
              name: String!
                @fusion__field(schema: B)
            }

            type Review
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: A)
              rating: Int!
                @fusion__field(schema: A)
            }

            union FeaturedItem
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Product")
              @fusion__unionMember(schema: A, member: "Review") = Product | Review
            """);

    private static FusionSchemaDefinition CreateCategoryFeaturedItemSchema()
        => CreateCategoryFeaturedItemSchema(requiresFeatured: false, productNameOnlyOnB: false);

    private static FusionSchemaDefinition CreateCategoryFeaturedItemSchemaWithProductNameOnlyOnB()
        => CreateCategoryFeaturedItemSchema(requiresFeatured: false, productNameOnlyOnB: true);

    private static FusionSchemaDefinition CreateCategoryFeaturedItemSchemaWithRequirements()
        => CreateCategoryFeaturedItemSchema(requiresFeatured: true, productNameOnlyOnB: false);

    private static FusionSchemaDefinition CreateCategoryFeaturedItemSchemaWithRequirementsAndProductNameOnlyOnB()
        => CreateCategoryFeaturedItemSchema(requiresFeatured: true, productNameOnlyOnB: true);

    private static FusionSchemaDefinition CreateCategoryFeaturedItemSchema(
        bool requiresFeatured,
        bool productNameOnlyOnB)
        => CreateExecutionSchema(
            $$"""
            type Query
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              category: Category
                @fusion__field(schema: B)
                @fusion__field(schema: A)
            }

            type Category
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              id: ID
                @fusion__field(schema: B)
                @fusion__field(schema: A)
              featured: FeaturedItem
                @fusion__field(schema: B, sourceType: "Product!")
                @fusion__field(schema: A)
                {{CreateRequiresDirective(requiresFeatured)}}
            }

            type Product
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: B)
                @fusion__field(schema: A)
              name: String!
                @fusion__field(schema: B)
                {{CreateProductNameField(productNameOnlyOnB)}}
            }

            type Review
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: A)
              rating: Int!
                @fusion__field(schema: A)
            }

            union FeaturedItem
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Product")
              @fusion__unionMember(schema: A, member: "Review") = Product | Review
            """);

    private static string CreateRequiresDirective(bool enabled)
        => enabled
            ? """
              @fusion__requires(
                schema: B
                requirements: "id"
                field: "featured(id: ID!): Product"
                map: ["id"]
              )
            """
            : string.Empty;

    private static string CreateProductNameField(bool productNameOnlyOnB)
        => productNameOnlyOnB
            ? string.Empty
            : "@fusion__field(schema: A)";

    private static FusionSchemaDefinition CreateInterfaceFragmentSchema(bool sourceImplementsAnimal)
        => sourceImplementsAnimal
            ? CreateExecutionSchema(
                """
                type Query
                  @fusion__type(schema: S)
                  @fusion__type(schema: A) {
                  a: AB
                    @fusion__field(schema: S, sourceType: "B!")
                    @fusion__field(schema: A)
                }

                union AB
                  @fusion__type(schema: A)
                  @fusion__unionMember(schema: A, member: "B")
                  @fusion__unionMember(schema: A, member: "C") = B | C

                interface Pet
                  @fusion__type(schema: S)
                  @fusion__type(schema: A) {
                  petField: String!
                    @fusion__field(schema: S)
                    @fusion__field(schema: A)
                }

                interface Animal
                  @fusion__type(schema: S)
                  @fusion__type(schema: A) {
                  animalField: String!
                    @fusion__field(schema: S)
                    @fusion__field(schema: A)
                }

                type B implements Pet & Animal
                  @fusion__type(schema: S)
                  @fusion__type(schema: A)
                  @fusion__implements(schema: S, interface: "Pet")
                  @fusion__implements(schema: S, interface: "Animal")
                  @fusion__implements(schema: A, interface: "Pet")
                  @fusion__implements(schema: A, interface: "Animal") {
                  id: ID!
                    @fusion__field(schema: S)
                    @fusion__field(schema: A)
                  petField: String!
                    @fusion__field(schema: S)
                    @fusion__field(schema: A)
                  animalField: String!
                    @fusion__field(schema: S)
                    @fusion__field(schema: A)
                }

                type C implements Pet
                  @fusion__type(schema: A)
                  @fusion__implements(schema: A, interface: "Pet") {
                  id: ID!
                    @fusion__field(schema: A)
                  petField: String!
                    @fusion__field(schema: A)
                }
                """)
            : CreateExecutionSchema(
                """
                type Query
                  @fusion__type(schema: S)
                  @fusion__type(schema: A) {
                  a: AB
                    @fusion__field(schema: S, sourceType: "B!")
                    @fusion__field(schema: A)
                }

                union AB
                  @fusion__type(schema: A)
                  @fusion__unionMember(schema: A, member: "B")
                  @fusion__unionMember(schema: A, member: "C") = B | C

                interface Pet
                  @fusion__type(schema: S)
                  @fusion__type(schema: A) {
                  petField: String!
                    @fusion__field(schema: S)
                    @fusion__field(schema: A)
                }

                interface Animal
                  @fusion__type(schema: A) {
                  animalField: String!
                    @fusion__field(schema: A)
                }

                type B implements Pet & Animal
                  @fusion__type(schema: S)
                  @fusion__type(schema: A)
                  @fusion__implements(schema: S, interface: "Pet")
                  @fusion__implements(schema: A, interface: "Pet")
                  @fusion__implements(schema: A, interface: "Animal") {
                  id: ID!
                    @fusion__field(schema: S)
                    @fusion__field(schema: A)
                  petField: String!
                    @fusion__field(schema: S)
                    @fusion__field(schema: A)
                  animalField: String!
                    @fusion__field(schema: A)
                }

                type C implements Pet
                  @fusion__type(schema: A)
                  @fusion__implements(schema: A, interface: "Pet") {
                  id: ID!
                    @fusion__field(schema: A)
                  petField: String!
                    @fusion__field(schema: A)
                }
                """);

    private static FusionSchemaDefinition CreateAbstractNarrowingSchema()
        => CreateExecutionSchema(
            """
            type Query
              @fusion__type(schema: B) {
              featured: FeaturedItem
                @fusion__field(schema: B, sourceType: "ProductItem!")
            }

            type Product
              @fusion__type(schema: B)
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: B)
                @fusion__field(schema: A)
              name: String!
                @fusion__field(schema: B)
                @fusion__field(schema: A)
            }

            type Review
              @fusion__type(schema: A) {
              id: ID!
                @fusion__field(schema: A)
              rating: Int!
                @fusion__field(schema: A)
            }

            union FeaturedItem
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Product")
              @fusion__unionMember(schema: A, member: "Review") = Product | Review

            union ProductItem
              @fusion__type(schema: B)
              @fusion__unionMember(schema: B, member: "Product") = Product
            """);

    private static FusionSchemaDefinition CreateDistributedInterfaceMembershipSchema()
        => CreateExecutionSchema(
            """
            type Query
              @fusion__type(schema: A) {
              products: [Product]
                @fusion__field(schema: A)
              nodes: [Node]
                @fusion__field(schema: A)
            }

            union Product
              @fusion__type(schema: A)
              @fusion__unionMember(schema: A, member: "Oven")
              @fusion__unionMember(schema: A, member: "Toaster") = Oven | Toaster

            interface Node
              @fusion__type(schema: A)
              @fusion__type(schema: B) {
              id: ID!
                @fusion__field(schema: A)
                @fusion__field(schema: B)
              details: Details
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }

            interface WithWarranty
              @fusion__type(schema: B) {
              warranty: Int
                @fusion__field(schema: B)
            }

            type Details
              @fusion__type(schema: A)
              @fusion__type(schema: B) {
              warranty: Int
                @fusion__field(schema: B)
            }

            type Oven implements Node & WithWarranty
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__implements(schema: B, interface: "Node")
              @fusion__implements(schema: B, interface: "WithWarranty")
              @fusion__lookup(
                schema: B
                key: "{ id }"
                field: "ovenById(id: ID!): Oven"
                map: ["id"]
              ) {
              id: ID!
                @fusion__field(schema: A)
                @fusion__field(schema: B)
              details: Details
                @fusion__field(schema: A)
                @fusion__field(schema: B)
              warranty: Int
                @fusion__field(schema: B)
            }

            type Toaster implements Node & WithWarranty
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__implements(schema: A, interface: "Node")
              @fusion__implements(schema: B, interface: "WithWarranty") {
              id: ID!
                @fusion__field(schema: A)
                @fusion__field(schema: B)
              details: Details
                @fusion__field(schema: A)
                @fusion__field(schema: B)
              warranty: Int
                @fusion__field(schema: A)
                @fusion__field(schema: B)
            }
            """);

    private static FusionSchemaDefinition CreateNarrowedDistributedInterfaceMembershipSchema()
        => CreateExecutionSchema(
            """
            type Query
              @fusion__type(schema: S) {
              item: AB
                @fusion__field(schema: S, sourceType: "B!")
            }

            union AB
              @fusion__type(schema: S)
              @fusion__unionMember(schema: S, member: "B")
              @fusion__unionMember(schema: S, member: "C") = B | C

            interface Node
              @fusion__type(schema: S)
              @fusion__type(schema: A)
              {
              id: ID!
                @fusion__field(schema: S)
                @fusion__field(schema: A)
            }

            type B implements Node
              @fusion__type(schema: S)
              @fusion__type(schema: A)
              @fusion__implements(schema: S, interface: "Node")
              @fusion__implements(schema: A, interface: "Node") {
              id: ID!
                @fusion__field(schema: S)
                @fusion__field(schema: A)
            }

            type C implements Node
              @fusion__type(schema: S)
              @fusion__type(schema: A)
              @fusion__implements(schema: A, interface: "Node") {
              id: ID!
                @fusion__field(schema: S)
                @fusion__field(schema: A)
            }
            """);

    private static FusionSchemaDefinition CreateExecutionSchema(string schema)
        => CreateCompositeSchema(schema + FusionDefinitions);

    private static SelectionSetPartitionerResult PartitionSchemaA(
        FusionSchemaDefinition schema,
        string operationText)
        => PartitionSchema(schema, operationText, "A");

    private static SelectionSetPartitionerResult PartitionSchemaB(
        FusionSchemaDefinition schema,
        string operationText)
        => PartitionSchema(schema, operationText, "B");

    private static SelectionSetPartitionerResult PartitionSchema(
        FusionSchemaDefinition schema,
        string operationText,
        string schemaName)
    {
        var document = Utf8GraphQLParser.Parse(operationText);
        var rewriter = new DocumentRewriter(schema);
        var operation = rewriter.RewriteDocument(document).Definitions
            .OfType<OperationDefinitionNode>()
            .Single();
        var index = SelectionSetIndexer.Create(operation);
        var partitioner = new SelectionSetPartitioner(schema);

        return partitioner.Partition(
            new SelectionSetPartitionerInput
            {
                SchemaName = schemaName,
                SelectionSet = new SelectionSet(
                    index.GetId(operation.SelectionSet),
                    operation.SelectionSet,
                    schema.QueryType,
                    SelectionPath.Root),
                SelectionSetIndex = index
            });
    }

    private static void AssertSourceTypeName(
        FusionSchemaDefinition schema,
        string fieldName,
        string schemaName,
        string sourceTypeName)
        => AssertSourceTypeName(schema, "Query", fieldName, schemaName, sourceTypeName);

    private static void AssertSourceTypeName(
        FusionSchemaDefinition schema,
        string typeName,
        string fieldName,
        string schemaName,
        string sourceTypeName)
    {
        var source = schema.Types.GetType<FusionComplexTypeDefinition>(typeName)
            .Fields.GetField(fieldName, allowInaccessibleFields: true)
            .Sources[schemaName];
        Assert.Equal(sourceTypeName, source.SourceTypeName);
    }

    private const string FusionDefinitions =
        """

        enum fusion__Schema {
          A
          B
          S
        }

        scalar fusion__FieldDefinition
        scalar fusion__FieldSelectionMap
        scalar fusion__FieldSelectionSet

        directive @fusion__type(
          schema: fusion__Schema!
        ) repeatable on OBJECT | INTERFACE | UNION | ENUM | INPUT_OBJECT | SCALAR

        directive @fusion__field(
          schema: fusion__Schema!
          sourceName: String
          sourceType: String
          provides: fusion__FieldSelectionSet
          external: Boolean! = false
        ) repeatable on FIELD_DEFINITION

        directive @fusion__requires(
          schema: fusion__Schema!
          requirements: fusion__FieldSelectionSet!
          field: fusion__FieldDefinition!
          map: [fusion__FieldSelectionMap]!
        ) repeatable on FIELD_DEFINITION

        directive @fusion__lookup(
          schema: fusion__Schema!
          key: fusion__FieldSelectionSet!
          field: fusion__FieldDefinition!
          map: [fusion__FieldSelectionMap!]!
          internal: Boolean! = false
        ) repeatable on OBJECT | INTERFACE

        directive @fusion__implements(
          schema: fusion__Schema!
          interface: String!
        ) repeatable on OBJECT | INTERFACE

        directive @fusion__unionMember(
          schema: fusion__Schema!
          member: String!
        ) repeatable on UNION
        """;
}
