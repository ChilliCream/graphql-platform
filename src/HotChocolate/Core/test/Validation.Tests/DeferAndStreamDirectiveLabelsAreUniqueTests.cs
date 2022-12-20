using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Validation;

public class DeferAndStreamDirectiveLabelsAreUniqueTests
    : DocumentValidatorVisitorTestBase
{
    public DeferAndStreamDirectiveLabelsAreUniqueTests()
        : base(builder => builder.AddDirectiveRules())
    {
    }

    [Fact]
    public void Label_Duplicate_On_Defer()
    {
        ExpectErrors(
            @"query {
                ... @defer(label: ""a"") {
                    a: __typename
                }
                ... @defer(label: ""a"") {
                    b: __typename
                }
            }",
            t => Assert.Equal(
                "If a label is passed, it must be unique within all other @defer " +
                "and @stream directives in the document.",
                t.Message));
    }

    [Fact]
    public void Label_Duplicate_On_Stream()
    {
        ExpectErrors(
            @"query {
                a: __schema {
                    _types @stream(label: ""a"") {
                        name
                    }
                }
                b: __schema {
                    _types @stream(label: ""a"") {
                        name
                    }
                }
            }",
            t => Assert.Equal(
                "If a label is passed, it must be unique within all other @defer " +
                "and @stream directives in the document.",
                t.Message));
    }

    [Fact]
    public void Label_Duplicate_On_Either_Stream_Or_Defer()
    {
        ExpectErrors(
            @"query {
                ... @defer(label: ""a"") {
                    a: __typename
                }
                b: __schema {
                    _types @stream(label: ""a"") {
                        name
                    }
                }
            }",
            t => Assert.Equal(
                "If a label is passed, it must be unique within all other @defer " +
                "and @stream directives in the document.",
                t.Message));
    }

    [Fact]
    public void Label_Is_Variable_On_Defer()
    {
        ExpectErrors(
            @"query($a: String) {
                ... @defer(label: $a) {
                    a: __typename
                }
            }",
            t => Assert.Equal(
                "If a label for @defer or @stream is passed, it must not be a variable.",
                t.Message));
    }

    [Fact]
    public void Label_Can_Be_Null_And_Is_Optional_And_Can_Be_A_Unique_Name()
    {
        ExpectValid(
            @"query {
                ... @defer(label: null) {
                    a: __typename
                }
                ... @defer {
                    a: __typename
                }
                ... @defer(label: ""c"") {
                    c: __typename
                }
                d: __schema {
                    _types @stream(label: null) {
                        name
                    }
                }
                e: __schema {
                    _types @stream {
                        name
                    }
                }
                f: __schema {
                    _types @stream(label: ""b"") {
                        name
                    }
                }
            }");
    }
}
