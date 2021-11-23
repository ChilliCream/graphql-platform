using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using HotChocolate;
using HotChocolate.Utilities;
using static System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    [Generator]
    public class CSharpClientGenerator : ISourceGenerator
    {
        private static string _location =
            GetDirectoryName(typeof(CSharpClientGenerator).Assembly.Location)!;

        static CSharpClientGenerator()
        {
            Assembly.LoadFrom("/Users/michael/.nuget/packages/streamjsonrpc/2.9.85/lib/netstandard2.0/StreamJsonRpc.dll");
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            _location = GetPackageLocation(context);
            var documentFileNames = GetDocumentFileNames(context);
            var client = CodeGenerationClient.Connect(_location);

            foreach (var configFileName in GetConfigFiles(context))
            {
                ExecuteAsync(context, client, configFileName, documentFileNames)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        private static async Task ExecuteAsync(
            GeneratorExecutionContext context,
            CodeGenerationClient client,
            string configFileName,
            string[] documentFileNames)
        {
            await client.SetConfigurationAsync(configFileName);
            await client.SetDocumentsAsync(documentFileNames);

            GeneratorResponse response = await client.GenerateAsync();

            foreach (SourceDocument document in response.Documents.SelectCSharp())
            {
                context.AddSource(document.Name, document.SourceText);
            }

            if (response.Errors.Length > 0)
            {
                foreach (ServerError error in response.Errors)
                {
                    context.ReportError(new Error(error.Message));
                }
            }
        }

        private static string[] GetDocumentFileNames(
            GeneratorExecutionContext context) =>
            context.AdditionalFiles
                .Select(t => t.Path)
                .Where(t => GetExtension(t).EqualsOrdinal(".graphql"))
                .ToArray();

        private static IReadOnlyList<string> GetConfigFiles(
            GeneratorExecutionContext context) =>
            context.AdditionalFiles
                .Select(t => t.Path)
                .Where(t => GetFileName(t).EqualsOrdinal(".graphqlrc.json"))
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
}
