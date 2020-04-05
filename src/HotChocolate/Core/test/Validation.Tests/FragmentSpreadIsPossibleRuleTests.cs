using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentSpreadIsPossibleRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public FragmentSpreadIsPossibleRuleTests()
            : base(services => services.AddFragmentsAreValidRule())
        {
        }

        [Fact]
        public void FragmentDoesNotMatchType()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...fragmentDoesNotMatchType
                    }
                }

                fragment fragmentDoesNotMatchType on Human {
                    name
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(t.Message,
                    "The parent type does not match the type condition on " +
                    "the fragment."));
            context.Errors.MatchSnapshot();
        }

        [Fact]
        public void InterfaceTypeDoesMatch()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...interfaceTypeDoesMatch
                    }
                }

                fragment interfaceTypeDoesMatch on Pet {
                    name
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void UnionTypeDoesMatch()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...unionTypeDoesMatch
                    }
                }

                fragment unionTypeDoesMatch on CatOrDog {
                    name
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void ObjectTypeDoesMatch()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...objectTypeDoesMatch
                    }
                }

                fragment objectTypeDoesMatch on Dog {
                    name
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }
    }
}
