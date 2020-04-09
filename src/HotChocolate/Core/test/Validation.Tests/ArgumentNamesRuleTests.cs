using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Validation
{
    public class ArgumentNamesRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public ArgumentNamesRuleTests()
            : base(builder => builder.AddArgumentRules())
        {
        }

        [Fact]
        public void ArgOnRequiredArg()
        {
            ExpectValid(@"
                query {
                    dog {
                        ... argOnRequiredArg
                    }
                }

                fragment argOnRequiredArg on Dog {
                    doesKnowCommand(dogCommand: SIT)
                }
            ");
        }

        [Fact]
        public void ArgOnOptional()
        {
            ExpectValid(@"
                query {
                    dog {
                        ... argOnOptional
                    }
                }

                fragment argOnOptional on Dog {
                    isHouseTrained(atOtherHomes: true) @include(if: true)
                }
            ");
        }

        [Fact]
        public void InvalidFieldArgName()
        {
            ExpectErrors(@"
                query {
                    dog {
                        ... invalidArgName
                    }
                }

                fragment invalidArgName on Dog {
                    doesKnowCommand(command: CLEAN_UP_HOUSE)
                }
            ",
            t => Assert.Equal(
                $"The argument `command` does not exist.", t.Message),
            t => Assert.Equal(
                $"The argument `dogCommand` is required.", t.Message));
        }

        [Fact]
        public void InvalidDirectiveArgName()
        {
            ExpectErrors(@"
                query {
                    dog {
                        ... invalidArgName
                    }
                }

                fragment invalidArgName on Dog {
                    isHouseTrained(atOtherHomes: true) @include(unless: false)
                }
            ",
            t => Assert.Equal(
                $"The argument `unless` does not exist.", t.Message),
            t => Assert.Equal(
                $"The argument `if` is required.", t.Message));
        }

        [Fact]
        public void ArgumentOrderDoesNotMatter()
        {
            // arrange
            ExpectValid(@"
                query {
                    arguments {
                        ... multipleArgs
                        ... multipleArgsReverseOrder
                    }
                }

                fragment multipleArgs on Arguments {
                    multipleReqs(x: 1, y: 2)
                }

                fragment multipleArgsReverseOrder on Arguments {
                    multipleReqs(y: 1, x: 2)
                }
            ");
        }
    }
}
