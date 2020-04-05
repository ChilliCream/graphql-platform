using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation
{
    public class ExecutableDefinitionsRuleTests
        : DocumentValidatorVisitorTestBase
    {
        public ExecutableDefinitionsRuleTests()
            : base(services => services.AddExecutableDefinitionsRule())
        {
        }

        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query getDogName {
                    dog {
                        name
                        color
                    }
                }

                extend type Dog {
                    color: String
                }
            ");

            // act
            Rule.Validate(context, query);

            // assert
            Assert.Collection(context.Errors,
                t => Assert.Equal(
                    "A document containing TypeSystemDefinition " +
                    "is invalid for execution.", t.Message));
            context.Errors.First().MatchSnapshot();
        }

        [Fact]
        public void QueryWithoutTypeSystemDefinitions()
        {
            // arrange
            IDocumentValidatorContext context = ValidationUtils.CreateContext();
            DocumentNode query = Utf8GraphQLParser.Parse(@"
                query getDogName {
                    dog {
                        name
                        color
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
