namespace HotChocolate.Types;

//// Ported tests from XmlDocumentationProviderTests to ensure identical behavior between both documentation providers.

public partial class ObjectTypeXmlDocInferenceTests
{
    [Fact]
    public void When_xml_doc_is_missing_then_description_is_empty()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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

        var content = snapshot.Match();
        Assert.Empty(s_description.Matches(content));
    }

    [Fact]
    public void When_xml_doc_with_multiple_breaks_is_read_then_they_are_not_stripped_away()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal(
            "Query and manages users.\\n    \\nPlease note:\\n* Users ...\\n* Users ...\\n    * Users ...\\n"
            + "    * Users ...\\n    \\nYou need one of the following role: Owner,\\n"
            + "Editor, use XYZ to manage permissions.",
            emitted[1].Value);
    }

    [Fact]
    public void When_description_has_see_tag_then_it_is_converted()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("null for the default Record.\\nSee this and\\nthis at\\nhttps://foo.com/bar/baz.", emitted[1].Value);
    }

    [Fact]
    public void When_description_has_paramref_tag_then_it_is_converted()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("This is a parameter reference to id.", emitted[1].Value);
    }

    [Fact]
    public void When_description_has_generic_tags_then_it_is_converted()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("These are some tags.", emitted[1].Value);
    }

    [Fact]
    public void When_method_has_inheritdoc_then_it_is_resolved()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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
                    public virtual void Bar(string baz) { }
                }

                [QueryType]
                internal static partial class Query
                {
                    /// <inheritdoc cref="BaseBaseClass.Bar" />
                    public static int Bar(string baz) => 0;
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("Method doc.", emitted[1].Value);
    }

    [Fact]
    public void When_class_has_description_then_it_is_converted()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("I am a test class. This should not be escaped: >", emitted[1].Value);
    }

    [Fact]
    public void When_method_has_returns_then_it_is_converted()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("Query and manages users.\\n\\n\\n**Returns:**\\nBar", emitted[1].Value);
    }

    [Fact]
    public void When_method_has_exceptions_then_it_is_converted()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("Query and manages users.\\n\\n\\n**Returns:**\\nBar\\n\\n**Errors:**\\n1. FOO_ERROR: Foo Error\\n2. BAR_ERROR: Bar Error", emitted[1].Value);
    }

    [Fact]
    public void When_method_has_exceptions_then_exceptions_with_no_code_will_be_ignored()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("Query and manages users.\\n\\n\\n**Returns:**\\nBar\\n\\n**Errors:**\\n1. FOO_ERROR: Foo Error\\n2. BAR_ERROR: Bar Error", emitted[1].Value);
    }

    [Fact]
    public void When_method_has_only_exceptions_with_no_code_then_error_section_will_not_be_written()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
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

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("Query and manages users.\\n\\n\\n**Returns:**\\nBar", emitted[1].Value);
    }

    [Fact]
    public void When_parameter_has_inheritdoc_then_it_is_resolved()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                /// <summary>
                /// I am the base class.
                /// </summary>
                public class BaseClass
                {
                    /// <summary>Method doc.</summary>
                    /// <param name="baz">Parameter details.</param>
                    public virtual void Bar(string baz) { }
                }

                public class ClassWithInheritdoc : BaseClass
                {
                    /// <inheritdoc />
                    public override void Bar(string baz) { }
                }

                [QueryType]
                internal static partial class Query
                {
                    /// <inheritdoc cref="ClassWithInheritdoc.Bar" />
                    public static int Bar(string baz) => 0;
                }
                """);

        var content = snapshot.Match();
        AssertFieldDocumentation(content, "Method doc.", "Parameter details.");
    }

    [Fact]
    public void When_class_implements_interface_and_method_has_description_then_method_parameter_description_is_used()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading;
                using System.Threading.Tasks;
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                /// <summary>
                /// I am the base class.
                /// </summary>
                public class BaseClass
                {
                    /// <summary>Method doc.</summary>
                    /// <param name="baz">Parameter details.</param>
                    public virtual void Bar(string baz) { }
                }

                public class ClassWithInheritdoc : BaseClass
                {
                    /// <summary>
                    /// I am my own method.
                    /// </summary>
                    /// <param name="baz">I am my own parameter.</param>
                    public override void Bar(string baz) { }
                }

                [QueryType]
                internal static partial class Query
                {
                    /// <inheritdoc cref="ClassWithInheritdoc.Bar" />
                    public static int Bar(string baz) => 0;
                }
                """);

        var content = snapshot.Match();
        AssertFieldDocumentation(content, "I am my own method.", "I am my own parameter.");
    }

    private static void AssertFieldDocumentation(string content, string fieldDoc, params string[] parameterDocs)
    {
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal(fieldDoc, emitted[1].Value);
        if (parameterDocs.Length > 0)
        {
            var paramDescriptions = s_paramDescription.Matches(content).ToArray();
            Assert.Equal(parameterDocs.Length, paramDescriptions.Length);
            for (var index = 0; index < paramDescriptions.Length; index++)
            {
                var paramDescription = paramDescriptions[index];
                Assert.Equal(parameterDocs[index], paramDescription.Groups[2].Value);
            }
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex("configuration.Description = \"(.*)\";")]
    private static partial System.Text.RegularExpressions.Regex DescriptionExtractorRegex();

    [System.Text.RegularExpressions.GeneratedRegex("(\\s+)Description = \"(.*)\",")]
    private static partial System.Text.RegularExpressions.Regex ParameterDescriptionExtractorRegex();
}
