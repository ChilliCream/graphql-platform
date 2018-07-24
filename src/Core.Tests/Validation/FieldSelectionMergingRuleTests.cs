using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class FieldSelectionMergingRuleTests
        : ValidationTestBase
    {
        public FieldSelectionMergingRuleTests()
            : base(new FieldSelectionMergingRule())
        {
        }

        [Fact]
        public void MergeIdenticalFields()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment mergeIdenticalFields on Dog {
                    name
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void MergeIdenticalAliasesAndFields()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment mergeIdenticalAliasesAndFields on Dog {
                    otherName: name
                    otherName: name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

    }
}
