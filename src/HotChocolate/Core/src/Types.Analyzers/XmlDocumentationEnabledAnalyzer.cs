using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XmlDocumentationEnabledAnalyzer: DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.MissingXmlDocumentation];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        var compilation = context.Compilation;

        if (IsXmlDocumentationEnabled(compilation))
        {
            return;
        }

        if (HasAssemblyIgnoreAttribute(compilation.Assembly))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Errors.MissingXmlDocumentation, Location.None));
    }

    private static bool HasAssemblyIgnoreAttribute(IAssemblySymbol assembly)
    {
        const string attributeName = "GraphQLIgnoreXmlDocumentationAttribute";
        return assembly.GetAttributes().Any(attr =>
            attr.AttributeClass?.Name == attributeName
            && (attr.NamedArguments.Length == 0
            || (attr.NamedArguments[0].Key == "Ignore" && attr.NamedArguments[0].Value.Value is true)));
    }

    private static bool IsXmlDocumentationEnabled(Compilation compilation)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            if (tree.Options is CSharpParseOptions csharpOptions)
            {
                if (csharpOptions.DocumentationMode != DocumentationMode.None)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
