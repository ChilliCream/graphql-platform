using System.Text.RegularExpressions;

namespace HotChocolate.Types;

public partial class ObjectTypeDisableXmlDocumentationTests
{
    private static readonly Regex s_description = DescriptionExtractorRegex();

    [Fact]
    public void XmlDocumentation_Is_Suppressed_When_DisableXmlDocumentation_Is_Set()
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

                [assembly: Module("Test", ModuleOptions.Default | ModuleOptions.DisableXmlDocumentation)]

                namespace TestNamespace;

                [QueryType]
                internal static partial class Query
                {
                    /// <summary>
                    /// This should NOT appear in the schema.
                    /// </summary>
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        var matches = s_description.Matches(content);
        Assert.Empty(matches);
    }

    [Fact]
    public void GraphQLDescription_Attribute_Still_Works_When_DisableXmlDocumentation_Is_Set()
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

                [assembly: Module("Test", ModuleOptions.Default | ModuleOptions.DisableXmlDocumentation)]

                namespace TestNamespace;

                [QueryType]
                internal static partial class Query
                {
                    /// <summary>
                    /// This should NOT appear in the schema.
                    /// </summary>
                    [GraphQLDescription("Explicit description")]
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("Explicit description", emitted[1].Value);
    }

    [Fact]
    public void XmlDocumentation_Is_Emitted_When_DisableXmlDocumentation_Is_Not_Set()
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
                    /// User description from XML doc.
                    /// </summary>
                    public static string GetUser() => "User";
                }
                """);

        var content = snapshot.Match();
        var emitted = s_description.Matches(content).Single().Groups;
        Assert.Equal("User description from XML doc.", emitted[1].Value);
    }

    [GeneratedRegex("configuration.Description = \"(.*)\";")]
    private static partial Regex DescriptionExtractorRegex();
}
