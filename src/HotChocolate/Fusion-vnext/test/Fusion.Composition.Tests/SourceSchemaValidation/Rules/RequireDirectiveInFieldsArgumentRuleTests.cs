using HotChocolate.Fusion;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.SourceSchemaValidation.Rules;

namespace HotChocolate.Composition.SourceSchemaValidation.Rules;

public sealed class RequireDirectiveInFieldsArgumentRuleTests : CompositionTestBase
{
    private readonly SourceSchemaValidator _sourceSchemaValidator =
        new([new RequireDirectiveInFieldsArgumentRule()]);

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _sourceSchemaValidator.Validate(context);

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(context.Log.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(InvalidExamplesData))]
    public void Examples_Invalid(string[] sdl, string[] errorMessages)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _sourceSchemaValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(context.Log.All(e => e.Code == "REQUIRE_DIRECTIVE_IN_FIELDS_ARG"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this valid usage, the @require directive’s "fields" argument references "name"
            // without any directive applications, avoiding the error.
            {
                [
                    """
                    type User @key(fields: "id name") {
                        id: ID!
                        profile(name: String! @require(fields: "name")): Profile
                    }

                    type Profile {
                        id: ID!
                        name: String
                    }
                    """
                ]
            }
        };
    }

    public static TheoryData<string[], string[]> InvalidExamplesData()
    {
        return new TheoryData<string[], string[]>
        {
            // Because the @require selection ("name @lowercase") includes a directive application
            // (@lowercase), this violates the rule and triggers a REQUIRE_DIRECTIVE_IN_FIELDS_ARG
            // error.
            {
                [
                    """
                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
                        profile(name: String! @require(fields: "name @lowercase")): Profile
                    }

                    type Profile {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The @require directive on argument 'User.profile(name:)' in schema 'A' " +
                    "references field 'name', which must not include directive applications."
                ]
            },
            // Nested field.
            {
                [
                    """
                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
                        profile(name: String! @require(fields: "info { name @lowercase }")): Profile
                    }

                    type Profile {
                        id: ID!
                        info: ProfileInfo
                    }

                    type ProfileInfo {
                        name: String
                    }
                    """
                ],
                [
                    "The @require directive on argument 'User.profile(name:)' in schema 'A' " +
                    "references field 'info.name', which must not include directive applications."
                ]
            },
            // Multiple fields.
            {
                [
                    """
                    directive @example on FIELD_DEFINITION

                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
                        profile(
                            name: String! @require(fields: "id @example name @example")
                        ): Profile
                    }
                    """
                ],
                [
                    "The @require directive on argument 'User.profile(name:)' in schema 'A' " +
                    "references field 'id', which must not include directive applications.",

                    "The @require directive on argument 'User.profile(name:)' in schema 'A' " +
                    "references field 'name', which must not include directive applications."
                ]
            }
        };
    }
}
