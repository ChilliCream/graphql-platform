using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Validation
{
    public class QueryValidatorTests
    {
        [Fact]
        public void QueryIsNull()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            // act
            Action a = () => queryValidator.Validate(schema, null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void SchemaIsNull()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            // act
            Action a = () => queryValidator.Validate(null,
                new DocumentNode(null, new List<IDefinitionNode>()));

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            // arrange
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

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "A document containing TypeSystemDefinition " +
                    "is invalid for execution.", t.Message),
                t => Assert.Equal(
                    "The field `color` does not exist " +
                    "on the type `Dog`.", t.Message));
        }

        [Fact]
        public void QueryWithOneAnonymousAndOneNamedOperation()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        name
                    }
                }

                query getName {
                    dog {
                        owner {
                            name
                        }
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t =>
                {
                    Assert.Equal(
                        "GraphQL allows a short‐hand form for defining query " +
                        "operations when only that one operation exists in " +
                        "the document.", t.Message);
                });
        }

        [Fact]
        public void TwoQueryOperationsWithTheSameName()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                query getName {
                    dog {
                        name
                    }
                }

                query getName {
                    dog {
                        owner {
                            name
                        }
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                        $"The operation name `getName` is not unique.",
                        t.Message));
        }

        [Fact]
        public void OperationWithTwoVariablesThatHaveTheSameName()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                query houseTrainedQuery(
                    $atOtherHomes: Boolean, $atOtherHomes: Boolean) {
                    dog {
                        isHousetrained(atOtherHomes: $atOtherHomes)
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "A document containing operations that " +
                    "define more than one variable with the same " +
                    "name is invalid for execution.", t.Message),
                t => Assert.Equal(
                    "The field `isHousetrained` does not exist " +
                    "on the type `Dog`.", t.Message));
        }

        [Fact]
        public void DuplicateArgument()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                fragment goodNonNullArg on Arguments {
                    nonNullBooleanArgField(
                        nonNullBooleanArg: true, nonNullBooleanArg: true)
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"Arguments are not unique.", t.Message),
                t => Assert.Equal(
                    "The specified fragment `goodNonNullArg` " +
                    "is not used within the current document.",
                    t.Message));
        }

        [Fact]
        public void MissingRequiredArgNonNullBooleanArg()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                fragment missingRequiredArg on Arguments {
                    nonNullBooleanArgField(nonNullBooleanArg: null)
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"The argument `nonNullBooleanArg` is required " +
                    "and does not allow null values.", t.Message),
                t => Assert.Equal(
                    "The specified fragment `missingRequiredArg` " +
                    "is not used within the current document.",
                    t.Message));
        }

        [Fact]
        public void DisallowedSecondRootField()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                subscription sub {
                    newMessage {
                        body
                        sender
                    }
                    disallowedSecondRootField
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    $"Subscription operation `sub` must " +
                    "have exactly one root field.", t.Message),
                t => Assert.Equal(
                    "The field `disallowedSecondRootField` does not exist " +
                    "on the type `Subscription`.", t.Message));
        }

        [Fact]
        public void FieldIsNotDefinedOnTypeInFragment()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                fragment fieldNotDefined on Dog {
                    meowVolume
                }

                fragment aliasedLyingFieldTargetNotDefined on Dog {
                    barkVolume: kawVolume
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The field `meowVolume` does not exist " +
                    "on the type `Dog`.", t.Message),
                t => Assert.Equal(
                    "The field `kawVolume` does not exist " +
                    "on the type `Dog`.", t.Message),
                t => Assert.Equal(
                    "The specified fragment `fieldNotDefined` " +
                    "is not used within the current document.",
                    t.Message),
                t => Assert.Equal(
                    "The specified fragment " +
                    "`aliasedLyingFieldTargetNotDefined` " +
                    "is not used within the current document.",
                    t.Message));
        }

        [Fact]
        public void VariableNotUsedWithinFragment()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                query variableNotUsedWithinFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHousetrainedWithoutVariableFragment
                    }
                }

                fragment isHousetrainedWithoutVariableFragment on Dog {
                    barkVolume
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The following variables were not used: " +
                    "atOtherHomes.", t.Message));
        }

        [Fact]
        public void SkipDirectiveIsInTheWrongPlace()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                query @skip(if: $foo) {
                    field
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The field `field` does not exist " +
                    "on the type `Query`.", t.Message),
                t => Assert.Equal(
                    "The specified directive is not valid the " +
                    "current location.", t.Message));
        }

        [Fact]
        public void QueriesWithInvalidVariableTypes()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                query takesCat($cat: Cat) {
                    # ...
                }

                query takesDogBang($dog: Dog!) {
                    # ...
                }

                query takesListOfPet($pets: [Pet]) {
                    # ...
                }

                query takesCatOrDog($catOrDog: CatOrDog) {
                    # ...
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The following variables were not used: cat.",
                    t.Message),
                t => Assert.Equal(
                    "The following variables were not used: dog.",
                    t.Message),
                t => Assert.Equal(
                    "The following variables were not used: pets.",
                    t.Message),
                t => Assert.Equal(
                    "The following variables were not used: catOrDog.",
                    t.Message),
                t => Assert.Equal(
                    "The type of variable `cat` is not an input type.",
                    t.Message),
                t => Assert.Equal(
                    "The type of variable `dog` is not an input type.",
                    t.Message),
                t => Assert.Equal(
                    "The type of variable `pets` is not an input type.",
                    t.Message),
                t => Assert.Equal(
                    "The type of variable `catOrDog` is not an input type.",
                    t.Message));
        }

        [Fact]
        public void ConflictingBecauseAlias()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                fragment conflictingBecauseAlias on Dog {
                    name: nickname
                    name
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The query has non-mergable fields.",
                    t.Message),
                t => Assert.Equal(
                    "The specified fragment `conflictingBecauseAlias` " +
                    "is not used within the current document.",
                    t.Message));
        }

        [Fact]
        public void InvalidFieldArgName()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                fragment invalidArgName on Dog {
                    doesKnowCommand(command: CLEAN_UP_HOUSE)
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The argument `dogCommand` is required and does not " +
                    "allow null values.",
                    t.Message),
                t => Assert.Equal(
                    "The argument `command` does not exist.", t.Message),
                t => Assert.Equal(
                    "The specified fragment `invalidArgName` " +
                    "is not used within the current document.",
                    t.Message));
        }

        [Fact]
        public void UnusedFragment()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                fragment nameFragment on Dog { # unused
                    name
                }

                {
                    dog {
                        name
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified fragment `nameFragment` " +
                    "is not used within the current document.", t.Message));
        }

        [Fact]
        public void DuplicateFragments()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
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

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "There are multiple fragments with the name `fragmentOne`.",
                    t.Message));
        }

        [Fact]
        public void ScalarSelectionsNotAllowedOnInt()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        barkVolume {
                            sinceWhen
                        }
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "`barkVolume` is a scalar field. Selections on scalars " +
                    "or enums are never allowed, because they are the leaf " +
                    "nodes of any GraphQL query."));
        }

        [Fact]
        public void InlineFragOnScalar()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                       ... inlineFragOnScalar
                    }
                }

                fragment inlineFragOnScalar on Dog {
                    ... on Boolean {
                        somethingElse
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "Fragments can only be declared on unions, interfaces, " +
                    "and objects."));
        }

        [Fact]
        public void FragmentCycle1()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...nameFragment
                    }
                }

                fragment nameFragment on Dog {
                    name
                    ...barkVolumeFragment
                }

                fragment barkVolumeFragment on Dog {
                    barkVolume
                    ...nameFragment
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(t.Message,
                    "The graph of fragment spreads must not form any " +
                    "cycles including spreading itself. Otherwise an " +
                    "operation could infinitely spread or infinitely " +
                    "execute on cycles in the underlying data."));
        }

        [Fact]
        public void UndefinedFragment()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...undefinedFragment
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified fragment `undefinedFragment` " +
                    "does not exist.",
                    t.Message));
        }

        [Fact]
        public void FragmentDoesNotMatchType()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...fragmentDoesNotMatchType
                    }
                }

                fragment fragmentDoesNotMatchType on Human {
                    name
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The parent type does not match the type condition on " +
                    "the fragment `fragmentDoesNotMatchType`.",
                    t.Message));
        }

        [Fact]
        public void NotExistingTypeOnInlineFragment()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        ...inlineNotExistingType
                    }
                }

                fragment inlineNotExistingType on Dog {
                    ... on NotInSchema {
                        name
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified inline fragment " +
                    "does not exist in the current schema.",
                    t.Message));
        }

        [Fact]
        public void InvalidInputObjectFieldsExist()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog(complex: { favoriteCookieFlavor: ""Bacon"" })
                    {
                        name
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified input object field " +
                    "`favoriteCookieFlavor` does not exist.",
                    t.Message));
        }

        [Fact]
        public void RequiredFieldIsNull()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog2(complex: { name: null })
                    {
                        name
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "`name` is a required field and cannot be null.",
                    t.Message));
        }

        [Fact]
        public void NameFieldIsAmbiguous()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    findDog(complex: { name: ""A"", name: ""B"" })
                    {
                        name
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "Field `name` is ambiguous.",
                    t.Message));
        }

        [Fact]
        public void UnsupportedDirective()
        {
            // arrange
            DocumentNode query = Parser.Default.Parse(@"
                {
                    dog {
                        name @foo(bar: true)
                    }
                }
            ");

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified directive `foo` " +
                    "is not supported by the current schema.",
                    t.Message));
        }

        [Fact]
        public void StringIntoInt()
        {
            // arrange
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

            Schema schema = ValidationUtils.CreateSchema();
            var queryValidator = CreateValidator();

            // act
            QueryValidationResult result =
                queryValidator.Validate(schema, query);

            // assert
            Assert.True(result.HasErrors);
            Assert.Collection(result.Errors,
                t => Assert.Equal(
                    "The specified value type of argument `intArg` " +
                    "does not match the argument type.",
                    t.Message));
        }


        private IQueryValidator CreateValidator()
        {
            var services = new ServiceCollection();
            services.AddQueryValidation();
            services.AddDefaultValidationRules();
            return services.BuildServiceProvider()
                .GetRequiredService<IQueryValidator>();
        }
    }


}
