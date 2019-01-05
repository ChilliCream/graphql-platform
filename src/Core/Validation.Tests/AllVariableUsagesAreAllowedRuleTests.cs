using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class AllVariableUsagesAreAllowedRuleTests
        : ValidationTestBase
    {
        public AllVariableUsagesAreAllowedRuleTests()
            : base(new AllVariableUsagesAreAllowedRule())
        {
        }

        [Fact]
        public void IntCannotGoIntoBoolean()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query intCannotGoIntoBoolean($intArg: Int) {
                    arguments {
                        booleanArgField(booleanArg: $intArg)
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The variable `intArg` type is not " +
                    "compatible with the type of the " +
                    "argument `booleanArg`." +
                    "\r\nExpected type: `Boolean`.",
                    t.Message));
        }

        [Fact]
        public void BooleanListCannotGoIntoBoolean()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query booleanListCannotGoIntoBoolean($booleanListArg: [Boolean]) {
                    arguments {
                        booleanArgField(booleanArg: $booleanListArg)
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The variable `booleanListArg` type is not " +
                    "compatible with the type of the " +
                    "argument `booleanArg`." +
                    "\r\nExpected type: `Boolean`.",
                    t.Message));
        }

        [Fact]
        public void BooleanArgQuery()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query booleanArgQuery($booleanArg: Boolean) {
                    arguments {
                        nonNullBooleanArgField(nonNullBooleanArg: $booleanArg)
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The variable `booleanArg` type is not " +
                    "compatible with the type of the " +
                    "argument `nonNullBooleanArg`." +
                    "\r\nExpected type: `Boolean`.",
                    t.Message));
        }

        [Fact]
        public void NonNullListToList()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query nonNullListToList($nonNullBooleanList: [Boolean]!) {
                    arguments {
                        booleanListArgField(booleanListArg: $nonNullBooleanList)
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ListToNonNullList()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query listToNonNullList($booleanList: [Boolean]) {
                    arguments {
                        nonNullBooleanListField(nonNullBooleanListArg: $booleanList)
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The variable `booleanList` type is not " +
                    "compatible with the type of the " +
                    "argument `nonNullBooleanListArg`." +
                    "\r\nExpected type: `Boolean`.",
                    t.Message));
        }

        [Fact]
        public void BooleanArgQueryWithDefault1()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query booleanArgQueryWithDefault($booleanArg: Boolean) {
                    arguments {
                        optionalNonNullBooleanArgField(optionalBooleanArg: $booleanArg)
                    }
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void BooleanArgQueryWithDefault2()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query booleanArgQueryWithDefault($booleanArg: Boolean = true) {
                    arguments {
                        nonNullBooleanArgField(nonNullBooleanArg: $booleanArg)
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
