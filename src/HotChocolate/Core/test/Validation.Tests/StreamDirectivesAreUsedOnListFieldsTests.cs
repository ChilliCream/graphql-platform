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
    public void Stream_On_String_Field_1()
    {
        ExpectErrors(
            @"query {
                __typename @stream
            }",
            t => Assert.Equal(
                "@stream directive is only valid on list fields.",
                t.Message));
    }

    [Fact]
    public void Stream_On_String_Field_2()
    {
        ExpectErrors(
            @"query {
                __schema {
                    description @stream
                }
            }",
            t => Assert.Equal(
                "@stream directive is only valid on list fields.",
                t.Message));
    }

    [Fact]
    public void Stream_On_Types()
    {
        ExpectValid(
            @"query {
                __schema {
                    types @stream {
                        name
                    }
                }
            }");
    }
}
