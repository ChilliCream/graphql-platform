namespace HotChocolate.Types;

public partial class ObjectTypeXmlDocInferenceTests
{
    [Fact]
    public void Method_WithInheritdoc_And_MultipleLayersOfInheritance()
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

        const string expected = "Method doc.";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

    [Fact]
    public void Method_WithInheritdoc_ThatContainsInheritdoc()
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

        const string expected = "Concrete Method doc.\\nMethod doc.";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }

    [Fact]
    public void XmlDocumentation_Is_Overriden_By_DescriptionAttribute()
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
                /// This is ...
                /// </summary>
                [GraphQLDescription("Nothing")]
                public static string GetUser() => "User";
            }
            """);

        const string expected = "Nothing";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
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
        var snap = TestHelper.GetGeneratedSourceSnapshot(
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

        const string expected = "This type is similar useless to 'The Bar type.'.";

        var emitted = DescriptionExtractorRegex().Matches(snap.Render()).Single().Groups;
        Assert.Equal(expected, emitted[1].Value);
    }
}
