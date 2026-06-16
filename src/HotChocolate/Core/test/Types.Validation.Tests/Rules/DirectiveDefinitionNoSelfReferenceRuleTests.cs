using HotChocolate.Rules;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Types.Validation.Rules;

public sealed class DirectiveDefinitionNoSelfReferenceRuleTests
    : RuleTestBase<DirectiveDefinitionNoSelfReferenceRule>
{
    [Fact]
    public void Validate_DirectiveDoesNotReferenceItself_Succeeds()
    {
        var schema = new MutableSchemaDefinition();
        var onDirectiveDefinition = new MutableDirectiveDefinition("onDirectiveDefinition")
        {
            Locations = DirectiveLocation.DirectiveDefinition
        };
        var custom = new TestDirectiveDefinition("custom")
        {
            Locations = DirectiveLocation.Object
        };
        custom.Directives.Add(new Mutable.Directive(onDirectiveDefinition));
        schema.DirectiveDefinitions.Add(onDirectiveDefinition);
        schema.DirectiveDefinitions.Add(custom);

        AssertValid(schema);
    }

    [Fact]
    public void Validate_DirectiveDefinitionSelfApplication_Fails()
    {
        var schema = new MutableSchemaDefinition();
        var custom = new TestDirectiveDefinition("custom")
        {
            Locations = DirectiveLocation.DirectiveDefinition
        };
        custom.Directives.Add(new Mutable.Directive(custom));
        schema.DirectiveDefinitions.Add(custom);

        AssertInvalid(
            schema,
            """
            {
                "message": "The directive definition '@custom' must not reference itself.",
                "code": "HCV0027",
                "severity": "Error",
                "coordinate": "@custom",
                "member": "@custom",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    [Fact]
    public void Validate_DirectiveDefinitionArgumentSelfApplication_Fails()
    {
        var schema = new MutableSchemaDefinition();
        var custom = new TestDirectiveDefinition("custom")
        {
            Locations = DirectiveLocation.ArgumentDefinition | DirectiveLocation.DirectiveDefinition
        };
        var argument = new MutableInputFieldDefinition("arg");
        argument.Directives.Add(new Mutable.Directive(custom));
        custom.Arguments.Add(argument);
        schema.DirectiveDefinitions.Add(custom);

        AssertInvalid(
            schema,
            """
            {
                "message": "The directive definition '@custom' must not reference itself.",
                "code": "HCV0027",
                "severity": "Error",
                "coordinate": "@custom",
                "member": "@custom",
                "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Type-System.Directives.Type-Validation"
                }
            }
            """);
    }

    private sealed class TestDirectiveDefinition(string name)
        : MutableDirectiveDefinition(name)
        , IDirectivesProvider
    {
        public Mutable.DirectiveCollection Directives { get; } = [];

        IReadOnlyDirectiveCollection IDirectivesProvider.Directives => Directives;
    }
}
