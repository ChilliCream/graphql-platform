using System.Linq;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation
{
    public class AllVariableUsagesAreAllowedRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public AllVariableUsagesAreAllowedRuleTests()
            : base(services => services.AddAllVariableUsagesAreAllowedRule())
        {
        }

        [Fact]
        public void IntCannotGoIntoBoolean()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query intCannotGoIntoBoolean($intArg: Int) {
                    arguments {
                        booleanArgField(booleanArg: $intArg)
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The variable `intArg` is not compatible with the " +
                    "type of the current location.",
                    t.Message));
            context.Errors.First().MatchSnapshot();
        }

        [Fact]
        public void BooleanListCannotGoIntoBoolean()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query booleanListCannotGoIntoBoolean($booleanListArg: [Boolean]) {
                    arguments {
                        booleanArgField(booleanArg: $booleanListArg)
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The variable `booleanListArg` is not compatible with the " +
                    "type of the current location.",
                    t.Message));
            context.Errors.First().MatchSnapshot();
        }

        [Fact]
        public void BooleanArgQuery()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query booleanArgQuery($booleanArg: Boolean) {
                    arguments {
                        nonNullBooleanArgField(nonNullBooleanArg: $booleanArg)
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The variable `booleanArg` is not compatible with the " +
                    "type of the current location.",
                    t.Message));
            context.Errors.First().MatchSnapshot();
        }

        [Fact]
        public void NonNullListToList()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query nonNullListToList($nonNullBooleanList: [Boolean]!) {
                    arguments {
                        booleanListArgField(booleanListArg: $nonNullBooleanList)
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void BooleanVariableAsListElement()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query nonNullListToList($b: Boolean) {
                    arguments {
                        booleanListArgField(booleanListArg: [$b])
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void NullableBooleanVariableAsListElement()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query nonNullBooleanListArgField($nullableBoolean: Boolean) {
                    arguments {
                        nonNullBooleanListArgField(booleanListArg: [$nullableBoolean])
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The variable `nullableBoolean` is not compatible with the " +
                    "type of the current location.",
                    t.Message));
            context.Errors.First().MatchSnapshot();
        }

        [Fact]
        public void ListToNonNullList()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query listToNonNullList($booleanList: [Boolean]) {
                    arguments {
                        nonNullBooleanListField(nonNullBooleanListArg: $booleanList)
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The variable `booleanList` is not compatible with the " +
                    "type of the current location.",
                    t.Message));
            context.Errors.First().MatchSnapshot();
        }

        [Fact]
        public void BooleanArgQueryWithDefault1()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query booleanArgQueryWithDefault($booleanArg: Boolean) {
                    arguments {
                        optionalNonNullBooleanArgField(optionalBooleanArg: $booleanArg)
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void BooleanArgQueryWithDefault2()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query booleanArgQueryWithDefault($booleanArg: Boolean = true) {
                    arguments {
                        nonNullBooleanArgField(nonNullBooleanArg: $booleanArg)
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }
    }
}
