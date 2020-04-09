using HotChocolate.Language;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation
{
    public class NoUndefinedVariablesRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public NoUndefinedVariablesRuleTests()
            : base(services => services.AddVariableRules())
        {
        }

        [Fact]
        public void AllVariablesDefined()
        {
            // arrange
            ExpectValid(@"
                query Foo($a: String, $b: String, $c: String) {
                    field(a: $a, b: $b, c: $c)
                }
            ");
        }

        [Fact]
        public void AllVariablesDeeplyDefined()
        {
            // arrange
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
        public void AllVariablesDeeplyInInlineFragmentsDefined()
        {
            // arrange
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
        public void AllVariablesInFragmentsDeeplyDefined()
        {
            // arrange
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
        public void VariableWithinSingleFragmentDefinedInMultipleOperations()
        {
            // arrange
            ExpectValid(@"
                query Foo($a: String) {
                    ...FragA
                }
                query Bar($a: String) {
                    ...FragA
                }
                fragment FragA on Query {
                    field(a: $a)
                }
            ");
        }

        [Fact]
        public void VariableWithinFragmentsDefinedInOperations()
        {
            // arrange
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
        public void VariableWithinRecursiveFragmentDefined()
        {
            // arrange
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
        public void VariableNotDefined()
        {
            // arrange
            ExpectErrors(@"
                query Foo($a: String, $b: String, $c: String) {
                    field(a: $a, b: $b, c: $c, d: $d)
                }
            ");
        }

        [Fact]
        public void VariableNotDefinedByUnNamedQuery()
        {
            // arrange
            ExpectErrors(@"
                query Foo($a: String, $b: String, $c: String) {
                    field(a: $a, b: $b, c: $c, d: $d)
                }
            ");
        }

        [Fact]
        public void MultipleVariablesNotDefined()
        {
            // arrange
            ExpectErrors(@"
                query Foo($b: String) {
                    field(a: $a, b: $b, c: $c)
                }
            ");
        }

        [Fact]
        public void VariableInFragmentNotDefinedByUnNamedQuery()
        {
            // arrange
            ExpectErrors(@" 
                {
                    ...FragA
                }
                fragment FragA on Query {
                    field(a: $a)
                }
            ");
        }

        [Fact]
        public void VariableInFragmentNotDefinedByOperation()
        {
            // arrange
            ExpectErrors(@" 
                query Foo($a: String, $b: String) {
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
        public void MultipleVariablesInFragmentsNotDefined()
        {
            // arrange
            ExpectErrors(@" 
                query Foo($b: String) {
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
        public void SingleVariableInFragmentNotDefinedByMultipleOperations()
        {
            // arrange
            ExpectErrors(@" 
                query Foo($a: String) {
                    ...FragAB
                }
                query Bar($a: String) {
                    ...FragAB
                }
                fragment FragAB on Query {
                    field(a: $a, b: $b)
                }
            ");
        }

        [Fact]
        public void VariablesInFragmentNotDefinedByMultipleOperations()
        {
            // arrange
            ExpectErrors(@" 
                query Foo($b: String) {
                    ...FragAB
                }
                query Bar($a: String) {
                    ...FragAB
                }
                fragment FragAB on Query {
                    field(a: $a, b: $b)
                }
            ");
        }

        [Fact]
        public void VariableInFragmentUsedByOtherOperation()
        {
            // arrange
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

        [Fact]
        public void MultipleUndefinedVariablesProduceMultipleErrors()
        {
            // arrange
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
}
