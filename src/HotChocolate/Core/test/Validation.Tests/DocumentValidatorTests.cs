using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookieCrumble;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Language;
using HotChocolate.StarWars;

namespace HotChocolate.Validation;

public class DocumentValidatorTests
{
    [Fact]
    public void DocumentIsNull()
    {
        // arrange
        var schema = ValidationUtils.CreateSchema();
        var queryValidator = CreateValidator();

        // act
        async Task Error() =>
            await queryValidator.ValidateAsync(
                schema,
                null!,
                new OperationDocumentId("abc"),
                new Dictionary<string, object>(),
                false);

        // assert
        Assert.ThrowsAsync<ArgumentNullException>(Error);
    }

    [Fact]
    public void SchemaIsNull()
    {
        // arrange
        var queryValidator = CreateValidator();

        // act
        async Task Error() =>
            await queryValidator.ValidateAsync(
                null!,
                new DocumentNode(null, new List<IDefinitionNode>()),
                new OperationDocumentId("abc"),
                new Dictionary<string, object>(),
                false);

        // assert
        Assert.ThrowsAsync<ArgumentNullException>(Error);
    }

    [Fact]
    public async Task QueryWithTypeSystemDefinitions()
    {
        await ExpectErrors(
            """
            query getDogName {
                dog {
                    name
                    color
                }
            }

            extend type Dog {
                color: String
            }
            """,
            t => Assert.Equal(
                "A document containing TypeSystemDefinition " +
                "is invalid for execution.",
                t.Message),
            t => Assert.Equal(
                "The field `color` does not exist " +
                "on the type `Dog`.",
                t.Message));
    }

    [Fact]
    public async Task QueryWithOneAnonymousAndOneNamedOperation()
    {
        await ExpectErrors(
            """
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
            """,
            t =>
            {
                Assert.Equal(
                    "GraphQL allows a short‐hand form for defining query " +
                    "operations when only that one operation exists in " +
                    "the document.",
                    t.Message);
            });
    }

    [Fact]
    public async Task TwoQueryOperationsWithTheSameName()
    {
        await ExpectErrors(
            """
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
            """,
            t => Assert.Equal(
                "The operation name `getName` is not unique.",
                t.Message));
    }

    [Fact]
    public async Task OperationWithTwoVariablesThatHaveTheSameName()
    {
        await ExpectErrors(
            @"
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
                "name is invalid for execution.",
                t.Message));
    }

    [Fact]
    public async Task DuplicateArgument()
    {
        await ExpectErrors(
             @"
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
            "is ambiguous and invalid.",
            t.Message));
    }

    [Fact]
    public async Task MissingRequiredArgNonNullBooleanArg()
    {
        await ExpectErrors(
            @"
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
    public async Task DisallowedSecondRootField()
    {
        await ExpectErrors(
            @"
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
                "on the type `Subscription`.",
                t.Message));
    }

    [Fact]
    public async Task FieldIsNotDefinedOnTypeInFragment()
    {
        await ExpectErrors(
            @"
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
                "on the type `Dog`.",
                t.Message),
            t => Assert.Equal(
                "The field `kawVolume` does not exist " +
                "on the type `Dog`.",
                t.Message));
    }

    [Fact]
    public async Task VariableNotUsedWithinFragment()
    {
        await ExpectErrors(
            @"
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
                "atOtherHomes.",
                t.Message));
    }

    [Fact]
    public async Task SkipDirectiveIsInTheWrongPlace()
    {
        await ExpectErrors(
            @"
                query @skip(if: $foo) {
                    field
                }
            ");
    }

    [Fact]
    public async Task QueriesWithInvalidVariableTypes()
    {
        // arrange
        await ExpectErrors(
            null,
            new ServiceCollection()
                .AddValidation()
                .ModifyValidationOptions(o => o.MaxAllowedErrors = int.MaxValue)
                .Services
                .BuildServiceProvider()
                .GetRequiredService<IDocumentValidatorFactory>()
                .CreateValidator(),
            @"
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
                }",
            t => Assert.Equal(
                "Operation `takesCat` has a empty selection set. Root types without " +
                "subfields are disallowed.",
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
    public async Task ConflictingBecauseAlias()
    {
        await ExpectErrors(
            @"
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
    public async Task InvalidFieldArgName()
    {
        await ExpectErrors(
            @"
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
                "The argument `command` does not exist.",
                t.Message),
            t => Assert.Equal(
                "The argument `dogCommand` is required.",
                t.Message));
    }

    [Fact]
    public async Task UnusedFragment()
    {
        await ExpectErrors(
            @"
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
                "is not used within the current document.",
                t.Message));
    }

    [Fact]
    public async Task DuplicateFragments()
    {
        await ExpectErrors(
            @"
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
    public async Task ScalarSelectionsNotAllowedOnInt()
    {
        await ExpectErrors(
            @"
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
    public async Task InlineFragOnScalar()
    {
        await ExpectErrors(
            @"
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
            t => Assert.Equal(
                t.Message,
                "Fragments can only be declared on unions, interfaces, " +
                "and objects."));
    }

    [Fact]
    public async Task FragmentCycle1()
    {
        await ExpectErrors(
            @"
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
            t => Assert.Equal(
                t.Message,
                "The graph of fragment spreads must not form any " +
                "cycles including spreading itself. Otherwise an " +
                "operation could infinitely spread or infinitely " +
                "execute on cycles in the underlying data."));
    }

    [Fact]
    public async Task UndefinedFragment()
    {
        await ExpectErrors(
            @"
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
    public async Task FragmentDoesNotMatchType()
    {
        await ExpectErrors(
            @"
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
    public async Task NotExistingTypeOnInlineFragment()
    {
        await ExpectErrors(
            @"
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
    public async Task InvalidInputObjectFieldsExist()
    {
        await ExpectErrors(
            @"
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
    public async Task RequiredFieldIsNull()
    {
        await ExpectErrors(
            @"
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
    public async Task NameFieldIsAmbiguous()
    {
        await ExpectErrors(
            @"
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
    public async Task UnsupportedDirective()
    {
        await ExpectErrors(
            @"
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
    public async Task StringIntoInt()
    {
        await ExpectErrors(
            @"
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
    public async Task MaxDepthRuleIsIncluded()
    {
        await ExpectErrors(
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
                    "The GraphQL document has an execution depth of 2 " +
                    "which exceeds the max allowed execution depth of 1.",
                    t.Message);
            });
    }

    [Fact]
    public async Task GoodBooleanArgDefault2()
    {
        await ExpectValid(
            @"
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
    public async Task StarWars_Query_Is_Valid()
    {
        await ExpectValid(
            SchemaBuilder.New().AddStarWarsTypes().Create(),
            null,
            FileResource.Open("StarWars_Request.graphql"));
    }

    [Fact]
    public async Task DuplicatesWillBeIgnoredOnFieldMerging()
    {
        // arrange
        var schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        var document = Utf8GraphQLParser.Parse(
            FileResource.Open("InvalidIntrospectionQuery.graphql"));

        var originalOperation = ((OperationDefinitionNode)document.Definitions[0]);
        var operationWithDuplicates = originalOperation.WithSelectionSet(
            originalOperation.SelectionSet.WithSelections(
                new List<ISelectionNode>
                {
                    originalOperation.SelectionSet.Selections[0],
                    originalOperation.SelectionSet.Selections[0],
                }));

        document = document.WithDefinitions(
            new List<IDefinitionNode>(document.Definitions.Skip(1)) { operationWithDuplicates, });

        var services = new ServiceCollection()
            .AddValidation()
            .Services
            .BuildServiceProvider();

        var factory = services.GetRequiredService<IDocumentValidatorFactory>();
        var validator = factory.CreateValidator();

        // act
        var result = await validator.ValidateAsync(
            schema,
            document,
            new OperationDocumentId("abc"),
            new Dictionary<string, object>(),
            false);

        // assert
        Assert.False(result.HasErrors);
    }

    [Fact]
    public async Task Ensure_That_Merged_Fields_Are_Not_In_Violation_Of_Duplicate_Directives_Rule()
    {
        await ExpectValid(
            @"
                query ($a: Boolean!) {
                    dog {
                        ... inlineFragOnScalar
                        owner @include(if: $a) {
                            address
                        }
                    }
                }

                fragment inlineFragOnScalar on Dog {
                    owner @include(if: $a) {
                        name
                    }
                }
            ");
    }

    [Fact]
    public async Task Ensure_Recursive_Fragments_Fail()
    {
        await ExpectErrors("fragment f on Query{...f} {...f}");
    }

    [Fact]
    public async Task Ensure_Recursive_Fragments_Fail_2()
    {
        await ExpectErrors(
            @"
                fragment f on Query {
                    ...f
                    f {
                        ...f
                        f {
                            ...f
                        }
                    }
                }

                {...f}");
    }

    [Fact]
    public async Task Short_Long_Names()
    {
        await ExpectErrors(FileResource.Open("short_long_names_query.graphql"));
    }

    [Fact]
    public async Task Anonymous_empty_query_repeated_25000()
    {
        await ExpectErrors(FileResource.Open("anonymous_empty_query_repeated_25000.graphql"));
    }

    [Fact]
    public async Task Type_query_repeated_6250()
    {
        await ExpectErrors(FileResource.Open("__type_query_repeated_6250.graphql"));
    }

    [Fact]
    public async Task Typename_query_repeated_4167()
    {
        await ExpectErrors(FileResource.Open("__typename_query_repeated_4167.graphql"));
    }

    [Fact]
    public async Task Typename_query()
    {
        await ExpectValid(FileResource.Open("__typename_query.graphql"));
    }

    [Fact]
    public async Task Produce_Many_Errors_100_query()
    {
        await ExpectErrors(FileResource.Open("100_query.graphql"));
    }

    [Fact]
    public async Task Produce_Many_Errors_1000_query()
    {
        await ExpectErrors(FileResource.Open("1000_query.graphql"));
    }

    [Fact]
    public async Task Produce_Many_Errors_10000_query()
    {
        await ExpectErrors(FileResource.Open("10000_query.graphql"));
    }

    [Fact]
    public async Task Produce_Many_Errors_25000_query()
    {
        await ExpectErrors(FileResource.Open("25000_query.graphql"));
    }

    [Fact]
    public async Task Produce_Many_Errors_30000_query()
    {
        await ExpectErrors(FileResource.Open("30000_query.graphql"));
    }

    [Fact]
    public async Task Produce_Many_Errors_50000_query()
    {
        await ExpectErrors(FileResource.Open("50000_query.graphql"));
    }

    private Task ExpectValid(string sourceText) => ExpectValid(null, null, sourceText);

    private async Task ExpectValid(ISchema schema, IDocumentValidator validator, string sourceText)
    {
        // arrange
        schema ??= ValidationUtils.CreateSchema();
        validator ??= CreateValidator();
        var query = Utf8GraphQLParser.Parse(sourceText);

        // act
        var result = await validator.ValidateAsync(
            schema,
            query,
            new OperationDocumentId("abc"),
            new Dictionary<string, object>(),
            false);

        // assert
        Assert.Empty(result.Errors);
    }

    private async Task ExpectErrors(string sourceText, params Action<IError>[] elementInspectors) =>
        await ExpectErrors(null, null, sourceText, elementInspectors);

    private async Task ExpectErrors(
        ISchema schema,
        IDocumentValidator validator,
        string sourceText,
        params Action<IError>[] elementInspectors)
    {
        // arrange
        schema ??= ValidationUtils.CreateSchema();
        validator ??= CreateValidator();
        var query = Utf8GraphQLParser.Parse(sourceText, new ParserOptions(maxAllowedFields: int.MaxValue));

        // act
        var result = await validator.ValidateAsync(
            schema,
            query,
            new OperationDocumentId("abc"),
            new Dictionary<string, object>(),
            false);

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
