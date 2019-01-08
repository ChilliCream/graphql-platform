using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class MaxComplexityRuleTests
    {
        [InlineData(9, false)]
        [InlineData(8, false)]
        [InlineData(7, true)]
        [Theory]
        public void MaxComplexityReached(
            int maxAllowedComplexity,
            bool hasErrors)
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    foo {
                        ... on Foo {
                            ... on Foo {
                                field
                                ... on Bar {
                                    baz {
                                        foo {
                                            field
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            ");

            ISchema schema = CreateSchema();

            var rule = new MaxComplexityRule(new QueryExecutionOptions
            {
                MaxOperationComplexity = maxAllowedComplexity
            }, null);

            // act
            QueryValidationResult result = rule.Validate(schema, query);

            // assert
            Assert.Equal(hasErrors, result.HasErrors);
            result.Snapshot("MaxComplexityReached_" + maxAllowedComplexity);
        }

        private ISchema CreateSchema()
        {
            return Schema.Create(
                FileResource.Open("CostSchema.graphql"),
                c => c.Use(next => context => Task.CompletedTask));
        }
    }
}
