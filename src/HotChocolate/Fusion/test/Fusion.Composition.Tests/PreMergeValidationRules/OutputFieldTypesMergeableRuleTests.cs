namespace HotChocolate.Fusion.PreMergeValidationRules;

public sealed class OutputFieldTypesMergeableRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new OutputFieldTypesMergeableRule();

    // Fields with the same type are mergeable.
    [Fact]
    public void Validate_OutputFieldTypesMergeable_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                birthdate: String
            }
            """,
            """
            type User {
                birthdate: String
            }
            """
        ]);
    }

    // Fields with different nullability are mergeable, resulting in a merged field with a nullable
    // type.
    [Fact]
    public void Validate_OutputFieldTypesMergeableDifferentNullability_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                birthdate: String!
            }
            """,
            """
            type User {
                birthdate: String
            }
            """
        ]);
    }

    // Fields that differ on nullability of a field list type are mergeable.
    [Fact]
    public void Validate_OutputFieldTypesMergeableDifferentNullabilityListTypes_Succeeds()
    {
        AssertValid(
        [
            """
            type User {
                tags: [String!]
            }
            """,
            """
            type User {
                tags: [String]!
            }
            """,
            """
            type User {
                tags: [String]
            }
            """
        ]);
    }

    // Fields are not mergeable if the named types are different in kind or name.
    [Fact]
    public void Validate_OutputFieldTypesNotMergeableDifferentNamedTypes_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    birthdate: String!
                }
                """,
                """
                type User {
                    birthdate: DateTime!
                }
                """
            ],
            [
                """
                {
                    "message": "The output field 'User.birthdate' has a different type shape in schema 'A' than it does in schema 'B'.",
                    "code": "OUTPUT_FIELD_TYPES_NOT_MERGEABLE",
                    "severity": "Error",
                    "coordinate": "User.birthdate",
                    "member": "birthdate",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Fields are not mergeable if the list type elements are different in kind or name.
    [Fact]
    public void Validate_OutputFieldTypesNotMergeableDifferentListElementKind_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    tags: [Tag]
                }

                type Tag {
                    value: String
                }
                """,
                """
                type User {
                    tags: [Tag]
                }

                scalar Tag
                """
            ],
            [
                """
                {
                    "message": "The output field 'User.tags' has a different type shape in schema 'A' than it does in schema 'B'.",
                    "code": "OUTPUT_FIELD_TYPES_NOT_MERGEABLE",
                    "severity": "Error",
                    "coordinate": "User.tags",
                    "member": "tags",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // A single composed return type is selected per field group, so an unmergeable group is
    // reported once, naming the first field that cannot be unified with the preceding fields.
    [Fact]
    public void Validate_OutputFieldTypesNotMergeableMoreThanTwoSchemas_Fails()
    {
        AssertInvalid(
            [
                """
                type User {
                    birthdate: String!
                }
                """,
                """
                type User {
                    birthdate: DateTime!
                }
                """,
                """
                type User {
                    birthdate: Int!
                }
                """
            ],
            [
                """
                {
                    "message": "The output field 'User.birthdate' has a different type shape in schema 'A' than it does in schema 'B'.",
                    "code": "OUTPUT_FIELD_TYPES_NOT_MERGEABLE",
                    "severity": "Error",
                    "coordinate": "User.birthdate",
                    "member": "birthdate",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Fields with composite return types are mergeable when one declared return type is a supertype
    // of all the others. Here the union contains the object type.
    [Fact]
    public void Validate_OutputFieldTypesMergeableUnionSupertypeOfObject_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                featured: FeaturedItem
            }

            union FeaturedItem = Product

            type Product {
                id: ID
            }
            """,
            """
            type Query {
                featured: Product
            }

            type Product {
                id: ID
            }
            """
        ]);
    }

    // An interface is a valid supertype of an object that implements it. The implementing object can
    // be defined in a different source schema than the one that returns the interface, so the
    // implementation relationship is read from the object's own schema.
    [Fact]
    public void Validate_OutputFieldTypesMergeableInterfaceSupertypeOfObject_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                node: Node
            }

            interface Node {
                id: ID
            }
            """,
            """
            type Query {
                node: Product
            }

            interface Node {
                id: ID
            }

            type Product implements Node {
                id: ID
            }
            """
        ]);
    }

    // The same union is declared in multiple schemas that each contribute different members. The
    // union still covers the object type, because its members are considered together rather than
    // per schema, so the field remains mergeable regardless of source schema order.
    [Fact]
    public void Validate_OutputFieldTypesMergeableUnionMembersSplitAcrossSchemas_Succeeds()
    {
        AssertValid(
        [
            """
            type Query {
                featured: FeaturedItem
            }

            union FeaturedItem = Product

            type Product {
                id: ID
            }
            """,
            """
            type Query {
                featured: Product
            }

            type Product {
                id: ID
            }
            """,
            """
            type Query {
                featured: FeaturedItem
            }

            union FeaturedItem = Review

            type Review {
                id: ID
            }
            """
        ]);
    }

    // Fields with composite return types are not mergeable when no declared return type is a
    // supertype of all the others.
    [Fact]
    public void Validate_OutputFieldTypesNotMergeableNoCommonSupertype_Fails()
    {
        AssertInvalid(
            [
                """
                type Query {
                    featured: FeaturedItem
                }

                union FeaturedItem = Product

                type Product {
                    id: ID
                }
                """,
                """
                type Query {
                    featured: Review
                }

                type Review {
                    id: ID
                }
                """
            ],
            [
                """
                {
                    "message": "The output field 'Query.featured' has a different type shape in schema 'A' than it does in schema 'B'.",
                    "code": "OUTPUT_FIELD_TYPES_NOT_MERGEABLE",
                    "severity": "Error",
                    "coordinate": "Query.featured",
                    "member": "featured",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
