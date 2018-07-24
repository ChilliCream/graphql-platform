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

        [Fact]
        public void ConflictingBecauseAlias()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment conflictingBecauseAlias on Dog {
                    name: nickname
                    name
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

        [Fact]
        public void MergeIdenticalFieldsWithIdenticalArgs()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment mergeIdenticalFieldsWithIdenticalArgs on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand(dogCommand: SIT)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void MergeIdenticalFieldsWithIdenticalValues()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment mergeIdenticalFieldsWithIdenticalValues on Dog {
                    doesKnowCommand(dogCommand: $dogCommand)
                    doesKnowCommand(dogCommand: $dogCommand)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ConflictingArgsOnValues()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment conflictingArgsOnValues on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand(dogCommand: HEEL)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

        [Fact]
        public void ConflictingArgsValueAndVar()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment conflictingArgsValueAndVar on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand(dogCommand: $dogCommand)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

        [Fact]
        public void ConflictingArgsWithVars()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment conflictingArgsWithVars on Dog {
                    doesKnowCommand(dogCommand: $varOne)
                    doesKnowCommand(dogCommand: $varTwo)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

        [Fact]
        public void DifferingArgs()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                fragment differingArgs on Dog {
                    doesKnowCommand(dogCommand: SIT)
                    doesKnowCommand
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        "The query has non-mergable fields.",
                        t.Message));
        }

    }
}
