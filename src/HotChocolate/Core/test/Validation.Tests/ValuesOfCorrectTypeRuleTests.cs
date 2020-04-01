using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Validation
{
    public class ValuesOfCorrectTypeRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public ValuesOfCorrectTypeRuleTests()
            : base(services => services.AddValuesOfCorrectTypeRule())
        {
        }

        [Fact]
        public void GoodBooleanArg()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    arguments {
                        ...goodBooleanArg
                    }
                }

                fragment goodBooleanArg on Arguments {
                    booleanArgField(booleanArg: true)
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.False(context.IsInError);
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void GoodBooleanListArg()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query queryWithListInput()
                {
                    booleanList(booleanListArg: [ true, false ])
                }");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.False(context.IsInError);
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void GoodBooleanListVariableArg()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query queryWithListInput($value: Boolean!)
                {
                    booleanList(booleanListArg: [ true, $value ])
                }");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.False(context.IsInError);
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void BadBooleanListNonNullVariableArg()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query queryWithListInput($value: Boolean)
                {
                    booleanList(booleanListArg: [ true, $value ])
                }");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.False(context.IsInError);
            Assert.Collection(context.Errors,
                t =>
                {
                    Assert.Equal(
                        "The specified argument value does not" +
                        " match the argument type.",
                        t.Message);
                    Assert.Equal("Boolean!", t.Extensions["locationType"]);
                    Assert.Equal("booleanListArg", t.Extensions["argument"]);
                });
        }

        [Fact]
        public void BadBooleanListArg()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query queryWithListInput()
                {
                    booleanList(booleanListArg: [ true, ""false"" ])
                }");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.False(context.IsInError);
            Assert.Collection(context.Errors,
                t =>
                {
                    Assert.Equal(
                        "The specified argument value does not" +
                        " match the argument type.",
                        t.Message);
                    Assert.Equal("Boolean!", t.Extensions["locationType"]);
                    Assert.Equal("booleanListArg", t.Extensions["argument"]);
                });
        }

        [Fact]
        public void BadBooleanListArgString()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query queryWithListInput()
                {
                    booleanList(booleanListArg:  ""false"" )
                }");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.False(context.IsInError);
            Assert.Collection(context.Errors,
                t =>
                {
                    Assert.Equal(
                        "The specified argument value does not" +
                        " match the argument type.",
                        t.Message);
                    Assert.Equal("[Boolean!]", t.Extensions["locationType"]);
                    Assert.Equal("booleanListArg", t.Extensions["argument"]);
                });
        }

        [Fact]
        public void CoercedIntIntoFloatArg()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
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
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.False(context.IsInError);
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void GoodComplexDefaultValue()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query goodComplexDefaultValue($search: ComplexInput = { name: ""Fido"" }) {
                    findDog(complex: $search)
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.False(context.IsInError);
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void StringIntoInt()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    arguments {
                        ...stringIntoInt
                    }
                }

                fragment stringIntoInt on Arguments {
                    intArgField(intArg: ""123"")
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The specified argument value does not match the " +
                    "argument type.",
                    t.Message));
        }

        [Fact]
        public void BadComplexValueArgument()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query badComplexValue {
                    findDog(complex: { name: 123 })
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The specified value type of field `name` " +
                    "does not match the field type.",
                    t.Message));
        }

        [Fact]
        public void BadVariableValueComplexArgument()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query badVariableValue($badArg: Int) {
                    findDog(complex: { name: $badArg })
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The specified value type of field `name` " +
                    "does not match the field type.",
                    t.Message));
        }

        [Fact]
        public void BadVariableValueArgument()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query badVariableValue($badArg: String) {
                    arguments {
                       intArgField(intArg: $badArg )
                    }
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The specified argument value " +
                    "does not match the argument type.",
                    t.Message));
        }

        [Fact]
        public void BadComplexValueVariable()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query goodComplexDefaultValue($search: ComplexInput = { name: 123 }) {
                    findDog(complex: $search)
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The specified value type of field `name` " +
                    "does not match the field type.",
                    t.Message));
        }

        [Fact]
        public void BadValueVariable()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query goodComplexDefaultValue($search: ComplexInput = 123) {
                    findDog(complex: $search)
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The specified value type of variable `search` " +
                    "does not match the variable type.",
                    t.Message));
        }
    }
}
