using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.StarWars;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Validation
{
    public class DocumentValidatorTests
    {
        [Fact]
        public void DocumentIsNull()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            IDocumentValidator queryValidator = CreateValidator();

            // act
            Action a = () => queryValidator.Validate(schema, null!);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void SchemaIsNull()
        {
            // arrange
            Schema schema = ValidationUtils.CreateSchema();
            IDocumentValidator queryValidator = CreateValidator();

            // act
            // act
            Action a = () => queryValidator.Validate(null!,
                new DocumentNode(null, new List<IDefinitionNode>()));

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void QueryWithTypeSystemDefinitions()
        {
            ExpectErrors(@"
                query getDogName {
                    dog {
                        name
                        color
                    }
                }

                extend type Dog {
                    color: String
                }
            ",
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
            ExpectErrors(@"
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
            ",
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
            ExpectErrors(@"
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
            ",
            t => Assert.Equal(
                    $"The operation name `getName` is not unique.",
                    t.Message));
        }

        [Fact]
        public void OperationWithTwoVariablesThatHaveTheSameName()
        {
            ExpectErrors(@"
                query houseTrainedQuery(
                    $atOtherHomes: Boolean, $atOtherHomes: Boolean) {
                    dog {
                        isHouseTrained(atOtherHomes: $atOtherHomes)
                    }
                }
            ",
            t => Assert.Equal(
                "A document containing operations that " +
                "define more than one variable with the same " +
                "name is invalid for execution.", t.Message));
        }

        [Fact]
        public void DuplicateArgument()
        {
            ExpectErrors(@"
                {
                    arguments {
                        ... goodNonNullArg
                    }
                }
                fragment goodNonNullArg on Arguments {
                    nonNullBooleanArgField(
                        nonNullBooleanArg: true, nonNullBooleanArg: true)
                }
            ",
            t => Assert.Equal(
                $"More than one argument with the same name in an argument set " +
                "is ambiguous and invalid.", t.Message));
        }

        [Fact]
        public void MissingRequiredArgNonNullBooleanArg()
        {
            ExpectErrors(@"
                {
                    arguments {
                        ... missingRequiredArg
                    }
                }

                fragment missingRequiredArg on Arguments {
                    nonNullBooleanArgField(nonNullBooleanArg: null)
                }
            ",
                t => Assert.Equal(
                    "The argument `nonNullBooleanArg` is required.",
                    t.Message));
        }

        [Fact]
        public void DisallowedSecondRootField()
        {
            ExpectErrors(@"
                subscription sub {
                    newMessage {
                        body
                        sender
                    }
                    disallowedSecondRootFieldNonExisting
                }
            ",
            t => Assert.Equal(
                $"Subscription operations must have exactly one root field.",
                t.Message),
            t => Assert.Equal(
                "The field `disallowedSecondRootFieldNonExisting` does not exist " +
                "on the type `Subscription`.", t.Message));
        }

        [Fact]
        public void FieldIsNotDefinedOnTypeInFragment()
        {
            ExpectErrors(@"
                {
                    dog {
                        ... fieldNotDefined
                        ... aliasedLyingFieldTargetNotDefined
                    }
                }

                fragment fieldNotDefined on Dog {
                    meowVolume
                }

                fragment aliasedLyingFieldTargetNotDefined on Dog {
                    barkVolume: kawVolume
                }
            ",
            t => Assert.Equal(
                "The field `meowVolume` does not exist " +
                "on the type `Dog`.", t.Message),
            t => Assert.Equal(
                "The field `kawVolume` does not exist " +
                "on the type `Dog`.", t.Message));
        }

        [Fact]
        public void VariableNotUsedWithinFragment()
        {
            ExpectErrors(@"
                query variableNotUsedWithinFragment($atOtherHomes: Boolean) {
                    dog {
                        ...isHouseTrainedWithoutVariableFragment
                    }
                }

                fragment isHouseTrainedWithoutVariableFragment on Dog {
                    barkVolume
                }
            ",
            t => Assert.Equal(
                "The following variables were not used: " +
                "atOtherHomes.", t.Message));
        }

        [Fact]
        public void SkipDirectiveIsInTheWrongPlace()
        {
            ExpectErrors(@"
                query @skip(if: $foo) {
                    field
                }
            ");
        }

        [Fact]
        public void QueriesWithInvalidVariableTypes()
        {
            // arrange
            ExpectErrors(@"
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
            ",
            t => Assert.Equal(
                "Operation `takesCat` has a empty selection set. Root types without subfields " +
                "are disallowed.",
                t.Message),
            t => Assert.Equal(
                "Operation `takesDogBang` has a empty selection set. Root types without " +
                "subfields are disallowed.",
                t.Message),
            t => Assert.Equal(
                "Operation `takesListOfPet` has a empty selection set. Root types without " +
                "subfields are disallowed.",
                t.Message),
            t => Assert.Equal(
                "Operation `takesCatOrDog` has a empty selection set. Root types without " +
                "subfields are disallowed.",
                t.Message),
            t => Assert.Equal(
                "The type of variable `cat` is not an input type.",
                t.Message),
            t => Assert.Equal(
                "The following variables were not used: cat.",
                t.Message),
            t => Assert.Equal(
                "The type of variable `dog` is not an input type.",
                t.Message),
            t => Assert.Equal(
                "The following variables were not used: dog.",
                t.Message),
            t => Assert.Equal(
                "The type of variable `pets` is not an input type.",
                t.Message),
            t => Assert.Equal(
                "The following variables were not used: pets.",
                t.Message),
            t => Assert.Equal(
                "The type of variable `catOrDog` is not an input type.",
                t.Message),
            t => Assert.Equal(
                "The following variables were not used: catOrDog.",
                t.Message));
        }

        [Fact]
        public void ConflictingBecauseAlias()
        {
            ExpectErrors(@"
                fragment conflictingBecauseAlias on Dog {
                    name: nickname
                    name
                }
            ",
                t => Assert.Equal(
                "The specified fragment `conflictingBecauseAlias` " +
                "is not used within the current document.",
                t.Message));
        }

        [Fact]
        public void InvalidFieldArgName()
        {
            ExpectErrors(@"
                {
                    dog {
                        ... invalidArgName
                    }
                }

                fragment invalidArgName on Dog {
                    doesKnowCommand(command: CLEAN_UP_HOUSE)
                }
            ",
            t => Assert.Equal(
                "The argument `command` does not exist.", t.Message),
            t => Assert.Equal(
                "The argument `dogCommand` is required.",
                t.Message));
        }

        [Fact]
        public void UnusedFragment()
        {
            ExpectErrors(@"
                fragment nameFragment on Dog { # unused
                    name
                }

                {
                    dog {
                        name
                    }
                }
            ",
            t => Assert.Equal(
                "The specified fragment `nameFragment` " +
                "is not used within the current document.", t.Message));
        }

        [Fact]
        public void DuplicateFragments()
        {
            ExpectErrors(@"
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
            ",
            t => Assert.Equal(
                "There are multiple fragments with the name `fragmentOne`.",
                t.Message));
        }

        [Fact]
        public void ScalarSelectionsNotAllowedOnInt()
        {
            ExpectErrors(@"
                {
                    dog {
                        barkVolume {
                            sinceWhen
                        }
                    }
                }
            ",
            t => Assert.Equal(
                "`barkVolume` returns a scalar value. Selections on scalars or enums" +
                " are never allowed, because they are the leaf nodes of any GraphQL query.",
                t.Message));
        }

        [Fact]
        public void InlineFragOnScalar()
        {
            ExpectErrors(@"
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
            ",
            t => Assert.Equal(t.Message,
                "Fragments can only be declared on unions, interfaces, " +
                "and objects."));
        }

        [Fact]
        public void FragmentCycle1()
        {
            ExpectErrors(@"
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
            ",
            t => Assert.Equal(t.Message,
                "The graph of fragment spreads must not form any " +
                "cycles including spreading itself. Otherwise an " +
                "operation could infinitely spread or infinitely " +
                "execute on cycles in the underlying data."));
        }

        [Fact]
        public void UndefinedFragment()
        {
            ExpectErrors(@"
                {
                    dog {
                        ...undefinedFragment
                    }
                }
            ",
            t => Assert.Equal(
                "The specified fragment `undefinedFragment` " +
                "does not exist.",
                t.Message));
        }

        [Fact]
        public void FragmentDoesNotMatchType()
        {
            ExpectErrors(@"
                {
                    dog {
                        ...fragmentDoesNotMatchType
                    }
                }

                fragment fragmentDoesNotMatchType on Human {
                    name
                }
            ",
            t => Assert.Equal(
                "The parent type does not match the type condition on " +
                "the fragment.",
                t.Message));
        }

        [Fact]
        public void NotExistingTypeOnInlineFragment()
        {
            ExpectErrors(@"
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
            ",
            t =>
            {
                Assert.Equal(
                    "Unknown type `NotInSchema`.",
                    t.Message);
            });
        }

        [Fact]
        public void InvalidInputObjectFieldsExist()
        {
            ExpectErrors(@"
                {
                    findDog(complex: { favoriteCookieFlavor: ""Bacon"" })
                    {
                        name
                    }
                }
            ",
            t => Assert.Equal(
                "The specified input object field " +
                "`favoriteCookieFlavor` does not exist.",
                t.Message));
        }

        [Fact]
        public void RequiredFieldIsNull()
        {
            ExpectErrors(@"
                {
                    findDog2(complex: { name: null })
                    {
                        name
                    }
                }
            ",
            t => Assert.Equal(
                "`name` is a required field and cannot be null.",
                t.Message));
        }

        [Fact]
        public void NameFieldIsAmbiguous()
        {
            ExpectErrors(@"
                {
                    findDog(complex: { name: ""A"", name: ""B"" })
                    {
                        name
                    }
                }
            ",
            t =>
                Assert.Equal("There can be only one input field named `name`.", t.Message));
        }

        [Fact]
        public void UnsupportedDirective()
        {
            ExpectErrors(@"
                {
                    dog {
                        name @foo(bar: true)
                    }
                }
            ",
            t => Assert.Equal(
                "The specified directive `foo` " +
                "is not supported by the current schema.",
                t.Message));
        }

        [Fact]
        public void StringIntoInt()
        {
            ExpectErrors(@"
                {
                    arguments {
                        ...stringIntoInt
                    }
                }

                fragment stringIntoInt on Arguments {
                    intArgField(intArg: ""123"")
                }
            ",
            t => Assert.Equal(
                "The specified argument value does not match the " +
                "argument type.",
                t.Message));
        }

        [Fact]
        public void MaxDepthRuleIsIncluded()
        {
            ExpectErrors(
                null,
                new ServiceCollection()
                    .AddValidation()
                    .AddMaxExecutionDepthRule(1)
                    .Services
                    .BuildServiceProvider()
                    .GetRequiredService<IDocumentValidatorFactory>()
                    .CreateValidator(),
                @"
                    query {
                        catOrDog
                        {
                            ... on Cat {
                                name
                            }
                        }
                    }
                ",
                t =>
                {
                    Assert.Equal(
                        "The GraphQL document has an operation complexity of 2 " +
                        "which exceeds the max allowed operation complexity of 1.",
                        t.Message);
                });
        }

        [Fact]
        public void GoodBooleanArgDefault2()
        {
            ExpectValid(@"
                query {
                    arguments {
                        ... goodBooleanArgDefault
                    }
                }

                fragment goodBooleanArgDefault on Arguments {
                    optionalNonNullBooleanArgField2
                }
            ");
        }

        [Fact]
        public void StarWars_Query_Is_Valid()
        {
            ExpectValid(
                SchemaBuilder.New().AddStarWarsTypes().Create(),
                null,
                FileResource.Open("StarWars_Request.graphql"));
        }

        [Fact]
        public void DuplicatesWillBeIgnoredOnFieldMerging()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddStarWarsTypes()
                .Create();

            DocumentNode document = Utf8GraphQLParser.Parse(
                FileResource.Open("InvalidIntrospectionQuery.graphql"));

            var originalOperation = ((OperationDefinitionNode)document.Definitions[0]);
            OperationDefinitionNode operationWithDuplicates = originalOperation.WithSelectionSet(
                originalOperation.SelectionSet.WithSelections(
                    new List<ISelectionNode>
                    {
                        originalOperation.SelectionSet.Selections[0],
                        originalOperation.SelectionSet.Selections[0]
                    }));

            document = document.WithDefinitions(
                new List<IDefinitionNode>(document.Definitions.Skip(1))
                {
                    operationWithDuplicates
                });

            ServiceProvider services = new ServiceCollection()
                .AddValidation()
                .Services
                .BuildServiceProvider();

            IDocumentValidatorFactory factory = services.GetRequiredService<IDocumentValidatorFactory>();
            IDocumentValidator validator = factory.CreateValidator();

            // act
            DocumentValidatorResult result = validator.Validate(schema, document);

            // assert
            Assert.False(result.HasErrors);
        }

        private void ExpectValid(string sourceText) => ExpectValid(null, null, sourceText);

        private void ExpectValid(ISchema schema, IDocumentValidator validator, string sourceText)
        {
            // arrange
            schema ??= ValidationUtils.CreateSchema();
            validator ??= CreateValidator();
            DocumentNode query = Utf8GraphQLParser.Parse(sourceText);

            // act
            DocumentValidatorResult result = validator.Validate(schema, query);

            // assert
            Assert.Empty(result.Errors);
        }

        private void ExpectErrors(string sourceText, params Action<IError>[] elementInspectors) =>
            ExpectErrors(null, null, sourceText, elementInspectors);

        private void ExpectErrors(
            ISchema schema,
            IDocumentValidator validator,
            string sourceText,
            params Action<IError>[] elementInspectors)
        {
            // arrange
            schema ??= ValidationUtils.CreateSchema();
            validator ??= CreateValidator();
            DocumentNode query = Utf8GraphQLParser.Parse(sourceText);

            // act
            DocumentValidatorResult result = validator.Validate(schema, query);

            // assert
            Assert.NotEmpty(result.Errors);

            if (elementInspectors.Length > 0)
            {
                Assert.Collection(result.Errors, elementInspectors);
            }

            result.Errors.MatchSnapshot();
        }

        private static IDocumentValidator CreateValidator()
        {
            return new ServiceCollection()
                .AddValidation()
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IDocumentValidatorFactory>()
                .CreateValidator();
        }
    }
}
