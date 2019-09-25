using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentsMustBeUsedRuleTests
       : ValidationTestBase
    {
        public FragmentsMustBeUsedRuleTests()
            : base(new FragmentsMustBeUsedRule())
        {
        }

        [Fact]
        public void UnusedFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified fragment `nameFragment` " +
                    "is not used within the current document.", t.Message));
        }

        [Fact]
        public void UsedFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void UsedNestedFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }
    }
}
