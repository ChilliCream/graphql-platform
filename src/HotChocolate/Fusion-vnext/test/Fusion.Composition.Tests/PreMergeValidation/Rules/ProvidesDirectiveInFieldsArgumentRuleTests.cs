using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class ProvidesDirectiveInFieldsArgumentRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new ProvidesDirectiveInFieldsArgumentRule()]);

    [Theory]
    [MemberData(nameof(ValidExamplesData))]
    public void Examples_Valid(string[] sdl)
    {
        // arrange
        var context = CreateCompositionContext(sdl);

        // act
        var result = _preMergeValidator.Validate(context);

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
        var result = _preMergeValidator.Validate(context);

        // assert
        Assert.True(result.IsFailure);
        Assert.Equal(errorMessages, context.Log.Select(e => e.Message).ToArray());
        Assert.True(context.Log.All(e => e.Code == "PROVIDES_DIRECTIVE_IN_FIELDS_ARG"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // In this example, the "fields" argument of the @provides directive does not have any
            // directive applications, satisfying the rule.
            {
                [
                    """
                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
                        profile: Profile @provides(fields: "name")
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
            // In this example, the "fields" argument of the @provides directive has a directive
            // application @lowercase, which is not allowed.
            {
                [
                    """
                    directive @lowercase on FIELD_DEFINITION

                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
                        profile: Profile @provides(fields: "name @lowercase")
                    }

                    type Profile {
                        id: ID!
                        name: String
                    }
                    """
                ],
                [
                    "The @provides directive on field 'User.profile' in schema 'A' references " +
                    "field 'name', which must not include directive applications."
                ]
            },
            // Nested field.
            {
                [
                    """
                    directive @lowercase on FIELD_DEFINITION

                    type User @key(fields: "id name") {
                        id: ID!
                        name: String
                        profile: Profile @provides(fields: "info { name @lowercase }")
                    }

                    type Profile {
                        id: ID!
                        info: ProfileInfo!
                    }

                    type ProfileInfo {
                        name: String
                    }
                    """
                ],
                [
                    "The @provides directive on field 'User.profile' in schema 'A' references " +
                    "field 'info.name', which must not include directive applications."
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
                        profile: Profile @provides(fields: "id @example name @example")
                    }
                    """
                ],
                [
                    "The @provides directive on field 'User.profile' in schema 'A' references " +
                    "field 'id', which must not include directive applications.",

                    "The @provides directive on field 'User.profile' in schema 'A' references " +
                    "field 'name', which must not include directive applications."
                ]
            }
        };
    }
}
