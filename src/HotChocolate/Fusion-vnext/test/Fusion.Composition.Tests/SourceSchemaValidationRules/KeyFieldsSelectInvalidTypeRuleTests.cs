namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class KeyFieldsSelectInvalidTypeRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new KeyFieldsSelectInvalidTypeRule();

    // In this example, the "Product" type has a valid @key directive referencing the scalar field
    // "sku".
    [Fact]
    public void Validate_KeyFieldsSelectValidType_Succeeds()
    {
        AssertValid(
        [
            """
            type Product @key(fields: "sku") {
                sku: String!
                name: String
            }
            """
        ]);
    }

    // In the following example, the "Product" type has an invalid @key directive referencing a
    // field ("featuredItem") whose type is an interface, violating the rule.
    [Fact]
    public void Validate_KeyFieldsSelectInvalidTypeInterface_Fails()
    {
        AssertInvalid(
            [
                """
                type Product @key(fields: "featuredItem { id }") {
                    featuredItem: Node!
                    sku: String!
                }

                interface Node {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'Product' in schema 'A' references field 'Product.featuredItem', which must not be a list, interface, or union type.",
                    "code": "KEY_FIELDS_SELECT_INVALID_TYPE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // In this example, the @key directive references a field ("tags") of type "List", which is also
    // not allowed.
    [Fact]
    public void Validate_KeyFieldsSelectInvalidTypeList_Fails()
    {
        AssertInvalid(
            [
                """
                type Product @key(fields: "tags") {
                    tags: [String!]!
                    sku: String!
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'Product' in schema 'A' references field 'Product.tags', which must not be a list, interface, or union type.",
                    "code": "KEY_FIELDS_SELECT_INVALID_TYPE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // In this example, the @key directive references a field ("relatedItems") of type "Union",
    // which violates the rule.
    [Fact]
    public void Validate_KeyFieldsSelectInvalidTypeUnion_Fails()
    {
        AssertInvalid(
            [
                """
                type Product @key(fields: "relatedItems") {
                    relatedItems: Related!
                    sku: String!
                }

                union Related = Product | Service

                type Service {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'Product' in schema 'A' references field 'Product.relatedItems', which must not be a list, interface, or union type.",
                    "code": "KEY_FIELDS_SELECT_INVALID_TYPE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Nested interface.
    [Fact]
    public void Validate_KeyFieldsSelectInvalidTypeNestedInterface_Fails()
    {
        AssertInvalid(
            [
                """
                type Product @key(fields: "info { featuredItem { id } }") {
                    info: ProductInfo!
                }

                type ProductInfo {
                    featuredItem: Node!
                }

                interface Node {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'Product' in schema 'A' references field 'ProductInfo.featuredItem', which must not be a list, interface, or union type.",
                    "code": "KEY_FIELDS_SELECT_INVALID_TYPE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Nested list.
    [Fact]
    public void Validate_KeyFieldsSelectInvalidTypeNestedList_Fails()
    {
        AssertInvalid(
            [
                """
                type Product @key(fields: "info { tags }") {
                    info: ProductInfo!
                }

                type ProductInfo {
                    tags: [String!]!
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'Product' in schema 'A' references field 'ProductInfo.tags', which must not be a list, interface, or union type.",
                    "code": "KEY_FIELDS_SELECT_INVALID_TYPE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Nested union.
    [Fact]
    public void Validate_KeyFieldsSelectInvalidTypeNestedUnion_Fails()
    {
        AssertInvalid(
            [
                """
                type Product @key(fields: "info { relatedItems }") {
                    info: ProductInfo!
                }

                type ProductInfo {
                    relatedItems: Related!
                }

                union Related = Product | Service

                type Service {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'Product' in schema 'A' references field 'ProductInfo.relatedItems', which must not be a list, interface, or union type.",
                    "code": "KEY_FIELDS_SELECT_INVALID_TYPE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    // Multiple keys.
    [Fact]
    public void Validate_KeyFieldsSelectInvalidTypeMultipleKeys_Fails()
    {
        AssertInvalid(
            [
                """
                type Product
                    @key(fields: "featuredItem { id }")
                    @key(fields: "tags")
                    @key(fields: "relatedItems") {
                    featuredItem: Node!
                    tags: [String!]!
                    relatedItems: Related!
                }

                interface Node {
                    id: ID!
                }

                union Related = Product | Service

                type Service {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "A @key directive on type 'Product' in schema 'A' references field 'Product.featuredItem', which must not be a list, interface, or union type.",
                    "code": "KEY_FIELDS_SELECT_INVALID_TYPE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "A @key directive on type 'Product' in schema 'A' references field 'Product.tags', which must not be a list, interface, or union type.",
                    "code": "KEY_FIELDS_SELECT_INVALID_TYPE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """,
                """
                {
                    "message": "A @key directive on type 'Product' in schema 'A' references field 'Product.relatedItems', which must not be a list, interface, or union type.",
                    "code": "KEY_FIELDS_SELECT_INVALID_TYPE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "key",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
