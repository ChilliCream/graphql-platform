using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentSpreadsMustNotFormCyclesRuleTests
        : ValidationTestBase
    {
        public FragmentSpreadsMustNotFormCyclesRuleTests()
            : base(new FragmentSpreadsMustNotFormCyclesRule())
        {
        }

        [Fact]
        public void FragmentCycle1()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "The graph of fragment spreads must not form any " +
                    "cycles including spreading itself. Otherwise an " +
                    "operation could infinitely spread or infinitely " +
                    "execute on cycles in the underlying data."));
        }

        [Fact]
        public void FragmentCycle2()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "The graph of fragment spreads must not form any " +
                    "cycles including spreading itself. Otherwise an " +
                    "operation could infinitely spread or infinitely " +
                    "execute on cycles in the underlying data."));
        }

        [Fact]
        public void InfiniteRecursion()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "The graph of fragment spreads must not form any " +
                    "cycles including spreading itself. Otherwise an " +
                    "operation could infinitely spread or infinitely " +
                    "execute on cycles in the underlying data."));
        }

        [Fact]
        public void QueryWithSideBySideFragSpreads()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }
    }
}
