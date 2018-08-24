using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentsOnCompositeTypesRuleTests
        : ValidationTestBase
    {
        public FragmentsOnCompositeTypesRuleTests()
            : base(new FragmentsOnCompositeTypesRule())
        {
        }

        [Fact]
        public void ScalarSelectionsNotAllowedOnInt()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                       ... fragOnObject
                    }
                }

                fragment fragOnObject on Dog {
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "`barkVolume` is a scalar field. Selections on scalars " +
                    "or enums are never allowed, because they are the leaf " +
                    "nodes of any GraphQL query."));
        }
    }
}
