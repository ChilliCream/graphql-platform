using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            var documentFileNames = GetDocumentFileNames(context);
            var codeGenServer = GetCodeGenServerLocation(context);

            var childProcess = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = codeGenServer,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                })!;

            var client = new CSharpGeneratorClient(
                childProcess.StandardInput.BaseStream,
                childProcess.StandardOutput.BaseStream);

            foreach (var configFileName in GetConfigFiles(context))
            {
                ExecuteAsync(context, client, configFileName, documentFileNames)
                    .GetAwaiter()
                    .GetResult();
            }
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
        string[] documentFileNames)
    {
        GeneratorRequest request = new(
            configFileName,
            documentFileNames,
            GetDefaultNamespace(context));
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

        }

        if (string?(
            "build_property.StrawberryShake_DefaultNamespace",
            out var value) &&
            !string.IsNullOrEmpty(value))
        {
            return value;
        }

    }

     private static string GetCodeGenServerLocation(GeneratorExecutionContext context)
    {
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
            "build_property.StrawberryShake_CodeGenServer",
            out var value) &&
            !string.IsNullOrEmpty(value))
        {
            return value;
        }

        throw new Exception("Could not locate the code generation server.");
    }

    private static string GetProjectFileName(GeneratorExecutionContext context)
    {
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
            "build_property.MSBuildProjectFile",
            out var value) &&
            !string.IsNullOrEmpty(value))
        {
            return value;
        }

        throw new Exception("Could not locate the code generation server.");
    }

    private static bool TryGetBuildProperty(
        GeneratorExecutionContext context,
        string key,
        out string? value)
    {
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
            $"build_property.{key}",
            out var value) &&
            !string.IsNullOrEmpty(value))
        {
            return value;
        }

        return null;
    }
}
