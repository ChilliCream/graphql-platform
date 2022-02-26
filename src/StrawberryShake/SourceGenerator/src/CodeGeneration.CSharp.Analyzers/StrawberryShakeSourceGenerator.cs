using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using static System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers;

[Generator]
public sealed class StrawberryShakeSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            var codeGenServer = GetCodeGenServerLocation(context);
            var documentFileNames = GetDocumentFileNames(context);

            var client = new CSharpGeneratorClient(codeGenServer);

            foreach (var configFileName in GetConfigFiles(context))
            {
                Execute(context, client, configFileName, documentFileNames);
            }
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SSG0003",
                        "Generator Error",
                        ex.Message + "\r\n" + ex.StackTrace,
                        "Generator",
                        DiagnosticSeverity.Error,
                        true),
                    Microsoft.CodeAnalysis.Location.None));
        }
    }

    private static void Execute(
        GeneratorExecutionContext context,
        CSharpGeneratorClient client,
        string configFileName,
        IReadOnlyList<string> documentFileNames)
    {
        GeneratorRequest request = new(
            configFileName,
            documentFileNames,
            GetDirectoryName(configFileName),
            GetDefaultNamespace(context),
            GetPersistedQueryDirectory(context));
        GeneratorResponse response = client.Execute(request);

        foreach (GeneratorDocument document in response.Documents.SelectCSharp())
        {
            context.AddSource(document.Name + ".g.cs", document.SourceText);
        }

        if (response.Errors.Count > 0)
        {
            foreach (GeneratorError error in response.Errors)
            {
                if (error.Location is null || error.FilePath is null)
                {
                    context.ReportDiagnostic(
                        error.Code,
                        error.Title,
                        error.Message);
                }
                else
                {
                    context.ReportDiagnostic(
                        error.Code,
                        error.Title,
                        error.Message,
                        error.FilePath,
                        new Location(error.Location.Line, error.Location.Column));
                }
            }
        }
    }

    private static string[] GetDocumentFileNames(
        GeneratorExecutionContext context) =>
        context.AdditionalFiles
            .Select(t => t.Path)
            .Where(t => GetExtension(t).Equals(".graphql", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static IReadOnlyList<string> GetConfigFiles(
        GeneratorExecutionContext context) =>
        context.AdditionalFiles
            .Select(t => t.Path)
            .Where(t => GetFileName(t).Equals(".graphqlrc.json", StringComparison.OrdinalIgnoreCase))
            .ToList();

    private static string GetDefaultNamespace(GeneratorExecutionContext context)
    {
        if (TryGetBuildProperty(context, "StrawberryShake_DefaultNamespace", out var ns))
        {
            return ns;
        }

        if (TryGetBuildProperty(context, "MSBuildProjectFile", out var projectFile))
        {
            return GetFileNameWithoutExtension(projectFile);
        }

        if (!string.IsNullOrEmpty(context.Compilation.Assembly.Name))
        {
            return context.Compilation.Assembly.Name;
        }

        return "StrawberryShake.Generated";
    }

    private static string GetCodeGenServerLocation(GeneratorExecutionContext context)
    {
        if (TryGetBuildProperty(context, "StrawberryShake_CodeGenServer", out var loc))
        {
            return loc;
        }

        throw new Exception("Could not locate the code generation server.");
    }

    private static string? GetPersistedQueryDirectory(GeneratorExecutionContext context)
        => TryGetBuildProperty(context, "StrawberryShake_PersistedQueryDirectory", out var loc)
            ? loc
            : null;

    private static bool TryGetBuildProperty(
        GeneratorExecutionContext context,
        string key,
        [NotNullWhen(true)] out string? value)
    {
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
            $"build_property.{key}",
            out value) &&
            !string.IsNullOrEmpty(value))
        {
            return true;
        }

        value = null;
        return false;
    }
}
