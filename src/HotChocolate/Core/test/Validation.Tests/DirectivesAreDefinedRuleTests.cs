using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation
{
    public class DirectivesAreDefinedRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public DirectivesAreDefinedRuleTests()
            : base(services => services.AddDirectivesAreValidRule())
        {
        }

        [Fact]
        public void SupportedDirective()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        name @skip(if: true)
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void UnsupportedDirective()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        name @foo(bar: true)
                    }
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The specified directive `foo` " +
                    "is not supported by the current schema.",
                    t.Message));
            context.Errors.First().MatchSnapshot();
        }

        [Fact]
        public void SkipDirectiveIsInTheWrongPlace()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query @skip(if: $foo) {
                    field
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "The specified directive is not valid the " +
                    "current location.", t.Message));
            context.Errors.First().MatchSnapshot();
        }

        [Fact]
        public void SkipDirectiveIsInTheRightPlace()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query a {
                    field @skip(if: $foo)
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void DuplicateSkipDirectives()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query ($foo: Boolean = true, $bar: Boolean = false) {
                    field @skip(if: $foo) @skip(if: $bar)
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "Only one of each directive is allowed per location.",
                    t.Message));
            context.Errors.First().MatchSnapshot();
        }

        [Fact]
        public void SkipOnTwoDifferentFields()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query ($foo: Boolean = true, $bar: Boolean = false) {
                    field @skip(if: $foo) {
                        subfieldA
                    }
                    field @skip(if: $bar) {
                        subfieldB
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
