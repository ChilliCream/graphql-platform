using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class FragmentNameUniquenessRuleTests
    : DocumentValidatorVisitorTestBase
{
    public FragmentNameUniquenessRuleTests()
        : base(builder => builder.AddFragmentRules())
    {
    }

    [Fact]
    public void UniqueFragments()
    {
        ExpectValid(@"
                {
                    dog {
                        ...fragmentOne
                        ...fragmentTwo
                    }
                }

                fragment fragmentOne on Dog {
                    name
                }

                fragment fragmentTwo on Dog {
                    owner {
                        name
                    }
                }
            ");
    }

    [Fact]
    public void DuplicateFragments()
    {
        ExpectErrors(@"
                {
                    dog {
                        ...fragmentOne
                    }
                }

                fragment fragmentOne on Dog {
                    name
                }

                fragment fragmentOne on Dog {
                    owner {
                        name
                    }
                }
            ",
            t => Assert.Equal(
                "There are multiple fragments with the name `fragmentOne`.",
                t.Message));
    }

    [Fact]
    public void OneFragment()
    {
        // arrange
        ExpectValid(@"
                {
                    ...fragA
                }

                fragment fragA on Query {
                    arguments {
                        idArgField
                    }
                }
            ");
    }

    [Fact]
    public void ManyFragments()
    {
        // arrange
        ExpectValid(@"
                {
                    ...fragA
                    ...fragB
                    ...fragC
                }

                fragment fragA on Query {
                    arguments {
                        idArgField
                    }
                }

                fragment fragB on Query {
                    dog {
                        name
                    }
                }

                fragment fragC on Query {
                    anyArg
                }
            ");
    }

    [Fact]
    public void InlineFragmentsAreAlwaysUnique()
    {
        // arrange
        ExpectValid(@"
                {
                    ...on Query {
                        arguments {
                            idArgField
                        }
                    }
                    ...on Query {
                        dog {
                            name
                        }
                    }
                }
            ");
    }

    [Fact]
    public void FragmentAndOperationNamedTheSame()
    {
        // arrange
        ExpectValid(@"
                query Foo {
                    ...Foo
                }

                fragment Foo on Query {
                    dog {
                        name
                    }
                }
            ");
    }

    [Fact]
    public void FragmentsNamedTheSame()
    {
        // arrange
        ExpectErrors(@"
                {
                    ...fragA
                }

                fragment fragA on Query {
                    arguments {
                        idArgField
                    }
                }

                fragment fragA on Query {
                    dog {
                        name
                    }
                }
            ");
    }

    [Fact]
    public void FragmentsNamedTheSameWithoutBeingReferenced()
    {
        // arrange
        ExpectErrors(@"
                fragment fragA on Query {
                    arguments {
                        idArgField
                    }
                }

                fragment fragA on Query {
                    dog {
                        name
                    }
                }
            ");
    }
}
