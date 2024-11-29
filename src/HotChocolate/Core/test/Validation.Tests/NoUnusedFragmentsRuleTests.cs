using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class NoUnusedFragmentsRuleTests
    : DocumentValidatorVisitorTestBase
{
    public NoUnusedFragmentsRuleTests()
        : base(services => services.AddFragmentRules())
    {
    }

    [Fact]
    public void AllFragmentNamesAreUsed()
    {
        ExpectValid(@"
                {
                    human {
                        ...HumanFields1
                        ... on Human {
                        ...HumanFields2
                        }
                    }
                }
                fragment HumanFields1 on Human {
                    name
                    ...HumanFields3
                }
                fragment HumanFields2 on Human {
                    name
                }
                fragment HumanFields3 on Human {
                    name
                }
            ");
    }

    [Fact]
    public void AllFragmentNamesAreUsedByMultipleOperations()
    {
        ExpectValid(@"
                query Foo {
                    human(id: 4) {
                    ...HumanFields1
                    }
                }
                query Bar {
                    human(id: 4) {
                    ...HumanFields2
                    }
                }
                fragment HumanFields1 on Human {
                    name
                    ...HumanFields3
                }
                fragment HumanFields2 on Human {
                    name
                }
                fragment HumanFields3 on Human {
                    name
                }
            ");
    }

    [Fact]
    public void ContainsUnknownFragments()
    {
        ExpectErrors(@"
                query Foo {
                    human(id: 4) {
                    ...HumanFields1
                    }
                }
                query Bar {
                    human(id: 4) {
                    ...HumanFields2
                    }
                }
                fragment HumanFields1 on Human {
                    name
                    ...HumanFields3
                }
                fragment HumanFields2 on Human {
                    name
                }
                fragment HumanFields3 on Human {
                    name
                }
                fragment Unused1 on Human {
                    name
                }
                fragment Unused2 on Human {
                    name
                }
            ");
    }

    [Fact]
    public void ContainsUnknownFragmentsWithRefCycle()
    {
        ExpectErrors(@"
                query Foo {
                    human(id: 4) {
                    ...HumanFields1
                    }
                }
                query Bar {
                    human(id: 4) {
                    ...HumanFields2
                    }
                }
                fragment HumanFields1 on Human {
                    name
                    ...HumanFields3
                }
                fragment HumanFields2 on Human {
                    name
                }
                fragment HumanFields3 on Human {
                    name
                }
                fragment Unused1 on Human {
                    name
                    ...Unused2
                }
                fragment Unused2 on Human {
                    name
                    ...Unused1
                }
            ");
    }
    [Fact]
    public void ContainsUnknownAndUndefFragments()
    {
        ExpectErrors(@"
               query Foo {
                    human(id: 4) {
                    ...bar
                    }
                }
                fragment foo on Human {
                    name
                }
            ");
    }
}
