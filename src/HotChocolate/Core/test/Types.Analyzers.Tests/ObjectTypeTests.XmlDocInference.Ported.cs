namespace HotChocolate.Types;

//// Ported tests from XmlDocumentationProviderTests to ensure identical behavior between both documentation providers.

public partial class ObjectTypeXmlDocInferenceTests
{
    [Fact]
    public void When_xml_doc_is_missing_then_description_is_empty()
    {
        var snap = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                public static string GetUser()
                    => "User";
            }
            """);

        Assert.Empty(DescriptionExtractorRegex().Matches(snap.Render()));
    }

    [Fact]
    public void When_xml_doc_with_multiple_breaks_is_read_then_they_are_not_stripped_away()
    {
        var snap = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// Query and manages users.
                ///
                /// Please note:
                /// * Users ...
                /// * Users ...
                ///     * Users ...
                ///     * Users ...
                ///
                /// You need one of the following role: Owner,
                /// Editor, use XYZ to manage permissions.
                /// </summary>
                public static string? Foo { get; set; }
            }
            """);

        const string expected =
            "Query and manages users.\\n    \\nPlease note:\\n* Users ...\\n* Users ...\\n    * Users ...\\n"
            + "    * Users ...\\n    \\nYou need one of the following role: Owner,\\n"
            + "Editor, use XYZ to manage permissions.";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

    [Fact]
    public void When_description_has_see_tag_then_it_is_converted()
    {
        var snap = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            public class Record;

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// <see langword="null"/> for the default <see cref="Record"/>.
                /// See <see cref="Record">this</see> and
                /// <see href="https://foo.com/bar/baz">this</see> at
                /// <see href="https://foo.com/bar/baz"/>.
                /// </summary>
                public static string? Foo { get; set; }
            }
            """);

        const string expected = "null for the default Record.\\nSee this and\\nthis at\\nhttps://foo.com/bar/baz.";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

    [Fact]
    public void When_description_has_paramref_tag_then_it_is_converted()
    {
        var snap = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// This is a parameter reference to <paramref name="id"/>.
                /// </summary>
                public int Foo(int id) => id;
            }
            """);

        const string expected = "This is a parameter reference to id.";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

    [Fact]
    public void When_description_has_generic_tags_then_it_is_converted()
    {
        var snap = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                /// <summary>These <c>are</c> <strong>some</strong> tags.</summary>
                public int Foo() => 0;
            }
            """);

        const string expected = "These are some tags.";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

    [Fact]
    public void When_method_has_inheritdoc_then_it_is_resolved()
    {
        var snap = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            /// <summary>
            /// I am the most base class.
            /// </summary>
            public class BaseBaseClass
            {
                /// <summary>Method doc.</summary>
                /// <param name="baz">Parameter details.</param>
                public virtual void Bar(string baz) { }
            }

            [QueryType]
            internal static partial class Query
            {
                /// <inheritdoc cref="BaseBaseClass.Bar" />
                public static int Bar(string baz) => 0;
            }
            """);

        const string expected = "Method doc.";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

    [Fact]
    public void When_class_has_description_then_it_is_converted()
    {
        var snap = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// I am a test class. This should not be escaped: >
                /// </summary>
                public static int Bar() => 0;
            }
            """);

        const string expected = "I am a test class. This should not be escaped: >";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

     [Fact]
     public void When_method_has_returns_then_it_is_converted()
     {
         var snap = TestHelper.GetGeneratedSourceSnapshot(
             """
             using System;
             using System.Collections.Generic;
             using System.Threading;
             using System.Threading.Tasks;
             using HotChocolate;
             using HotChocolate.Types;

             namespace TestNamespace;

             [QueryType]
             internal static partial class Query
             {
                 /// <summary>
                 /// Query and manages users.
                 /// </summary>
                 /// <returns>Bar</returns>
                 public static int Bar() => 0;
             }
             """);

         const string expected = "Query and manages users.\\n\\n\\n**Returns:**\\nBar";

         var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
         Assert.Equal(expected, emitted[1].Value);
     }

    [Fact]
    public void When_method_has_exceptions_then_it_is_converted()
    {
        var snap = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// Query and manages users.
                /// </summary>
                /// <returns>Bar</returns>
                /// <exception cref="Exception" code="FOO_ERROR">Foo Error</exception>
                /// <exception cref="Exception" code="BAR_ERROR">Bar Error</exception>
                public static int Bar() => 0;
            }
            """);

        const string expected = "Query and manages users.\\n\\n\\n**Returns:**\\nBar\\n\\n**Errors:**\\n1. FOO_ERROR: Foo Error\\n2. BAR_ERROR: Bar Error";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

    [Fact]
    public void When_method_has_exceptions_then_exceptions_with_no_code_will_be_ignored()
    {
        var snap = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// Query and manages users.
                /// </summary>
                /// <returns>Bar</returns>
                /// <exception cref="Exception">Foo Error</exception>
                /// <exception cref="Exception" code="FOO_ERROR">Foo Error</exception>
                /// <exception cref="Exception" code="BAR_ERROR">Bar Error</exception>
                public static int Bar() => 0;
            }
            """);

        const string expected = "Query and manages users.\\n\\n\\n**Returns:**\\nBar\\n\\n**Errors:**\\n1. FOO_ERROR: Foo Error\\n2. BAR_ERROR: Bar Error";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

    [Fact]
    public void When_method_has_only_exceptions_with_no_code_then_error_section_will_not_be_written()
    {
        var snap = TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using HotChocolate;
            using HotChocolate.Types;

            namespace TestNamespace;

            [QueryType]
            internal static partial class Query
            {
                /// <summary>
                /// Query and manages users.
                /// </summary>
                /// <returns>Bar</returns>
                /// <exception cref="Exception">Foo Error</exception>
                public static int Bar() => 0;
            }
            """);

        const string expected = "Query and manages users.\\n\\n\\n**Returns:**\\nBar";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

    [System.Text.RegularExpressions.GeneratedRegex("configuration.Description = \"(.*)\";")]
    private static partial System.Text.RegularExpressions.Regex DescriptionExtractorRegex();
}
