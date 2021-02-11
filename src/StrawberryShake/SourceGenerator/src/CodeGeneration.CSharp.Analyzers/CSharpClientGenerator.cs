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
            EnsurePreconditionsAreMet(context);

            try
            {
                int i = 1;
                var documents = GetGraphQLFiles(context);
                var documentNames = new HashSet<string>();

                foreach (var config in GetGraphQLConfigs(context))
                {
                    config.Documents ??= IOPath.Combine("**", "*.graphql");
                    string root = IOPath.GetDirectoryName(config.Location)!;
                    string generated = IOPath.Combine(root, "Generated");

                    var glob = Glob.Parse(config.Documents);
                    var configDocuments = documents
                        .Where(t => t.StartsWith(root) && glob.IsMatch(t))
                        .ToList();

                    var result = GenerateClient(documents, config.Extensions.StrawberryShake);

                    if (result.HasErrors())
                    {
                        CreateDiagnosticErrors(context, result.Errors);
                    }

                    foreach (CSharpDocument document in result.CSharpDocuments)
                    {
                        string documentName =
                            $"{config.Extensions.StrawberryShake.Name}.{document.Name}.{i}.cs";
                        string fileName = $"{document.Name}.StrawberryShake.cs";

                        if (!documentNames.Add(documentName))
                        {
                            string id = Guid.NewGuid().ToString("N");
                            documentName = id + documentName;
                            fileName = id + fileName;
                        }

                        context.AddSource(
                            documentName,
                            SourceText.From(document.SourceText, Encoding.UTF8));
                        

                        File.WriteAllText(
                            Path.Combine(generated, fileName),
                            document.SourceText);
                    }
                }
            }
            catch (GraphQLException ex)
            {
                CreateDiagnosticErrors(context, ex.Errors);
            }
        }

        private CSharpGeneratorResult GenerateClient(
            IEnumerable<string> documents,
            StrawberryShakeSettings settings)
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
                throw new GraphQLException(
                    ErrorBuilder.New()
                        .SetMessage(ex.Message)
                        .SetException(ex)
                        .Build());
            }
        }

        private static void EnsurePreconditionsAreMet(
            GeneratorExecutionContext context)
        {
            EnsureDependencyExists(
                context,
                "StrawberryShake.Core",
                "StrawberryShake.Core");

            EnsureDependencyExists(
                context,
                "StrawberryShake.Transport.Http",
                "StrawberryShake.Transport.Http");

            EnsureDependencyExists(
                context,
                "Microsoft.Extensions.Http",
                "Microsoft.Extensions.Http");
        }

        private static void EnsureDependencyExists(
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
            }
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
    }
}
