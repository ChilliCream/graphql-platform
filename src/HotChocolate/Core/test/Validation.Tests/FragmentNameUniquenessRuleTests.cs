using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public class FragmentNameUniquenessRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public FragmentNameUniquenessRuleTests()
            : base(services => services.AddFragmentsAreValidRule())
        {
        }

        [Fact]
        public void UniqueFragments()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...fragmentOne
                        ...fragmentTwo
                    }
                }

                fragment fragmentOne on Dog {
                    name
                }

                fragment fragmentTwo on Dog {
                    owner {
                        name
                    }
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Empty(context.Errors);
        }

        [Fact]
        public void DuplicateFragments()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                {
                    dog {
                        ...fragmentOne
                    }
                }

                fragment fragmentOne on Dog {
                    name
                }

                fragment fragmentOne on Dog {
                    owner {
                        name
                    }
                }
            ");
            context.Prepare(query);

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "There are multiple fragments with the name `fragmentOne`.",
                    t.Message));
            context.Errors.First().MatchSnapshot();
        }
    }
}
