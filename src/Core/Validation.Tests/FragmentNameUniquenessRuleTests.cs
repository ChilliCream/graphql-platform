using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentNameUniquenessRuleTests
        : ValidationTestBase
    {
        public FragmentNameUniquenessRuleTests()
            : base(new FragmentNameUniquenessRule())
        {
        }

        [Fact]
        public void UniqueFragments()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void DuplicateFragments()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "There are multiple fragments with the name `fragmentOne`.",
                    t.Message));
        }
    }
}
