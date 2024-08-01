using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class OperationNameUniquenessRuleTests
    : DocumentValidatorVisitorTestBase
{
    public OperationNameUniquenessRuleTests()
        : base(builder => builder.AddOperationRules())
    {
    }

    [Fact]
    public void TwoUniqueQueryOperations()
    {
        ExpectValid(@"
                query getDogName {
                    dog {
                        name
                    }
                }

                query getOwnerName {
                    dog {
                        owner {
                        name
                        }
                    }
                }
            ");
    }

    [Fact]
    public void TwoQueryOperationsWithTheSameName()
    {
        ExpectErrors(@"
                query getName {
                    dog {
                        name
                    }
                }

                query getName {
                    dog {
                        owner {
                            name
                        }
                    }
                }
            ",
            t => Assert.Equal(
                $"The operation name `getName` is not unique.",
                t.Message));
    }

    [Fact]
    public void TwoOperationsWithTheSameName()
    {
        ExpectErrors(@"
                query dogOperation {
                    dog {
                        name
                    }
                }

                mutation dogOperation {
                    mutateDog {
                        id
                    }
                }
            ",
            t => Assert.Equal(
                $"The operation name `dogOperation` is not unique.",
                t.Message));
    }

    [Fact]
    public void OneAnonOperation()
    {
        ExpectValid(@"
                {
                    anyArg
                }
            ");
    }

    [Fact]
    public void OneNamedOperation()
    {
        ExpectValid(@"
                query Foo {
                    anyArg
                }
            ");
    }

    [Fact]
    public void MultipleOperationsOfDifferentTypes()
    {
        ExpectValid(@"
                query Foo {
                    anyArg
                }

                mutation Bar {
                    field
                }

                subscription Baz {
                    newMessage {
                        bdoy
                    }
                }
            ");
    }

    [Fact]
    public void FragmentAndOperationNamedTheSame()
    {
        ExpectValid(@"
                query Foo {
                    ...Foo
                }

                fragment Foo on Query {
                    anyArg
                }
            ");
    }

    [Fact]
    public void MultipleOperationsOfSameName()
    {
        ExpectErrors(@"
                query Foo {
                    anyArg
                }

                query Foo {
                    anyArg
                }
            ");
    }

    [Fact]
    public void MultipleOpsOfSameNameOfDifferentTypesMutation()
    {
        ExpectErrors(@"
                query Foo {
                    anyArg
                }

                mutation Foo {
                    fieldB
                }
            ");
    }

    [Fact]
    public void MultipleOpsOfSameNameOfDifferentTypesSubscription()
    {
        ExpectErrors(@"
                query Foo {
                    anyArg
                }

                subscription Foo {
                    newMessage {
                        bdoy
                    }
                }
            ");
    }
}
