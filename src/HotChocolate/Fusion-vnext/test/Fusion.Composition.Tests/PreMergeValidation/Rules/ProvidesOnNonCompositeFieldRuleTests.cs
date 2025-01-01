using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.PreMergeValidation;
using HotChocolate.Fusion.PreMergeValidation.Rules;

namespace HotChocolate.Composition.PreMergeValidation.Rules;

public sealed class ProvidesOnNonCompositeFieldRuleTests : CompositionTestBase
{
    private readonly PreMergeValidator _preMergeValidator =
        new([new ProvidesOnNonCompositeFieldRule()]);

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
        Assert.True(context.Log.All(e => e.Code == "PROVIDES_ON_NON_COMPOSITE_FIELD"));
        Assert.True(context.Log.All(e => e.Severity == LogSeverity.Error));
    }

    public static TheoryData<string[]> ValidExamplesData()
    {
        return new TheoryData<string[]>
        {
            // Here, "profile" has an object base type "Profile". The @provides directive can
            // validly specify sub-fields like "settings { theme }".
            {
                [
                    """
                    type Profile {
                        email: String
                        settings: Settings
                    }

                    type Settings {
                        notificationsEnabled: Boolean
                        theme: String
                    }

                    type User {
                        id: ID!
                        profile: Profile @provides(fields: "settings { theme }")
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
            // In this example, "email" has a scalar base type (String). Because scalars do not
            // expose sub-fields, attaching @provides to "email" triggers a
            // PROVIDES_ON_NON_OBJECT_FIELD error.
            {
                [
                    """
                    type User {
                        id: ID!
                        email: String @provides(fields: "length")
                    }
                    """
                ],
                [
                    "The field 'User.email' in schema 'A' includes a @provides directive, but " +
                    "does not return a composite type."
                ]
            },
            // Here, the schema is defined with "email" being a non-null string.
            {
                [
                    """
                    type User {
                        id: ID!
                        email: String! @provides(fields: "length")
                    }
                    """
                ],
                [
                    "The field 'User.email' in schema 'A' includes a @provides directive, but " +
                    "does not return a composite type."
                ]
            },
            // Here, the schema is defined with "emails" being a non-null list of non-null strings.
            {
                [
                    """
                    type User {
                        id: ID!
                        emails: [String!]! @provides(fields: "length")
                    }
                    """
                ],
                [
                    "The field 'User.emails' in schema 'A' includes a @provides directive, but " +
                    "does not return a composite type."
                ]
            }
        };
    }
}
