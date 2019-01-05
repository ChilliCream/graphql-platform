using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class ValuesOfCorrectTypeRuleTests
        : ValidationTestBase
    {
        public ValuesOfCorrectTypeRuleTests()
            : base(new ValuesOfCorrectTypeRule())
        {
        }

        [Fact]
        public void GoodBooleanArg()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    arguments {
                        ...goodBooleanArg
                    }
                }

                fragment goodBooleanArg on Arguments {
                    booleanArgField(booleanArg: true)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void CoercedIntIntoFloatArg()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    arguments {
                        ...coercedIntIntoFloatArg
                    }
                }

                fragment coercedIntIntoFloatArg on Arguments {
                    # Note: The input coercion rules for Float allow Int literals.
                    floatArgField(floatArg: 123)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void GoodComplexDefaultValue()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query goodComplexDefaultValue($search: ComplexInput = { name: ""Fido"" }) {
                    findDog(complex: $search)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void StringIntoInt()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                {
                    arguments {
                        ...stringIntoInt
                    }
                }

                fragment stringIntoInt on Arguments {
                    intArgField(intArg: ""123"")
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified value type of argument `intArg` " +
                    "does not match the argument type.",
                    t.Message));
        }

        [Fact]
        public void BadComplexValueArgument()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query badComplexValue {
                    findDog(complex: { name: 123 })
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified value type of field `name` " +
                    "does not match the field type.",
                    t.Message));
        }

        [Fact]
        public void BadComplexValueVariable()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query goodComplexDefaultValue($search: ComplexInput = { name: 123 }) {
                    findDog(complex: $search)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified value type of field `name` " +
                    "does not match the field type.",
                    t.Message));
        }

        [Fact]
        public void BadValueVariable()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query goodComplexDefaultValue($search: ComplexInput = 123) {
                    findDog(complex: $search)
                }
            ");

            // act
            QueryValidationResult result = Rule.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified value type of variable `search` " +
                    "does not match the variable type.",
                    t.Message));
        }
    }
}
