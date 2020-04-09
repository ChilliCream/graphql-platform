using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class ArgumentNamesRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public ArgumentNamesRuleTests()
            : base(services => services.AddArgumentRules())
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

        [Fact]
        public void IgnoreArgsOfUnknowFields()
        {
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query {
                    dog {
                        ... argOnUnknownField 
                    }
                }

                fragment argOnUnknownField on Dog {
                    unknownField(unknownArg: SIT)
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.True(context.UnexpectedErrorsDetected);
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void ArgsAreKnowDeeply()
        {
            ExpectValid(@"
                {
                    dog {
                        doesKnowCommand(dogCommand: SIT)
                    }
                    human {
                        pets {
                            ... on Dog {
                                doesKnowCommand(dogCommand: SIT)
                            }
                        }
                    }
                }
            ");
        }

        [Fact]
        public void DirectiveArgsAreKnown()
        {
            ExpectValid(@"
                {
                    dog @skip(if: true)
                }
            ");
        }

        [Fact]
        public void DirectiveWithoutArgsIsValid()
        {
            ExpectValid(@"
                {
                    dog @complex
                }
            ");
        }

        [Fact]
        public void DirectiveWithWrongArgsIsInvalid()
        {
            ExpectErrors(@"
                {
                    dog @complex(if:false)
                }
            ");
        }

        [Fact]
        public void MisspelledDirectiveArgsAreReported()
        {
            ExpectErrors(@"
                {
                    dog @skip(iff: true)
                }
            ");
        }

        [Fact]
        public void MisspelledFieldArgsAreReported()
        {
            ExpectErrors(@"
                query {
                    dog {
                        ... invalidArgName
                    }
                }
                fragment invalidArgName on Dog {
                    doesKnowCommand(DogCommand: true)
                }
            ");
        }

        [Fact]
        public void UnknownArgsAmongstKnowArgs()
        {
            ExpectErrors(@"
                query {
                    dog {
                        ... oneGoodArgOneInvalidArg
                    }
                }
                fragment oneGoodArgOneInvalidArg on Dog {
                    doesKnowCommand(whoKnows: 1, dogCommand: SIT, unknown: true)
                }
            ");
        }

        [Fact]
        public void UnknownArgsDeeply()
        {
            ExpectErrors(@"
                {
                    dog {
                        doesKnowCommand(unknown: true)
                    }
                    human {
                    pet {
                        ... on Dog {
                                doesKnowCommand(unknown: true)
                            }
                        }
                    }
                }
            ");
        }
    }
}
