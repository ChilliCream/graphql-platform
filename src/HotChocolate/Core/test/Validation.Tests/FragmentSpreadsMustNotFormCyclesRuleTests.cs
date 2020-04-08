using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using Xunit;
using Snapshooter.Xunit;

namespace HotChocolate.Validation
{
    public class FragmentSpreadsMustNotFormCyclesRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public FragmentSpreadsMustNotFormCyclesRuleTests()
            : base(services => services.AddFragmentRules())
        {
        }

        [Fact]
        public void FragmentCycle1()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...nameFragment
                    }
                }

                fragment nameFragment on Dog {
                    name
                    ...barkVolumeFragment
                }

                fragment barkVolumeFragment on Dog {
                    barkVolume
                    ...nameFragment
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(t.Message,
                    "The graph of fragment spreads must not form any " +
                    "cycles including spreading itself. Otherwise an " +
                    "operation could infinitely spread or infinitely " +
                    "execute on cycles in the underlying data."));
            context.Errors.MatchSnapshot();
        }

        [Fact]
        public void FragmentCycle2()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...nameFragment
                    }
                }

                fragment nameFragment on Dog {
                    name
                    ...barkVolumeFragment
                }

                fragment barkVolumeFragment on Dog {
                    barkVolume
                    ...barkVolumeFragment1
                }

                fragment barkVolumeFragment1 on Dog {
                    barkVolume
                    ...barkVolumeFragment2
                }

                fragment barkVolumeFragment2 on Dog {
                    barkVolume
                    ...nameFragment
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(t.Message,
                    "The graph of fragment spreads must not form any " +
                    "cycles including spreading itself. Otherwise an " +
                    "operation could infinitely spread or infinitely " +
                    "execute on cycles in the underlying data."));
            context.Errors.MatchSnapshot();
        }

        [Fact]
        public void InfiniteRecursion()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...dogFragment
                    }
                }

                fragment dogFragment on Dog {
                    name
                    owner {
                        ...ownerFragment
                    }
                }

                fragment ownerFragment on Human {
                    name
                    pets {
                        ...dogFragment
                    }
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(t.Message,
                    "The graph of fragment spreads must not form any " +
                    "cycles including spreading itself. Otherwise an " +
                    "operation could infinitely spread or infinitely " +
                    "execute on cycles in the underlying data."));
            context.Errors.MatchSnapshot();
        }

        [Fact]
        public void QueryWithSideBySideFragSpreads()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...dogFragment
                        ...dogFragment
                        ...dogFragment
                        ...dogFragment
                        ...dogFragment
                        ...dogFragment
                        ...dogFragment
                    }
                }

                fragment dogFragment on Dog {
                    name
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }
    }
}
