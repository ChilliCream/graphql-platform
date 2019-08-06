using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Moq;
using Xunit;

namespace HotChocolate.Validation
{
    public class MaxDepthRuleTests
    {
        public MaxDepthRuleTests()
        {
        }

        [Fact]
        public void MaxDepth3_QueryWith4Levels_MaxDepthReached()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query {
                    level1_1 {
                        level2_1 {
                            level3_1 {
                                level4
                            }
                        }
                    }
                    level1_2
                    level1_2
                    {
                        level2_2
                        level2_3
                        {
                            level3_2
                        }
                    }
                }
            ");

            var options = new Mock<IValidateQueryOptionsAccessor>(
                MockBehavior.Strict);
            options.Setup(t => t.MaxExecutionDepth).Returns(3);

            var rule = new MaxDepthRule(options.Object);

            // act
            QueryValidationResult result = rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t =>
                {
                    Assert.Equal(
                        "The query exceded the maximum allowed execution " +
                        "depth of 3.", t.Message);
                    Assert.Equal(1, t.Locations.Count);
                });
        }

        [Fact]
        public void MaxDepth3_QueryWith4LevelsViaFraments_MaxDepthReached()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query {
                    level1_1 {
                        ... level2
                    }
                }

                fragment level2 on Level2
                {
                    level2_1 {
                        ... level3
                    }
                }

                fragment level3 on Level3
                {
                    level3_1 {
                        ... level4
                    }
                }

                fragment level4 on Level4
                {
                    level4
                }
            ");

            var options = new Mock<IValidateQueryOptionsAccessor>(
                MockBehavior.Strict);
            options.Setup(t => t.MaxExecutionDepth).Returns(3);

            var rule = new MaxDepthRule(options.Object);

            // act
            QueryValidationResult result = rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The query exceded the maximum allowed execution " +
                    "depth of 3.", t.Message));
        }

        [Fact]
        public void MaxDepth3_QueryWith4LevelsWithInlineFragm_MaxDepthReached()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query {
                    level1_1 {
                        ... on Level2 {
                            level2_1 {
                                ... on Level3 {
                                    level3_1
                                    {
                                        level4
                                    }
                                }
                            }
                        }
                    }
                }
            ");

            var options = new Mock<IValidateQueryOptionsAccessor>(
                MockBehavior.Strict);
            options.Setup(t => t.MaxExecutionDepth).Returns(3);

            var rule = new MaxDepthRule(options.Object);

            // act
            QueryValidationResult result = rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t =>
                {
                    Assert.Equal(
                        "The query exceded the maximum allowed execution " +
                        "depth of 3.", t.Message);
                    Assert.Equal(1, t.Locations.Count);
                });
        }

        [Fact]
        public void MaxDepth3_QueryWith3Levels_Valid()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query {
                    level1_1 {
                        level2_1 {
                            level3_1
                        }
                    }
                    level1_2
                    level1_2
                    {
                        level2_2
                        level2_3
                        {
                            level3_2
                        }
                    }
                }
            ");

            var options = new Mock<IValidateQueryOptionsAccessor>(
                MockBehavior.Strict);
            options.Setup(t => t.MaxExecutionDepth).Returns(3);

            var rule = new MaxDepthRule(options.Object);

            // act
            QueryValidationResult result = rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void MaxDepth3_QueryWith3LevelsViaFraments_Valid()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query {
                    level1_1 {
                        ... level2
                    }
                }

                fragment level2 on Level2
                {
                    level2_1 {
                        ... level3
                    }
                }

                fragment level3 on Level3
                {
                    level3_1
                }
            ");

            var options = new Mock<IValidateQueryOptionsAccessor>(
                MockBehavior.Strict);
            options.Setup(t => t.MaxExecutionDepth).Returns(3);

            var rule = new MaxDepthRule(options.Object);

            // act
            QueryValidationResult result = rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void MaxDepth3_QueryWith3LevelsWithInlineFragm_Valid()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query {
                    level1_1 {
                        ... on Level2 {
                            level2_1 {
                                ... on Level3 {
                                    level3_1
                                }
                            }
                        }
                    }
                }
            ");

            var options = new Mock<IValidateQueryOptionsAccessor>(
                MockBehavior.Strict);
            options.Setup(t => t.MaxExecutionDepth).Returns(3);

            var rule = new MaxDepthRule(options.Object);

            // act
            QueryValidationResult result = rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void NoMaxDepth_QueryWith4Levels_Valid()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query {
                    level1_1 {
                        level2_1 {
                            level3_1 {
                                level4
                            }
                        }
                    }
                    level1_2
                    level1_2
                    {
                        level2_2
                        level2_3
                        {
                            level3_2
                        }
                    }
                }
            ");

            var options = new Mock<IValidateQueryOptionsAccessor>(
                MockBehavior.Strict);
            options.Setup(t => t.MaxExecutionDepth)
                .Returns((int?)null);

            var rule = new MaxDepthRule(options.Object);

            // act
            QueryValidationResult result = rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }
    }
}
