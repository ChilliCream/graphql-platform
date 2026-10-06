using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.PostMergeValidationRules;

public sealed class AuthorizationTransitiveRequirementsRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new AuthorizationTransitiveRequirementsRule();

    private const string Directives =
        """
        directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @requiresScopes(scopes: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR

        directive @policy(policies: [[String!]!]!)
            on FIELD_DEFINITION | OBJECT | INTERFACE | ENUM | SCALAR
        """;

    [Fact]
    public void Validate_Should_Succeed_When_NoMemberIsProtected()
    {
        // arrange & act & assert
        AssertValid(RequireSchemas(requiring: "", dependency: ""));
    }

    [Fact]
    public void Validate_Should_Succeed_When_DependencyIsNotProtected()
    {
        // arrange & act & assert
        AssertValid(RequireSchemas(requiring: "@authenticated", dependency: ""));
    }

    [Fact]
    public void Validate_Should_Succeed_When_RequiringFieldHasTheSameScopes()
    {
        // arrange & act & assert
        AssertValid(
            RequireSchemas(
                requiring: """@requiresScopes(scopes: [["read"]])""",
                dependency: """@requiresScopes(scopes: [["read"]])"""));
    }

    [Fact]
    public void Validate_Should_Succeed_When_RequiringFieldStrengthensTheScopes()
    {
        // arrange & act & assert
        AssertValid(
            RequireSchemas(
                requiring: """@requiresScopes(scopes: [["read", "pii"]])""",
                dependency: """@requiresScopes(scopes: [["read"]])"""));
    }

    [Fact]
    public void Validate_Should_Succeed_When_EveryAlternativeCoversSomeDependencyAlternative()
    {
        // arrange & act & assert
        AssertValid(
            RequireSchemas(
                requiring: """@requiresScopes(scopes: [["a", "x"], ["b", "y"]])""",
                dependency: """@requiresScopes(scopes: [["a"], ["b"]])"""));
    }

    [Fact]
    public void Validate_Should_Succeed_When_RequiringFieldHasTheSamePolicyAndAuthentication()
    {
        // arrange & act & assert
        AssertValid(
            RequireSchemas(
                requiring: """@authenticated @policy(policies: [["tenant"]])""",
                dependency: """@authenticated @policy(policies: [["tenant"]])"""));
    }

    [Fact]
    public void Validate_Should_Succeed_When_RequiringTypeCarriesTheDependencyRequirement()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Product @key(fields: "id") @requiresScopes(scopes: [["read"]]) {
                id: ID!
                delivery(weight: Int! @require(field: "weight")): Int
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Product @key(fields: "id") {
                id: ID!
                weight: Int @requiresScopes(scopes: [["read"]])
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_NestedSelectionsAreCovered()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Product @key(fields: "id") {
                id: ID!
                delivery(weight: Int! @require(field: "dimension.weight")): Int
                    @requiresScopes(scopes: [["read", "pii"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Product @key(fields: "id") {
                id: ID!
                dimension: Dimension @requiresScopes(scopes: [["read"]])
            }

            type Dimension {
                weight: Int @requiresScopes(scopes: [["pii"]])
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_EachFieldOfAChainCoversItsDependency()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Product @key(fields: "id") {
                id: ID!
                label(price: Int! @require(field: "price")): String
                    @requiresScopes(scopes: [["finance", "pii"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Product @key(fields: "id") {
                id: ID!
                price(cost: Int! @require(field: "cost")): Int
                    @requiresScopes(scopes: [["finance"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema C
            type Product @key(fields: "id") {
                id: ID!
                cost: Int @requiresScopes(scopes: [["finance"]])
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_LookupKeyIsCoveredByTheServedFields()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Query { products: [Product] }

            type Product @key(fields: "id") {
                id: ID! @requiresScopes(scopes: [["admin"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query { productById(id: ID!): Product @lookup }

            type Product @key(fields: "id") {
                id: ID!
                name: String @requiresScopes(scopes: [["admin"]])
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_LookupKeyIsServedByTheLookupSchemaOnly()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Query { productById(id: ID!): Product @lookup }

            type Product @key(fields: "id") {
                id: ID! @requiresScopes(scopes: [["admin"]])
                name: String
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_IsDirectiveSelectsAProtectedKeyOnTheLookupField()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Query { products: [Product] }

            type Product @key(fields: "id") {
                id: ID! @authenticated
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query { productById(key: ID! @is(field: "id")): Product @lookup }

            type Product @key(fields: "id") {
                id: ID!
                name: String @authenticated
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_RequiringFieldDeclaresNoRequirement()
    {
        // arrange & act & assert
        AssertInvalid(
            RequireSchemas(
                requiring: "",
                dependency: """@requiresScopes(scopes: [["read"]])"""),
            [
                """
                {
                  "message": "The field 'Product.delivery' depends on 'Product.weight' through 'Product.delivery(weight:)' in schema 'A', but does not declare all authorization requirements of 'Product.weight'. Not covered: @requiresScopes.",
                  "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
                  "severity": "Error",
                  "coordinate": "Product.delivery",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_RequiringFieldDeclaresDifferentScopes()
    {
        // arrange & act & assert
        AssertInvalid(
            RequireSchemas(
                requiring: """@requiresScopes(scopes: [["other"]])""",
                dependency: """@requiresScopes(scopes: [["read"]])"""),
            [
                """
                {
                  "message": "The field 'Product.delivery' depends on 'Product.weight' through 'Product.delivery(weight:)' in schema 'A', but does not declare all authorization requirements of 'Product.weight'. Not covered: @requiresScopes.",
                  "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
                  "severity": "Error",
                  "coordinate": "Product.delivery",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_OneAlternativeDoesNotCoverTheDependency()
    {
        // arrange & act & assert
        AssertInvalid(
            RequireSchemas(
                requiring: """@requiresScopes(scopes: [["a"], ["b"]])""",
                dependency: """@requiresScopes(scopes: [["a"]])"""),
            [
                """
                {
                  "message": "The field 'Product.delivery' depends on 'Product.weight' through 'Product.delivery(weight:)' in schema 'A', but does not declare all authorization requirements of 'Product.weight'. Not covered: @requiresScopes.",
                  "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
                  "severity": "Error",
                  "coordinate": "Product.delivery",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_DependencyIsAuthenticatedAndRequiringFieldIsNot()
    {
        // arrange & act & assert
        AssertInvalid(
            RequireSchemas(
                requiring: """@requiresScopes(scopes: [["read"]])""",
                dependency: """@authenticated @requiresScopes(scopes: [["read"]])"""),
            [
                """
                {
                  "message": "The field 'Product.delivery' depends on 'Product.weight' through 'Product.delivery(weight:)' in schema 'A', but does not declare all authorization requirements of 'Product.weight'. Not covered: @authenticated.",
                  "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
                  "severity": "Error",
                  "coordinate": "Product.delivery",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_PolicyIsNotCovered()
    {
        // arrange & act & assert
        AssertInvalid(
            RequireSchemas(
                requiring: """@authenticated @policy(policies: [["tenant"]])""",
                dependency: """@authenticated @policy(policies: [["tenant", "audit"]])"""),
            [
                """
                {
                  "message": "The field 'Product.delivery' depends on 'Product.weight' through 'Product.delivery(weight:)' in schema 'A', but does not declare all authorization requirements of 'Product.weight'. Not covered: @policy.",
                  "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
                  "severity": "Error",
                  "coordinate": "Product.delivery",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_ListEveryUncoveredDirective_When_NothingIsCovered()
    {
        // arrange & act & assert
        AssertInvalid(
            RequireSchemas(
                requiring: "",
                dependency:
                    """
                    @authenticated
                    @requiresScopes(scopes: [["read"]])
                    @policy(policies: [["tenant"]])
                    """),
            [
                """
                {
                  "message": "The field 'Product.delivery' depends on 'Product.weight' through 'Product.delivery(weight:)' in schema 'A', but does not declare all authorization requirements of 'Product.weight'. Not covered: @authenticated, @requiresScopes, @policy.",
                  "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
                  "severity": "Error",
                  "coordinate": "Product.delivery",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_DependencyReturnTypeRequirementIsFlattenedOntoTheField()
    {
        // arrange & act & assert
        AssertInvalid(
        [
            $$"""
            # Schema A
            type Product @key(fields: "id") {
                id: ID!
                delivery(weight: Int! @require(field: "dimension.weight")): Int
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Product @key(fields: "id") {
                id: ID!
                dimension: Dimension
            }

            type Dimension @authenticated {
                weight: Int
            }

            {{Directives}}
            """
        ],
        [
            """
            {
              "message": "The field 'Product.delivery' depends on 'Dimension.weight' through 'Product.delivery(weight:)' in schema 'A', but does not declare all authorization requirements of 'Dimension.weight'. Not covered: @authenticated.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Product.delivery",
              "schema": "A",
              "extensions": {}
            }
            """,
            """
            {
              "message": "The field 'Product.delivery' depends on 'Product.dimension' through 'Product.delivery(weight:)' in schema 'A', but does not declare all authorization requirements of 'Product.dimension'. Not covered: @authenticated.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Product.delivery",
              "schema": "A",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_NestedSelectionReachesAProtectedField()
    {
        // arrange & act & assert
        AssertInvalid(
        [
            $$"""
            # Schema A
            type Product @key(fields: "id") {
                id: ID!
                delivery(weight: Int! @require(field: "dimension.weight")): Int
                    @requiresScopes(scopes: [["read"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Product @key(fields: "id") {
                id: ID!
                dimension: Dimension @requiresScopes(scopes: [["read"]])
            }

            type Dimension {
                weight: Int @requiresScopes(scopes: [["pii"]])
            }

            {{Directives}}
            """
        ],
        [
            """
            {
              "message": "The field 'Product.delivery' depends on 'Dimension.weight' through 'Product.delivery(weight:)' in schema 'A', but does not declare all authorization requirements of 'Dimension.weight'. Not covered: @requiresScopes.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Product.delivery",
              "schema": "A",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_ObjectConstructionReachesAProtectedField()
    {
        // arrange & act & assert
        AssertInvalid(
        [
            $$"""
            # Schema A
            type Product @key(fields: "id") {
                id: ID!
                delivery(
                    dimension: DimensionInput!
                        @require(field: "{ size: dimension.size, weight: dimension.weight }")
                ): Int
            }

            input DimensionInput {
                size: Int!
                weight: Int!
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Product @key(fields: "id") {
                id: ID!
                dimension: Dimension!
            }

            type Dimension {
                size: Int!
                weight: Int! @authenticated
            }

            {{Directives}}
            """
        ],
        [
            """
            {
              "message": "The field 'Product.delivery' depends on 'Dimension.weight' through 'Product.delivery(dimension:)' in schema 'A', but does not declare all authorization requirements of 'Dimension.weight'. Not covered: @authenticated.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Product.delivery",
              "schema": "A",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_ChoiceBranchesWithTypeConditionsReachAProtectedField()
    {
        // arrange & act & assert
        AssertInvalid(
        [
            $$"""
            # Schema A
            type Product @key(fields: "id") {
                id: ID!
                headline(
                    title: String!
                        @require(field: "media<Book>.isbn | media<Movie>.title")
                ): String
            }

            interface Media {
                title: String!
            }

            type Book implements Media {
                title: String!
                isbn: String!
            }

            type Movie implements Media {
                title: String!
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Product @key(fields: "id") {
                id: ID!
                media: Media
            }

            interface Media {
                title: String!
            }

            type Book implements Media {
                title: String!
                isbn: String!
            }

            type Movie implements Media {
                title: String! @authenticated
            }

            {{Directives}}
            """
        ],
        [
            """
            {
              "message": "The field 'Product.headline' depends on 'Movie.title' through 'Product.headline(title:)' in schema 'A', but does not declare all authorization requirements of 'Movie.title'. Not covered: @authenticated.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Product.headline",
              "schema": "A",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_ChainDoesNotCoverItsOwnDependency()
    {
        // arrange & act & assert
        AssertInvalid(
        [
            $$"""
            # Schema A
            type Product @key(fields: "id") {
                id: ID!
                label(price: Int! @require(field: "price")): String
                    @requiresScopes(scopes: [["finance"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Product @key(fields: "id") {
                id: ID!
                price(cost: Int! @require(field: "cost")): Int
                    @requiresScopes(scopes: [["finance"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema C
            type Product @key(fields: "id") {
                id: ID!
                cost: Int @requiresScopes(scopes: [["finance", "internal"]])
            }

            {{Directives}}
            """
        ],
        [
            """
            {
              "message": "The field 'Product.price' depends on 'Product.cost' through 'Product.price(cost:)' in schema 'B', but does not declare all authorization requirements of 'Product.cost'. Not covered: @requiresScopes.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Product.price",
              "schema": "B",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_LookupKeyIsNotCoveredByAServedField()
    {
        // arrange & act & assert
        AssertInvalid(
        [
            $$"""
            # Schema A
            type Query { products: [Product] }

            type Product @key(fields: "id") {
                id: ID! @requiresScopes(scopes: [["admin"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query { productById(id: ID!): Product @lookup }

            type Product @key(fields: "id") {
                id: ID!
                name: String
            }

            {{Directives}}
            """
        ],
        [
            """
            {
              "message": "The field 'Product.name' depends on 'Product.id' through 'Query.productById' in schema 'B', but does not declare all authorization requirements of 'Product.id'. Not covered: @requiresScopes.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Product.name",
              "schema": "B",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_UnionLookupKeyIsNotCoveredByAServedField()
    {
        // arrange & act & assert
        AssertInvalid(
        [
            $$"""
            # Schema A
            type Query { dogs: [Dog] }

            type Dog @key(fields: "id") {
                id: ID! @requiresScopes(scopes: [["admin"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query { animalById(id: ID!): Animal @lookup }

            union Animal = Dog | Cat

            type Dog @key(fields: "id") {
                id: ID!
                name: String
            }

            type Cat @key(fields: "id") {
                id: ID!
            }

            {{Directives}}
            """
        ],
        [
            """
            {
              "message": "The field 'Dog.name' depends on 'Dog.id' through 'Query.animalById' in schema 'B', but does not declare all authorization requirements of 'Dog.id'. Not covered: @requiresScopes.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Dog.name",
              "schema": "B",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_UnionLookupKeyIsCoveredByTheServedFields()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Query { dogs: [Dog] }

            type Dog @key(fields: "id") {
                id: ID! @requiresScopes(scopes: [["admin"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query { animalById(id: ID!): Animal @lookup }

            union Animal = Dog | Cat

            type Dog @key(fields: "id") {
                id: ID!
                name: String @requiresScopes(scopes: [["admin"]])
            }

            type Cat @key(fields: "id") {
                id: ID!
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_InterfaceLookupKeyIsNotCoveredByAServedField()
    {
        // arrange & act & assert
        AssertInvalid(
        [
            $$"""
            # Schema A
            type Query { dogs: [Dog] }

            type Dog @key(fields: "id") {
                id: ID! @requiresScopes(scopes: [["admin"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query { animalById(id: ID!): Animal @lookup }

            interface Animal {
                id: ID!
            }

            type Dog implements Animal @key(fields: "id") {
                id: ID!
                name: String
            }

            {{Directives}}
            """
        ],
        [
            """
            {
              "message": "The field 'Dog.name' depends on 'Dog.id' through 'Query.animalById' in schema 'B', but does not declare all authorization requirements of 'Dog.id'. Not covered: @requiresScopes.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Dog.name",
              "schema": "B",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_InterfaceLookupKeyIsCoveredByTheServedFields()
    {
        // arrange & act & assert
        AssertValid(
        [
            $$"""
            # Schema A
            type Query { dogs: [Dog] }

            type Dog @key(fields: "id") {
                id: ID! @requiresScopes(scopes: [["admin"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query { animalById(id: ID!): Animal @lookup }

            interface Animal {
                id: ID!
            }

            type Dog implements Animal @key(fields: "id") {
                id: ID!
                name: String @requiresScopes(scopes: [["admin"]])
            }

            {{Directives}}
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_UnionLookupKeyChoiceStartsWithABranchForAnotherMember()
    {
        // arrange & act & assert
        AssertInvalid(
        [
            $$"""
            # Schema A
            type Query { dogs: [Dog] cats: [Cat] }

            type Dog @key(fields: "id") {
                id: ID! @requiresScopes(scopes: [["admin"]])
            }

            type Cat @key(fields: "id") {
                id: ID!
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query { animalById(key: ID! @is(field: "<Cat>.id | <Dog>.id")): Animal @lookup }

            union Animal = Dog | Cat

            type Dog @key(fields: "id") {
                id: ID!
                name: String
            }

            type Cat @key(fields: "id") {
                id: ID!
                age: Int
            }

            {{Directives}}
            """
        ],
        [
            """
            {
              "message": "The field 'Dog.name' depends on 'Dog.id' through 'Query.animalById' in schema 'B', but does not declare all authorization requirements of 'Dog.id'. Not covered: @requiresScopes.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Dog.name",
              "schema": "B",
              "extensions": {}
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_NestedUnionLookupKeyChoiceProtectsALaterMember()
    {
        // arrange & act & assert
        AssertInvalid(
        [
            $$"""
            # Schema A
            type Query { dogs: [Dog] cats: [Cat] }

            type Dog @key(fields: "id") {
                id: ID!
            }

            type Cat @key(fields: "id") {
                id: ID! @requiresScopes(scopes: [["admin"]])
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Query {
                animalByKey(key: AnimalKey! @is(field: "{ id: <Dog>.id | <Cat>.id }")): Animal @lookup
            }

            input AnimalKey { id: ID! }

            union Animal = Dog | Cat

            type Dog @key(fields: "id") {
                id: ID!
                name: String
            }

            type Cat @key(fields: "id") {
                id: ID!
                age: Int
            }

            {{Directives}}
            """
        ],
        [
            """
            {
              "message": "The field 'Cat.age' depends on 'Cat.id' through 'Query.animalByKey' in schema 'B', but does not declare all authorization requirements of 'Cat.id'. Not covered: @requiresScopes.",
              "code": "AUTHORIZATION_TRANSITIVE_REQUIREMENTS_MISSING",
              "severity": "Error",
              "coordinate": "Cat.age",
              "schema": "B",
              "extensions": {}
            }
            """
        ]);
    }

    private static string[] RequireSchemas(
        [StringSyntax("graphql")] string requiring,
        [StringSyntax("graphql")] string dependency)
    {
        return
        [
            $$"""
            # Schema A
            type Product @key(fields: "id") {
                id: ID!
                delivery(weight: Int! @require(field: "weight")): Int {{requiring}}
            }

            {{Directives}}
            """,
            $$"""
            # Schema B
            type Product @key(fields: "id") {
                id: ID!
                weight: Int {{dependency}}
            }

            {{Directives}}
            """
        ];
    }
}
