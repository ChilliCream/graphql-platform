using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using DotNet.Globbing;
using HotChocolate;
using IOPath = System.IO.Path;
using static StrawberryShake.CodeGeneration.ErrorHelper;
using static StrawberryShake.CodeGeneration.CSharp.Analyzers.SourceGeneratorErrorCodes;
using static StrawberryShake.CodeGeneration.CSharp.Analyzers.DiagnosticErrorHelper;
using System.IO;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class ClientGeneratorContext
    {
        private readonly IReadOnlyList<string> _allDocuments;
        private IReadOnlyList<string>? _documents;

        public ClientGeneratorContext(
            GeneratorExecutionContext execution,
            ILogger log,
            StrawberryShakeSettings settings,
            string filter,
            string clientDirectory,
            IReadOnlyList<string> allDocuments)
        {
            Settings = settings;
            Filter = filter;
            ClientDirectory = clientDirectory;
            OutputDirectory = IOPath.Combine(
                clientDirectory,
                settings.OutputDirectoryName ?? ".generated");
            OutputFiles = settings.OutputDirectoryName is not null;
            _allDocuments = allDocuments;
            Execution = execution;
            Log = log;
        }

        public HashSet<string> FileNames { get; } = new();
        public StrawberryShakeSettings Settings { get; }
        public string Filter { get; }
        public string ClientDirectory { get; }
        public string OutputDirectory { get; }
        public bool OutputFiles { get; }
        public GeneratorExecutionContext Execution { get; }
        public ILogger Log { get; }

        public IReadOnlyList<string> GetDocuments()
        {
            if (_documents is null)
            {
                string rootDirectory = ClientDirectory + IOPath.DirectorySeparatorChar;

                var glob = Glob.Parse(Filter);

                _documents = _allDocuments
                    .Where(t => t.StartsWith(rootDirectory) && glob.IsMatch(t))
                    .ToList();
                Log.ClientDocuments(_documents);
            }

            return _documents;
        }

        public void ReportError(Exception exception) =>
            ReportError(
                ErrorBuilder.New()
                    .SetMessage(exception.Message)
                    .SetException(exception)
                    .Build());

        public void ReportError(IError error) => ReportError(new[] { error });

        public void ReportError(IEnumerable<IError> errors)
        {
            foreach (IError error in errors)
            {
                string title =
                    error.Extensions is not null &&
                    error.Extensions.TryGetValue(TitleExtensionKey, out var value) &&
                    value is string s ? s : Unexpected;

                string code = error.Code ?? Unexpected;

                if (error is { Locations: { Count: > 0 } locations } &&
                    error.Extensions is not null &&
                    error.Extensions.TryGetValue(FileExtensionKey, out value) &&
                    value is string filePath)
                {
                    ReportFileError(Execution, error, locations.First(), title, code, filePath);
                }
                else
                {
                    ReportGeneralError(Execution, error, title, code);
                }
            }
        }

        public string GetNamespace()
        {
            if (Settings.Namespace is { Length: > 0 })
            {
                return Settings.Namespace;
            }

            if (Execution.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                "build_property.StrawberryShake_DefaultNamespace",
                out string? value) &&
                !string.IsNullOrEmpty(value))
            {
                return value;
            }

            throw new GraphQLException(
                $"Specify a namespace for the client `{Settings.Name}` in the `.graphqlrc.json`.");
        }

        public string? GetPersistedQueryDirectory()
        {
            if (Execution.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                "build_property.StrawberryShake_PersistedQueryDirectory",
                out string? value) &&
                !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return null;
        }

        public string? GetStateDirectory()
        {
            if (Execution.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                "build_property.StrawberryShake_State",
                out string? value) &&
                !string.IsNullOrEmpty(value))
            {
                if (!Directory.Exists(value))
                {
                    Directory.CreateDirectory(value);
                }

                return value;
            }

            throw new GraphQLException(
                $"The MSBuild property `StrawberryShake_State` cannot be empty.");
        }
    }
}
