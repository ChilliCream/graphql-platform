using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentsMustBeUsedRuleTests
       : DocumentValidatorVisitorTestBase
    {
        public FragmentsMustBeUsedRuleTests()
            : base(services => services.AddFragmentRules())
        {
        }

        [Fact]
        public void UnusedFragment()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                fragment nameFragment on Dog { # unused
                    name
                }

                {
                    dog {
                        name
                    }
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The specified fragment `nameFragment` " +
                    "is not used within the current document.", t.Message));
            context.Errors.MatchSnapshot();
        }

        [Fact]
        public void UsedFragment()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                fragment nameFragment on Dog {
                    name
                }

                {
                    dog {
                        name
                        ... nameFragment
                    }
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void UsedNestedFragment()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                fragment nameFragment on Dog {
                    name
                    ... nestedNameFragment
                }

                fragment nestedNameFragment on Dog {
                    name
                }

                {
                    dog {
                        name
                        ... nameFragment
                    }
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
