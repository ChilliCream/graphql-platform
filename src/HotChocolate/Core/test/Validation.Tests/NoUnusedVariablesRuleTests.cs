using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class NoUnusedVariablesRuleTests
    : DocumentValidatorVisitorTestBase
{
    public NoUnusedVariablesRuleTests()
        : base(services => services.AddVariableRules())
    {
    }

    [Fact]
    public void UsesAllVariables()
    {
        ExpectValid(@"
                query ($a: String, $b: String, $c: String) {
                    field(a: $a, b: $b, c: $c)
                }
            ");
    }

    [Fact]
    public void UsesAllVariablesDeeply()
    {
        ExpectValid(@"
                query Foo($a: String, $b: String, $c: String) {
                    field(a: $a) {
                        field(b: $b) {
                            field(c: $c)
                        }
                    }
                }
            ");
    }

    [Fact]
    public void UsesAllVariablesDeeplyInInlineFragments()
    {
        ExpectValid(@"
                query Foo($a: String, $b: String, $c: String) {
                        ... on Query {
                            field(a: $a) {
                                field(b: $b) {
                                    ... on Query {
                                        field(c: $c)
                                    }
                                }
                            }
                        }
                }
            ");
    }

    [Fact]
    public void UsesAllVariablesInFragments()
    {
        ExpectValid(@"
                query Foo($a: String, $b: String, $c: String) {
                    ...FragA
                }
                fragment FragA on Query {
                    field(a: $a) {
                    ...FragB
                    }
                }
                fragment FragB on Query {
                    field(b: $b) {
                    ...FragC
                    }
                }
                fragment FragC on Query {
                    field(c: $c)
                }
            ");
    }

    [Fact]
    public void VariableUsedByFragmentInMultipleOperations()
    {
        ExpectValid(@"
                query Foo($a: String) {
                ...FragA
                }
                query Bar($b: String) {
                 ...FragB
                }
                fragment FragA on Query {
                  field(a: $a)
                }
                fragment FragB on Query {
                  field(b: $b)
                }
            ");
    }

    [Fact]
    public void VariableUsedByRecursiveFragment()
    {
        ExpectValid(@"
                query Foo($a: String) {
                    ...FragA
                }
                fragment FragA on Query {
                    field(a: $a) {
                    ...FragA
                    }
                }
            ");
    }

    [Fact]
    public void MultipleVariablesNotUsed()
    {
        ExpectErrors(@"
                query Foo($a: String, $b: String, $c: String) {
                    field(b: $b)
                }
            ");
    }

    [Fact]
    public void VariableNotUsedInFragments()
    {
        ExpectErrors(@"
                query Foo($a: String, $b: String, $c: String) {
                    ...FragA
                }
                fragment FragA on Query {
                    field(a: $a) {
                    ...FragB
                    }
                }
                fragment FragB on Query {
                    field(b: $b) {
                    ...FragC
                    }
                }
                fragment FragC on Query {
                    field
                }
            ");
    }

    [Fact]
    public void MultipleVariablesNotUsedInFragments()
    {
        ExpectErrors(@"
                query Foo($a: String, $b: String, $c: String) {
                    ...FragA
                }
                fragment FragA on Query {
                    field(a: $a) {
                    ...FragB
                    }
                }
                fragment FragB on Query {
                    field(b: $b) {
                    ...FragC
                    }
                }
                fragment FragC on Query {
                    field
                }
            ");
    }

    [Fact]
    public void VariableNotUsedByUnreferencedFragment()
    {
        ExpectErrors(@"
                query Foo($b: String) {
                    ...FragA
                }
                fragment FragA on Query {
                    field(a: $a)
                }
                fragment FragB on Query {
                    field(b: $b)
                }
            ");
    }

    [Fact]
    public void VariableNotUsedByFragmentUsedByOtherOperation()
    {
        ExpectErrors(@"
                query Foo($b: String) {
                    ...FragA
                }
                query Bar($a: String) {
                    ...FragB
                }
                fragment FragA on Query {
                    field(a: $a)
                }
                fragment FragB on Query {
                    field(b: $b)
                }
            ");
    }
}
