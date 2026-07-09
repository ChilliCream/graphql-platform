using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Planning.Partitioners;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SupertypeNarrowingPlanningTests : FusionTestBase
{
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

    private static FusionSchemaDefinition CreateExecutionSchema(string schema)
        => CreateCompositeSchema(schema + FusionDefinitions);

    private static SelectionSetPartitionerResult PartitionSchemaB(
        FusionSchemaDefinition schema,
        string operationText)
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
                SchemaName = "B",
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
