using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Execution.Validation
{
    public class ExecutableDefinitionsValidatorTests
    {
        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
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
            ExecutableDefinitionsValidator validator =
                new ExecutableDefinitionsValidator();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "A document containing TypeSystemDefinition " +
                    "is invalid for execution.", t.Message));
        }

        [Fact]
        public void QueryWithoutTypeSystemDefinitions()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            DocumentNode query = Parser.Default.Parse(@"
                query getDogName {
                    dog {
                        name
                        color
                    }
                }
            ");

            // act
            ExecutableDefinitionsValidator validator =
                new ExecutableDefinitionsValidator();
            QueryValidationResult result = validator.Validate(schema, query);

            // assert
            Assert.False(result.HasErrors);
            Assert.Empty(result.Errors);
        }
    }

    public static class ValidationUtils
    {
        public static Schema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterQueryType<QueryType>();
                c.RegisterType<AlientType>();
                c.RegisterType<CatOrDogType>();
                c.RegisterType<CatType>();
                c.RegisterType<DogOrHumanType>();
                c.RegisterType<DogType>();
                c.RegisterType<HumanOrAlienType>();
                c.RegisterType<HumanType>();
                c.RegisterType<PetType>();
            });
        }
    }
}
