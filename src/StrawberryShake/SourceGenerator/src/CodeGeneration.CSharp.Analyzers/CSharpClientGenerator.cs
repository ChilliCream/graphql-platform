using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using static System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers;

[Generator]
public class CSharpClientGenerator : ISourceGenerator
{
    private static string _location =
        GetDirectoryName(typeof(CSharpClientGenerator).Assembly.Location)!;

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            _location = GetPackageLocation(context);
            var documentFileNames = GetDocumentFileNames(context);

            Process? childProcess = Process.Start(new ProcessStartInfo("/Users/michael/local/hc-1/src/StrawberryShake/CodeGeneration/src/CodeGeneration.CSharp.Server/bin/Debug/net6.0/BerryCodeGen")
            {
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
        GeneratorRequest request = new(configFileName, documentFileNames);
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

    private static string GetPackageLocation(GeneratorExecutionContext context)
    {
        if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
            "build_property.StrawberryShake_BuildDirectory",
            out var value) &&
            !string.IsNullOrEmpty(value))
        {
            return value;
        }

        return _location;
    }
}
