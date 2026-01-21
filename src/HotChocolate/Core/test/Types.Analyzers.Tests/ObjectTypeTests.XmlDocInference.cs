using System.Text.RegularExpressions;

namespace HotChocolate.Types;

public partial class ObjectTypeXmlDocInferenceTests
{
    private static readonly Regex s_description = DescriptionExtractorRegex();

    [Fact]
    public void Method_WithInheritdoc_And_MultipleLayersOfInheritance()
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

                public class BaseBaseClass
                {
                    /// <summary>Method doc.</summary>
                    public virtual void Bar() { }
                }

                public class BaseClass : BaseBaseClass
                {
                    /// <inheritdoc />
                    public override void Bar() { }
                }

                [QueryType]
                internal static partial class Query
                {
                    /// <inheritdoc cref="BaseClass.Bar" />
                    public static int Bar(string baz) => 0;
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("Method doc.", emitted[1].Value);
    }

    [Fact]
    public void Method_WithInheritdoc_ThatContainsInheritdoc()
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

                public class BaseBaseClass
                {
                    /// <summary>Method doc.</summary>
                    public virtual void Bar() { }
                }

                public class BaseClass : BaseBaseClass
                {
                    /// <summary>
                    /// Concrete Method doc.
                    /// <inheritdoc />
                    /// </summary>
                    public override void Bar() { }
                }

                public class ConcreteClass : BaseClass
                {
                    /// <inheritdoc />
                    public override void Bar() { }
                }

                [QueryType]
                internal static partial class Query
                {
                    /// <inheritdoc cref="ConcreteClass.Bar" />
                    public static int Bar(string baz) => 0;
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("Concrete Method doc.\\nMethod doc.", emitted[1].Value);
    }

    [Fact]
    public void XmlDocumentation_Is_Overriden_By_DescriptionAttribute()
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
                    /// This is ...
                    /// </summary>
                    [GraphQLDescription("Nothing")]
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("Nothing", emitted[1].Value);
    }

    [Fact]
    public async Task XmlDocumentation_With_InheritdocCref_AllPossibleTargets()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
            """
            using System;
            using HotChocolate.Types;

            namespace TestNamespace;

            /// <summary>
            /// Type description.
            /// </summary>
            public class Foo
            {
                /// <summary>
                /// Field description.
                /// </summary>
                public static int F = 0;

                /// <summary>
                /// Property description.
                /// </summary>
                public static int P => 0;

                /// <summary>
                /// Method description.
                /// </summary>
                public static int M() => 0;

                /// <summary>
                /// Event description.
                /// </summary>
                public event EventHandler E;

                /// <summary>
                /// Int-overloaded method description.
                /// </summary>
                public static Bar GetBar(int id) => new Bar();

                /// <summary>
                /// String-overloaded method description.
                /// </summary>
                public static Bar GetBar(string id) => new Bar();

                public class Bar
                {
                    /// <summary>
                    /// Nested type instance field description.
                    /// </summary>
                    public int B = 0;
                }
            }

            [QueryType]
            public static partial class Query
            {
                /// <inheritdoc cref="Foo"/>
                public static string? T() => null;
                /// <inheritdoc cref="Foo.F"/>
                public static string? F() => null;
                /// <inheritdoc cref="Foo.P"/>
                public static string? P() => null;
                /// <inheritdoc cref="Foo.M"/>
                public static string? M() => null;
                /// <inheritdoc cref="Foo.E"/>
                public static string? E() => null;
                /// <inheritdoc cref="Foo.Bar.B"/>
                public static string? Nested() => null;
                /// <inheritdoc cref="Foo.GetBar(int)"/>
                public static string? Overloaded(int id) => null;
            }
            """).MatchMarkdownAsync();
    }

    [Fact]
    public void XmlDocumentation_With_Nested_InheritdocCref()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using System;
                using HotChocolate.Types;

                namespace TestNamespace;

                /// <summary>
                /// This type is similar useless to '<inheritdoc cref="BarType"/>'.
                /// </summary>
                public class FooType
                {
                }

                /// <summary>
                /// The Bar type.
                /// </summary>
                public class BarType
                {
                }

                [QueryType]
                public static partial class Query
                {
                    /// <inheritdoc cref="FooType"/>
                    public static string? Foo() => null;
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("This type is similar useless to 'The Bar type.'.", emitted[1].Value);
    }

    [Fact]
    public void XmlDocumentation_Is_Used_When_No_Attributes_Are_Present()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                [QueryType]
                internal static partial class Query
                {
                    /// <summary>
                    /// This is XML
                    /// </summary>
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("This is XML", emitted[1].Value);
    }

    [Fact]
    public void XmlDocumentation_Is_Ignored_When_IgnoreAttribute_Is_On_Member()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                [QueryType]
                internal static partial class Query
                {
                    /// <summary>
                    /// This is XML
                    /// </summary>
                    [GraphQLIgnoreXmlDocumentation]
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        Assert.DoesNotMatch(s_description, content);
    }

    [Fact]
    public void XmlDocumentation_Is_Ignored_When_IgnoreAttribute_Is_On_Type()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                [QueryType]
                [GraphQLIgnoreXmlDocumentation]
                internal static partial class Query
                {
                    /// <summary>
                    /// This is XML
                    /// </summary>
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        Assert.DoesNotMatch(s_description, content);
    }

    [Fact]
    public void XmlDocumentation_Is_Ignored_When_IgnoreAttribute_Is_On_Assembly()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using HotChocolate;
                using HotChocolate.Types;

                [assembly: GraphQLIgnoreXmlDocumentation]

                namespace TestNamespace;

                [QueryType]
                internal static partial class Query
                {
                    /// <summary>
                    /// This is XML
                    /// </summary>
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        Assert.DoesNotMatch(s_description, content);
    }

    [Fact]
    public void XmlDocumentation_OptIn_OnType_Overrides_Assembly_Level_Ignore()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using HotChocolate;
                using HotChocolate.Types;

                [assembly: GraphQLIgnoreXmlDocumentation]

                namespace TestNamespace;

                [QueryType]
                [GraphQLIgnoreXmlDocumentation(Ignore = false)]
                internal static partial class Query
                {
                    /// <summary>
                    /// This is XML
                    /// </summary>
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("This is XML", emitted[1].Value);
    }

    [Fact]
    public void XmlDocumentation_OptIn_OnMember_Overrides_Assembly_Level_Ignore()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using HotChocolate;
                using HotChocolate.Types;

                [assembly: GraphQLIgnoreXmlDocumentation]

                namespace TestNamespace;

                [QueryType]
                internal static partial class Query
                {
                    /// <summary>
                    /// This is XML
                    /// </summary>
                    [GraphQLIgnoreXmlDocumentation(Ignore = false)]
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("This is XML", emitted[1].Value);
    }

    [Fact]
    public void XmlDocumentation_OptIn_OnMember_Overrides_Type_Level_Ignore()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using HotChocolate;
                using HotChocolate.Types;

                namespace TestNamespace;

                [QueryType]
                [GraphQLIgnoreXmlDocumentation]
                internal static partial class Query
                {
                    /// <summary>
                    /// This is XML
                    /// </summary>
                    [GraphQLIgnoreXmlDocumentation(Ignore = false)]
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("This is XML", emitted[1].Value);
    }

    [Fact]
    public void DescriptionAttribute_Overrides_Xml_Even_When_XmlIgnored()
    {
        var snapshot =
            TestHelper.GetGeneratedSourceSnapshot(
                """
                using HotChocolate;
                using HotChocolate.Types;

                [assembly: GraphQLIgnoreXmlDocumentation]

                namespace TestNamespace;

                [QueryType]
                internal static partial class Query
                {
                    /// <summary>
                    /// This is XML
                    /// </summary>
                    [GraphQLIgnoreXmlDocumentation(Ignore = false)]
                    [GraphQLDescription("Explicit")]
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("Explicit", emitted[1].Value);
    }
}
