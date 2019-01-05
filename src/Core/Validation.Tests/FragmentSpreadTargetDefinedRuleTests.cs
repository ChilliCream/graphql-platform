using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentSpreadTargetDefinedRuleTests
        : ValidationTestBase
    {
        public FragmentSpreadTargetDefinedRuleTests()
            : base(new FragmentSpreadTargetDefinedRule())
        {
        }

        [Fact]
        public void UndefinedFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...undefinedFragment
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified fragment `undefinedFragment` " +
                    "does not exist.",
                    t.Message));
        }

        [Fact]
        public void DefinedFragment()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...definedFragment
                    }
                }

                fragment definedFragment on Dog
                {
                    barkVolume
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }
    }
}
