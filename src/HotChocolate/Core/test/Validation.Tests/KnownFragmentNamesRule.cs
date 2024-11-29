using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class KnownFragmentNamesTests
    : DocumentValidatorVisitorTestBase
{
    public KnownFragmentNamesTests()
        : base(services => services.AddFragmentRules())
    {
    }

    [Fact]
    public void UniqueFragments()
    {
        ExpectValid(@"
                 {
                    human(id: 4) {
                        ...HumanFields1
                        ... on Human {
                            ...HumanFields2
                        }
                        ... {
                            name
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
    public void DuplicateFragments()
    {
        // arrange
        ExpectErrors(@"
                {
                    human(id: 4) {
                        ...UnknownFragment1
                        ... on Human {
                            ...UnknownFragment2
                        }
                    }
                }
                fragment HumanFields on Human {
                    name
                    ...UnknownFragment3
                }
            " );
    }
}
