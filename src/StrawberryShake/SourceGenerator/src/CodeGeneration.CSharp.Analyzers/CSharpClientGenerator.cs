using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Newtonsoft.Json;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using IOPath = System.IO.Path;
using static StrawberryShake.CodeGeneration.CSharp.Analyzers.DiagnosticErrorHelper;

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

            _location = GetPackageLocation(context);

            using ILogger log = CreateLogger(context);

            log.SetLocation(_location);

            var allDocuments = GetGraphQLFiles(context);
            var allConfigurations = GetGraphQLConfigs(context);

            foreach (var config in allConfigurations)
            {
                var clientContext = new ClientGeneratorContext(
                    context,
                    log,
                    config.Extensions.StrawberryShake,
                    config.Documents ?? IOPath.Combine("**", "*.graphql"),
                    IOPath.GetDirectoryName(config.Location)!,
                    allDocuments);

                if (clientContext.GetDocuments().Count > 0)
                {
                    log.Begin(config, clientContext);
                    Execute(clientContext);
                    log.End();
                }
            }
        }

        private void Execute(ClientGeneratorContext context)
        {
            try
            {
                CreateDirectoryIfNotExists(context.OutputDirectory);

                if (!TryGenerateClient(context, out CSharpGeneratorResult? result))
                {
                    // there were unexpected errors and we will stop generating this client.
                    return;
                }

                if (result.HasErrors())
                {
                    // if we have generator errors like invalid GraphQL syntax we will also stop.
                    context.ReportError(result.Errors);
                    return;
                }

                // If the generator has no errors we will write the documents.
                var directories = new HashSet<string>();
                foreach (SourceDocument document in
                    result.Documents.Where(t => t.Kind == SourceDocumentKind.CSharp))
                {
                    WriteDocument(context, directories, document);
                }

                string? persistedQueryDirectory = context.GetPersistedQueryDirectory();
                if (context.Settings.RequestStrategy == RequestStrategy.PersistedQuery &&
                    persistedQueryDirectory is not null)
                {
                    if (!Directory.Exists(persistedQueryDirectory))
                    {
                        Directory.CreateDirectory(persistedQueryDirectory);
                    }

                    foreach (SourceDocument document in
                        result.Documents.Where(t => t.Kind == SourceDocumentKind.GraphQL))
                    {
                        WriteGraphQLQuery(context, persistedQueryDirectory, document);
                    }
                }

                // remove files that are now obsolete
                Clean(context);
            }
            catch (Exception ex)
            {
                context.Log.Error(ex);
                context.ReportError(ex);
            }
        }

        private void WriteDocument(
            ClientGeneratorContext context,
            HashSet<string> directories,
            SourceDocument document)
        {
            string documentName = $"{document.Name}.{context.Settings.Name}.StrawberryShake.cs";
            context.Log.WriteDocument(documentName);

            var directory = document.Path is null
                ? context.OutputDirectory
                : IOPath.Combine(context.OutputDirectory, document.Path);

            if (directories.Add(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileName = IOPath.Combine(directory, documentName);

            context.FileNames.Add(fileName);
            context.Execution.AddSource(
                documentName,
                SourceText.From(document.SourceText, Encoding.UTF8));

            WriteFile(fileName, document.SourceText);
        }

        private void WriteGraphQLQuery(
            ClientGeneratorContext context,
            string persistedQueryDirectory,
            SourceDocument document)
        {
            string documentName = document.Hash + ".graphql";
            string fileName = IOPath.Combine(persistedQueryDirectory, documentName);

            context.Log.WriteDocument(documentName);

            WriteFile(fileName, document.SourceText);
        }

        private void Clean(ClientGeneratorContext context)
        {
            context.Log.BeginClean();

            try
            {
                foreach (string fileName in Directory.GetFiles(context.OutputDirectory, "*.cs"))
                {
                    if (!context.FileNames.Contains(fileName))
                    {
                        context.Log.RemoveFile(fileName);
                        File.Delete(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                context.Log.Error(ex);
                context.ReportError(ex);
            }
            finally
            {
                context.Log.EndClean();
            }
        }

        private bool TryGenerateClient(
            ClientGeneratorContext context,
            [NotNullWhen(true)] out CSharpGeneratorResult? result)
        {
            context.Log.BeginGenerateCode();

            try
            {
                var settings = new CSharpGeneratorSettings
                {
                    ClientName = context.Settings.Name,
                    Namespace = context.GetNamespace(),
                    RequestStrategy = context.Settings.RequestStrategy,
                    StrictSchemaValidation = context.Settings.StrictSchemaValidation,
                    HashProvider = context.Settings.HashAlgorithm?.ToLowerInvariant() switch
                    {
                        "sha1" => new Sha1DocumentHashProvider(HashFormat.Hex),
                        "sha256" => new Sha256DocumentHashProvider(HashFormat.Hex),
                        "md5" => new MD5DocumentHashProvider(HashFormat.Hex),
                        _ => new Sha1DocumentHashProvider(HashFormat.Hex)
                    }
                };

                if (context.Settings.TransportProfiles?
                    .Where(t => !string.IsNullOrEmpty(t.Name))
                    .ToList() is { Count: > 0 } profiles)
                {
                    var names = new HashSet<string>();
                    settings.TransportProfiles.Clear();

                    foreach (var profile in profiles)
                    {
                        settings.TransportProfiles.Add(
                            new TransportProfile(
                                profile.Name,
                                profile.Default,
                                profile.Query,
                                profile.Mutation,
                                profile.Subscription));
                    }
                }

                string? persistedQueryDirectory = context.GetPersistedQueryDirectory();

                context.Log.SetGeneratorSettings(settings);
                context.Log.SetPersistedQueryLocation(persistedQueryDirectory);

                if (settings.RequestStrategy == RequestStrategy.PersistedQuery &&
                    persistedQueryDirectory is null)
                {
                    settings.RequestStrategy = RequestStrategy.Default;
                }

                result = CSharpGenerator.Generate(context.GetDocuments(), settings);
                return true;
            }
            catch (GraphQLException ex)
            {
                context.ReportError(ex.Errors);
                result = null;
                return false;
            }
            catch (Exception ex)
            {
                context.Log.Error(ex);
                context.ReportError(ex);
                result = null;
                return false;
            }
            finally
            {
                context.Log.EndGenerateCode();
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
                ReportMissingDependency(context, packageName);
                return false;
            }
            return true;
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
                            .SetExtension(ErrorHelper.FileExtensionKey, configLocation)
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

        private ILogger CreateLogger(GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                "build_property.StrawberryShake_LogFile",
                out string? value) &&
                !string.IsNullOrEmpty(value))
            {
                return new FileLogger(value);
            }

            return new NoOpLogger();
        }

        private string GetPackageLocation(GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                "build_property.StrawberryShake_BuildDirectory",
                out string? value) &&
                !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return _location;
        }
    }
}
