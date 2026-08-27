namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class PolicyOnInterfaceRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new PolicyOnInterfaceRule();

    [Fact]
    public void Validate_Should_Succeed_When_PolicyIsOnObjectTypeAndField()
    {
        AssertValid(
        [
            """
            # Source Schema A
            type Order @policy(names: "CanReadOrder") {
                id: ID!
                amount: Int @policy(names: "CanReadAmount")
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_PolicyIsOnInterfaceType()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                interface Node @policy(names: "CanReadNode") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The interface type 'Node' in schema 'A' must not be annotated with the @policy directive.",
                    "code": "POLICY_ON_INTERFACE",
                    "severity": "Error",
                    "coordinate": "Node",
                    "member": "Node",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_PolicyIsOnInterfaceField()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                interface Node {
                    id: ID! @policy(names: "CanReadNodeId")
                }
                """
            ],
            [
                """
                {
                    "message": "The interface field 'Node.id' in schema 'A' must not be annotated with the @policy directive.",
                    "code": "POLICY_ON_INTERFACE",
                    "severity": "Error",
                    "coordinate": "Node.id",
                    "member": "id",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_PolicyIsOnInterfaceObjectStandInType()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                type Product @interfaceObject @policy(names: "CanReadProduct") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The interface type 'Product' in schema 'A' must not be annotated with the @policy directive.",
                    "code": "POLICY_ON_INTERFACE",
                    "severity": "Error",
                    "coordinate": "Product",
                    "member": "Product",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_PolicyIsOnInterfaceObjectStandInField()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                type Product @interfaceObject {
                    id: ID!
                    name: String @policy(names: "CanReadProductName")
                }
                """
            ],
            [
                """
                {
                    "message": "The interface field 'Product.name' in schema 'A' must not be annotated with the @policy directive.",
                    "code": "POLICY_ON_INTERFACE",
                    "severity": "Error",
                    "coordinate": "Product.name",
                    "member": "name",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
