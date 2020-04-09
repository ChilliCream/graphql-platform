using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentNameUniquenessRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public FragmentNameUniquenessRuleTests()
            : base(services => services.AddFragmentRules())
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
            // arrange
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
    }
}
