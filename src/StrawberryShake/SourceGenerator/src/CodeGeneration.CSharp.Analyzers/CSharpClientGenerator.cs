using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Utilities;
using Newtonsoft.Json;
using StrawberryShake.Tools.Configuration;
using IOPath = System.IO.Path;
using static StrawberryShake.CodeGeneration.CSharp.Analyzers.DiagnosticErrorHelper;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    [Generator]
    public class CSharpClientGenerator : ISourceGenerator
    {
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
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = context.SyntaxReceiver;

            _location = GetPackageLocation(context);

            using ILogger log = CreateLogger(context);

            log.SetLocation(_location);

            var allDocuments = GetGraphQLFiles(context);
            var allConfigurations = GetGraphQLConfigs(context);

            log.Flush();

            foreach (var config in allConfigurations)
            {
                var clientContext = new ClientGeneratorContext(
                    context,
                    log,
                    config.Extensions.StrawberryShake,
                    config.Documents,
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
                var hasErrors = !TryGenerateClient(context, out CSharpGeneratorResult? result);

                // Ensure that all needed packages are installed.
                if (!EnsurePreconditionsAreMet(context.Execution, context.Settings, result))
                {
                    return;
                }

                if (result?.HasErrors() ?? false)
                {
                    // if we have generator errors like invalid GraphQL syntax we will also stop.
                    context.ReportError(result.Errors);
                    hasErrors = true;
                }

                // If the generator has no errors we will write the documents.
                IDocumentWriter writer = new FileDocumentWriter(
                    keepFileName: context.Settings.UseSingleFile);

                IReadOnlyList<SourceDocument> documents = hasErrors || result is null
                    ? context.GetLastSuccessfulGeneratedSourceDocuments()
                    : result.Documents;

                if (documents.Count == 0)
                {
                    return;
                }

                if (!hasErrors && result is not null && result.Documents.Count > 0)
                {
                    context.PreserveSourceDocuments(result.Documents);
                }

                foreach (SourceDocument document in documents.SelectCSharp())
                {
                    writer.WriteDocument(context, document);
                }

                writer.Flush();

                // if we have persisted query support enabled we need to write the query files.
                var persistedQueryDirectory = context.GetPersistedQueryDirectory();
                if (context.Settings.RequestStrategy == RequestStrategy.PersistedQuery &&
                    persistedQueryDirectory is not null)
                {
                    if (!Directory.Exists(persistedQueryDirectory))
                    {
                        Directory.CreateDirectory(persistedQueryDirectory);
                    }

                    foreach (SourceDocument document in documents.SelectGraphQL())
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

        private void WriteGraphQLQuery(
            ClientGeneratorContext context,
            string persistedQueryDirectory,
            SourceDocument document)
        {
            string documentName = document.Hash + ".graphql";
            string fileName = IOPath.Combine(persistedQueryDirectory, documentName);

            // we only write the file if it does not exist to not trigger
            // dotnet watch.
            if (!File.Exists(fileName))
            {
                context.Log.WriteDocument(documentName);
                File.WriteAllText(fileName, document.SourceText, Encoding.UTF8);
            }
        }

        private void Clean(ClientGeneratorContext context)
        {
            context.Log.BeginClean();

            try
            {
                if (Directory.Exists(context.OutputDirectory))
                {
                    foreach (string fileName in GetGeneratedFiles(context.OutputDirectory))
                    {
                        if (!context.FileNames.Contains(fileName))
                        {
                            context.Log.RemoveFile(fileName);
                            File.Delete(fileName);
                        }
                    }

                    if (!GetGeneratedFiles(context.OutputDirectory).Any())
                    {
                        Directory.Delete(context.OutputDirectory, true);
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

        private IEnumerable<string> GetGeneratedFiles(string outputDirectory) =>
            Directory.EnumerateFiles(outputDirectory, "*.cs", SearchOption.AllDirectories);

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
                    NoStore = context.Settings.NoStore,
                    InputRecords = context.Settings.Records.Inputs,
                    RazorComponents = context.Settings.RazorComponents,
                    EntityRecords = context.Settings.Records.Entities,
                    SingleCodeFile = context.Settings.UseSingleFile,
                    HashProvider = context.Settings.HashAlgorithm.ToLowerInvariant() switch
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

                var persistedQueryDirectory = context.GetPersistedQueryDirectory();

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
            GeneratorExecutionContext context,
            StrawberryShakeSettings settings,
            CSharpGeneratorResult? result)
        {
            const string http = "StrawberryShake.Transport.Http";
            const string websockets = "StrawberryShake.Transport.WebSockets";
            const string inmemory = "StrawberryShake.Transport.InMemory";
            const string razor = "StrawberryShake.Razor";

            if (settings.TransportProfiles.Count == 1)
            {
                StrawberryShakeSettingsTransportProfile settingsTransportProfile = settings.TransportProfiles[0];

                if (settingsTransportProfile.Default == TransportType.Http &&
                    settingsTransportProfile.Subscription == TransportType.WebSocket &&
                    settingsTransportProfile.Query == null &&
                    settingsTransportProfile.Mutation == null)
                {
                    if (!EnsureDependencyExists(context, http))
                    {
                        return false;
                    }

                    if (result is not null &&
                        result.OperationTypes.Contains(OperationType.Subscription) &&
                        !EnsureDependencyExists(context, http))
                    {
                        return false;
                    }

                    return true;
                }
            }

            var usedTransports = settings.TransportProfiles
                .SelectMany(t => t.GetUsedTransports()).Distinct().ToList();

            if (usedTransports.Contains(TransportType.Http))
            {
                if (!EnsureDependencyExists(context, http))
                {
                    return false;
                }
            }

            if (usedTransports.Contains(TransportType.WebSocket))
            {
                if (!EnsureDependencyExists(context, websockets))
                {
                    return false;
                }
            }

            if (usedTransports.Contains(TransportType.InMemory))
            {
                if (!EnsureDependencyExists(context, inmemory))
                {
                    return false;
                }
            }

            if (settings.RazorComponents)
            {
                if (!EnsureDependencyExists(context, razor))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool EnsureDependencyExists(
            GeneratorExecutionContext context,
            string assemblyName)
        {
            if (!context.Compilation.ReferencedAssemblyNames.Any(
                ai => ai.Name.Equals(assemblyName, StringComparison.OrdinalIgnoreCase)))
            {
                ReportMissingDependency(context, assemblyName);
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

                    if (NameUtils.IsValidGraphQLName(config.Extensions.StrawberryShake.Name))
                    {
                        config.Location = configLocation;
                        list.Add(config);
                    }
                    else
                    {
                        ReportInvalidClientName(
                            context,
                            config.Extensions.StrawberryShake.Name,
                            configLocation);
                    }
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

        private ILogger CreateLogger(GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                "build_property.StrawberryShake_LogFile",
                out var value) &&
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
                out var value) &&
                !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return _location;
        }
    }
}
