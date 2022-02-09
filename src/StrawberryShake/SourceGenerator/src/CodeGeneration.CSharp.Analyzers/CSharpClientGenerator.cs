using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using static System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers;

[Generator]
public class CSharpClientGenerator : ISourceGenerator
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

            var childProcess = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = codeGenServer,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                })!;

            using var client = new CSharpGeneratorClient(
                childProcess.StandardOutput.BaseStream,
                childProcess.StandardInput.BaseStream);

            foreach (var configFileName in GetConfigFiles(context))
            {
                ExecuteAsync(context, client, configFileName, documentFileNames)
                    .GetAwaiter()
                    .GetResult();
            }

            client.CloseAsync()
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "SSG002",
                        "Generator Error",
                        ex.Message,
                        "Generator",
                        DiagnosticSeverity.Error,
                        true),
                    Microsoft.CodeAnalysis.Location.None));
        }
    }

    private static async Task ExecuteAsync(
        GeneratorExecutionContext context,
        CSharpGeneratorClient client,
        string configFileName,
        IReadOnlyList<string> documentFileNames)
    {
        GeneratorRequest request = new(
            configFileName,
            documentFileNames,
            GetDefaultNamespace(context),
            GetPersistedQueryDirectory(context));
        GeneratorResponse response = await client.GenerateAsync(request);

        foreach (GeneratorDocument document in response.Documents.SelectCSharp())
        {
            context.AddSource(document.Name, document.SourceText);
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
