namespace HotChocolate.Fusion.SourceSchemaValidationRules;

public sealed class PolicyDefinitionInvalidRuleTests : RuleTestBase
{
    protected override object Rule { get; } = new PolicyDefinitionInvalidRule();

    [Fact]
    public void Validate_Should_Succeed_When_DefinitionIsCanonical()
    {
        AssertValid(
        [
            """
            # Source Schema A
            directive @policy(
                names: [[String!]!]!
                onDenied: PolicyDenialBehavior
            ) repeatable on OBJECT | FIELD_DEFINITION

            enum PolicyDenialBehavior {
                NULL
                ERROR
                ABORT
            }

            type Order @policy(names: "CanReadOrder") {
                id: ID!
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Succeed_When_SchemaDoesNotDeclarePolicy()
    {
        AssertValid(
        [
            """
            # Source Schema A
            type Order {
                id: ID!
            }
            """
        ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_OnDeniedArgumentMissing()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                directive @policy(
                    names: [[String!]!]!
                ) repeatable on OBJECT | FIELD_DEFINITION

                type Order @policy(names: "CanReadOrder") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @policy directive definition declared in schema 'A' is incompatible with the canonical definition. Expected: directive @policy(names: [[String!]!]! onDenied: PolicyDenialBehavior) repeatable on OBJECT | FIELD_DEFINITION",
                    "code": "POLICY_DEFINITION_INVALID",
                    "severity": "Error",
                    "member": "A",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_DefinitionIsNotRepeatable()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                directive @policy(
                    names: [[String!]!]!
                    onDenied: PolicyDenialBehavior
                ) on OBJECT | FIELD_DEFINITION

                enum PolicyDenialBehavior {
                    NULL
                    ERROR
                    ABORT
                }

                type Order @policy(names: "CanReadOrder") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @policy directive definition declared in schema 'A' is incompatible with the canonical definition. Expected: directive @policy(names: [[String!]!]! onDenied: PolicyDenialBehavior) repeatable on OBJECT | FIELD_DEFINITION",
                    "code": "POLICY_DEFINITION_INVALID",
                    "severity": "Error",
                    "member": "A",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_LocationIncludesInterface()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                directive @policy(
                    names: [[String!]!]!
                    onDenied: PolicyDenialBehavior
                ) repeatable on OBJECT | FIELD_DEFINITION | INTERFACE

                enum PolicyDenialBehavior {
                    NULL
                    ERROR
                    ABORT
                }

                type Order @policy(names: "CanReadOrder") {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @policy directive definition declared in schema 'A' is incompatible with the canonical definition. Expected: directive @policy(names: [[String!]!]! onDenied: PolicyDenialBehavior) repeatable on OBJECT | FIELD_DEFINITION",
                    "code": "POLICY_DEFINITION_INVALID",
                    "severity": "Error",
                    "member": "A",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }

    [Fact]
    public void Validate_Should_Fail_When_DefinitionIncompatibleAndNoApplicationsExist()
    {
        AssertInvalid(
            [
                """
                # Source Schema A
                directive @policy(
                    names: [[String!]!]!
                ) repeatable on OBJECT | FIELD_DEFINITION

                type Order {
                    id: ID!
                }
                """
            ],
            [
                """
                {
                    "message": "The @policy directive definition declared in schema 'A' is incompatible with the canonical definition. Expected: directive @policy(names: [[String!]!]! onDenied: PolicyDenialBehavior) repeatable on OBJECT | FIELD_DEFINITION",
                    "code": "POLICY_DEFINITION_INVALID",
                    "severity": "Error",
                    "member": "A",
                    "schema": "A",
                    "extensions": {}
                }
                """
            ]);
    }
}
