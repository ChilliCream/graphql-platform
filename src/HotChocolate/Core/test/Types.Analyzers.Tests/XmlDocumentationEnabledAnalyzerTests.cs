using Microsoft.CodeAnalysis;

namespace HotChocolate.Types;

public class XmlDocumentationEnabledAnalyzerTests
{
    [Fact]
    public async Task XmlDocumentationEnabled_NoDocs_NoIgnoreAttribute_ProducesWarning()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
                ["""
                 using HotChocolate;
                 using HotChocolate.Types;

                 namespace TestNamespace;

                 [QueryType]
                 public static partial class Query
                 {
                     public static string GetUser() => "User";
                 }
                 """],
                enableAnalyzers: true,
                documentationMode: DocumentationMode.None)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task XmlDocumentationEnabled_Docs_NoIgnoreAttribute_ProducesNoWarning()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
                ["""
                 using HotChocolate;
                 using HotChocolate.Types;

                 namespace TestNamespace;

                 [QueryType]
                 public static partial class Query
                 {
                     public static string GetUser() => "User";
                 }
                 """],
                enableAnalyzers: true,
                documentationMode: DocumentationMode.Parse)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task XmlDocumentationEnabled_Docs_IgnoreAttribute_ProducesNoWarning()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
                ["""
                 using HotChocolate;
                 using HotChocolate.Types;

                 [assembly: GraphQLIgnoreXmlDocumentation]

                 namespace TestNamespace;

                 [QueryType]
                 public static partial class Query
                 {
                     public static string GetUser() => "User";
                 }
                 """],
                enableAnalyzers: true,
                documentationMode: DocumentationMode.Parse)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task XmlDocumentationEnabled_NoDocs_IgnoreAttribute_ProducesNoWarning()
    {
        await TestHelper.GetGeneratedSourceSnapshot(
                ["""
                 using HotChocolate;
                 using HotChocolate.Types;

                 [assembly: GraphQLIgnoreXmlDocumentation]

                 namespace TestNamespace;

                 [QueryType]
                 public static partial class Query
                 {
                     public static string GetUser() => "User";
                 }
                 """],
                enableAnalyzers: true,
                documentationMode: DocumentationMode.None)
            .MatchMarkdownAsync();
    }
}
