using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using DotNet.Globbing;
using HotChocolate;
using HotChocolate.Utilities;
using Newtonsoft.Json;
using IOPath = System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    [Generator]
    public class CSharpClientGenerator : ISourceGenerator
    {
        private const string _category = "StrawberryShakeGenerator";

        private static string _location = System.IO.Path.GetDirectoryName(
            typeof(CSharpClientGenerator).Assembly.Location)!;

        static CSharpClientGenerator()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        private static Assembly? CurrentDomainOnAssemblyResolve(
            object sender,
            ResolveEventArgs args)
        {
            try
            {
                var assemblyName = new AssemblyName(args.Name);
                var path = IOPath.Combine(_location, assemblyName.Name + ".dll");
                return Assembly.LoadFrom(path);
            }
            catch
            {
                return null;
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // if preconditions are not met we just stop and do not process any further.
            if (!EnsurePreconditionsAreMet(context))
            {
                return;
            }

            try
            {
                var allDocuments = GetGraphQLFiles(context);

                foreach (var config in GetGraphQLConfigs(context))
                {
                    var log = new StringBuilder();
                    log.AppendLine("Client: " + config.Extensions.StrawberryShake.Name);

                    var fileNames = new HashSet<string>();

                    StrawberryShakeSettings settings = config.Extensions.StrawberryShake;
                    string filter = config.Documents ?? IOPath.Combine("**", "*.graphql");
                    string rootDir = IOPath.GetDirectoryName(config.Location)!;
                    string generatedDir = GetGeneratedDirectory(context, settings, rootDir);

                    try
                    {
                        log.AppendLine($"filter: {filter}");
                        log.AppendLine($"rootDir: {rootDir}");
                        log.AppendLine($"generatedDir: {generatedDir}");

                        CreateDirectoryIfNotExists(generatedDir);

                        // get documents that are relevant to this config.
                        var documents = GetClientGraphQLFiles(allDocuments, rootDir, filter);
                        log.AppendLine($"configDocuments: {string.Join("\r\n", documents)}");

                        // generate the client.
                        var result = GenerateClient(documents, config.Extensions.StrawberryShake, log);

                        if (result.HasErrors())
                        {
                            // if we have errors ... we will output them an not generate anything.
                            CreateDiagnosticErrors(context, result.Errors);
                            log.AppendLine("We have errors!");
                            WriteFile(IOPath.Combine(generatedDir, "gen.log"), log.ToString());
                            continue;
                        }

                        // add updated documents.
                        foreach (CSharpDocument document in result.CSharpDocuments)
                        {
                            WriteDocument(context, document, settings, fileNames, generatedDir);
                        }

                        // remove files that are now obsolete
                        log.AppendLine("clean");
                        foreach (string fileName in Directory.GetFiles(generatedDir, "*.cs"))
                        {
                            if (!fileNames.Contains(IOPath.GetFileName(fileName)))
                            {
                                log.AppendLine(fileName);
                                File.Delete(fileName);
                            }
                        }
                    }
                    catch(Exception ex) 
                    {
                        log.AppendLine(ex.Message);
                        log.AppendLine(ex.StackTrace);
                        log.AppendLine(ex.GetType().FullName);
                        throw;
                    }
                    finally
                    {
                        WriteFile(IOPath.Combine(generatedDir, "gen.log"), log.ToString());
                    }
                }
            }
            catch (GraphQLException ex)
            {
                CreateDiagnosticErrors(context, ex.Errors);
            }
        }

        private void WriteDocument(
            GeneratorExecutionContext context,
            CSharpDocument document,
            StrawberryShakeSettings settings,
            HashSet<string> fileNames,
            string generatedDirectory)
        {
            string documentName = $"{document.Name}.{settings.Name}.StrawberryShake.cs";
            fileNames.Add(documentName);

            var fileName = IOPath.Combine(generatedDirectory, documentName);
            var sourceText = SourceText.From(document.SourceText, Encoding.UTF8);

            context.AddSource(documentName, sourceText);

            WriteFile(fileName, document.SourceText);
        }

        private CSharpGeneratorResult GenerateClient(
            IEnumerable<string> documents,
            StrawberryShakeSettings settings,
            StringBuilder log)
        {
            try
            {
                var generator = new CSharpGenerator();
                return generator.Generate(documents, settings.Name, settings.Namespace);
            }
            catch (GraphQLException)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.AppendLine(ex.Message);
                log.AppendLine(ex.StackTrace);
                log.AppendLine(ex.GetType().FullName);

                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .SetException(ex)
                        .Build());
            }
        }

        private static bool EnsurePreconditionsAreMet(
            GeneratorExecutionContext context)
        {
            if (!EnsureDependencyExists(
                context,
                "StrawberryShake.Core",
                "StrawberryShake.Core"))
            {
                return false;
            }

            if (!EnsureDependencyExists(
                context,
                "StrawberryShake.Transport.Http",
                "StrawberryShake.Transport.Http"))
            {
                return false;
            }

            if (!EnsureDependencyExists(
                context,
                "Microsoft.Extensions.Http",
                "Microsoft.Extensions.Http"))
            {
                return false;
            }

            return true;
        }

        private static bool EnsureDependencyExists(
            GeneratorExecutionContext context,
            string assemblyName,
            string packageName)
        {
            if (!context.Compilation.ReferencedAssemblyNames.Any(
                ai => ai.Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase)))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: CodeGenerationErrorCodes.NoTypeDocumentsFound,
                            title: "Dependency Missing",
                            messageFormat: $"The package reference `{packageName}` is missing.\r\n`dotnet add package {packageName}`",
                            category: _category,
                            DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Microsoft.CodeAnalysis.Location.None));
                return false;
            }
            return true;
        }

        private void CreateDiagnosticErrors(
            GeneratorExecutionContext context,
            IReadOnlyList<IError> errors)
        {
            foreach (IError error in errors)
            {
                string title =
                    error.Extensions is not null &&
                    error.Extensions.TryGetValue(
                        CodeGenerationThrowHelper.TitleExtensionKey,
                        out var value) &&
                    value is string s
                        ? s
                        : "Unexpected";

                string code = error.Code ?? SourceGeneratorErrorCodes.Unexpected;

                if (error.Extensions is not null &&
                    error.Extensions.TryGetValue(
                        CodeGenerationThrowHelper.FileExtensionKey,
                        out value) &&
                    value is string filePath)
                {
                    HotChocolate.Location location =
                        error.Locations?.First() ??
                        throw new ArgumentNullException();

                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                id: code,
                                title: title,
                                messageFormat: error.Message,
                                category: _category,
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true),
                            Microsoft.CodeAnalysis.Location.Create(
                                filePath,
                                TextSpan.FromBounds(
                                    1,
                                    2),
                                new LinePositionSpan(
                                    new LinePosition(
                                        location.Line,
                                        location.Column),
                                    new LinePosition(
                                        location.Line,
                                        location.Column + 1)))));
                }
                else
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(
                                id: code,
                                title: title,
                                messageFormat: $"An error occured during generation: {error.Message}",
                                category: _category,
                                DiagnosticSeverity.Error,
                                isEnabledByDefault: true,
                                description: error.Message),
                            Microsoft.CodeAnalysis.Location.None));
                }
            }
        }

        private IReadOnlyList<string> GetGraphQLFiles(
            GeneratorExecutionContext context) =>
            context.AdditionalFiles
                .Select(t => t.Path)
                .Where(t => IOPath.GetExtension(t).EqualsOrdinal(".graphql"))
                .ToList();

        private IReadOnlyList<GraphQLConfig> GetGraphQLConfigs(
            GeneratorExecutionContext context)
        {
            var list = new List<GraphQLConfig>();

            foreach (var configLocation in GetGraphQLConfigFiles(context))
            {
                try
                {
                    string json = File.ReadAllText(configLocation);
                    var config = JsonConvert.DeserializeObject<GraphQLConfig>(json);
                    config.Location = configLocation;
                    list.Add(config);
                }
                catch (Exception ex)
                {
                    throw new GraphQLException(
                        ErrorBuilder.New()
                            .SetMessage(ex.Message)
                            .SetException(ex)
                            .SetExtension(CodeGenerationThrowHelper.FileExtensionKey, configLocation)
                            .AddLocation(new HotChocolate.Location(1, 1))
                            .Build());
                }
            }

            return list;
        }

        private IReadOnlyList<string> GetGraphQLConfigFiles(
            GeneratorExecutionContext context) =>
            context.AdditionalFiles
                .Select(t => t.Path)
                .Where(t => IOPath.GetFileName(t).EqualsOrdinal(".graphqlrc.json"))
                .ToList();

        private static IReadOnlyList<string> GetClientGraphQLFiles(
            IReadOnlyList<string> allDocuments,
            string rootDirectory,
            string filter)
        {
            rootDirectory += IOPath.DirectorySeparatorChar;
            
            var glob = Glob.Parse(filter);

            return allDocuments
                .Where(t => t.StartsWith(rootDirectory) && glob.IsMatch(t))
                .ToList();
        }

        private string GetGeneratedDirectory(
            GeneratorExecutionContext context,
            StrawberryShakeSettings settings,
            string clientFolder)
        {
            if (settings.OutputToClientDirectory)
            {
                return IOPath.Combine(clientFolder, "Generated");
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                "build_property.StrawberryShake_GeneratedFiles",
                out string? value) &&
                !string.IsNullOrEmpty(value))
            {
                return IOPath.Combine(value, settings.Name);
            }

            return IOPath.Combine(clientFolder, "Generated");
        }

        private void CreateDirectoryIfNotExists(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private void WriteFile(string fileName, string sourceText)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            File.WriteAllText(fileName, sourceText, Encoding.UTF8);
        }
    }
}
