using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class StreamDirectivesAreUsedOnListFieldsTests
    : DocumentValidatorVisitorTestBase
{
    public StreamDirectivesAreUsedOnListFieldsTests()
        : base(builder => builder.AddFieldRules())
    {
    }

    [Fact]
    public void Stream_On_Typename_Field()
    {
        ExpectErrors(
            """
            query {
              __typename @stream
            }
            """,
            t => Assert.Equal(
                "@stream directive is only valid on list fields.",
                t.Message));
    }

    [Fact]
    public void Stream_On_String_Field()
    {
        ExpectErrors(
            """
            query {
              __schema {
                description @stream
              }
            }
            """,
            t => Assert.Equal(
                "@stream directive is only valid on list fields.",
                t.Message));
    }

    [Fact]
    public void Stream_On_Types()
    {
        ExpectValid(
            """
            query {
              __schema {
                types @stream {
                  name
                }
              }
            }
            """
        );
    }
}
