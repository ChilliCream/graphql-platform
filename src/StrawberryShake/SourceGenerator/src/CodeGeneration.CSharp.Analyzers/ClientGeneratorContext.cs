using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using DotNet.Globbing;
using HotChocolate;
using HotChocolate.Language;
using Newtonsoft.Json;
using StrawberryShake.Tools.Configuration;
using IOPath = System.IO.Path;
using static StrawberryShake.CodeGeneration.ErrorHelper;
using static StrawberryShake.CodeGeneration.CSharp.Analyzers.SourceGeneratorErrorCodes;
using static StrawberryShake.CodeGeneration.CSharp.Analyzers.DiagnosticErrorHelper;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class ClientGeneratorContext
    {
        private readonly MD5DocumentHashProvider _hashProvider = new(HashFormat.Hex);
        private readonly IReadOnlyList<string> _allDocuments;
        private IReadOnlyList<string>? _documents;
        private string? _stateDirectory;

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
                settings.OutputDirectoryName);
            _allDocuments = allDocuments;
            Execution = execution;
            Log = log;
        }

        public HashSet<string> FileNames { get; } = new();

        public StrawberryShakeSettings Settings { get; }

        public string Filter { get; }

        public string ClientDirectory { get; }

        public string OutputDirectory { get; }

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

        public IReadOnlyList<SourceDocument> GetLastSuccessfulGeneratedSourceDocuments()
        {
            string fileName = IOPath.Combine(GetStateDirectory(), Settings.Name + ".code");

            if (File.Exists(fileName))
            {
                try
                {
                    string json = File.ReadAllText(fileName);
                    return JsonConvert
                        .DeserializeObject<IEnumerable<SourceDocumentDto>>(json)
                        .Select(dto => new SourceDocument(
                            dto.Name, 
                            dto.SourceText, 
                            dto.Kind, 
                            dto.Hash, 
                            dto.Path))
                        .ToArray();
                }
                catch
                {
                    // we ignore any error here.
                }
            }

            return Array.Empty<SourceDocument>();
        }

        public void PreserveSourceDocuments(IReadOnlyList<SourceDocument> sourceDocuments)
        {
            string fileName = IOPath.Combine(GetStateDirectory(), Settings.Name + ".code");

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            File.WriteAllText(
                fileName,
                JsonConvert.SerializeObject(
                    sourceDocuments.Select(doc => new SourceDocumentDto 
                    { 
                        Name = doc.Name,
                        Path = doc.Path,
                        Hash = doc.Hash,
                        Kind = doc.Kind,
                        SourceText = doc.SourceText
                    })));
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
                out var value) &&
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
                out var value) &&
                !string.IsNullOrEmpty(value))
            {
                return value;
            }

            return null;
        }

        public string GetStateDirectory()
        {
            if (_stateDirectory is not null)
            {
                return _stateDirectory;
            }

            if (Execution.AnalyzerConfigOptions.GlobalOptions.TryGetValue(
                "build_property.StrawberryShake_State",
                out var value) &&
                !string.IsNullOrEmpty(value))
            {
                _stateDirectory = value;
            }
            else
            {
                string hash = _hashProvider.ComputeHash(
                    Encoding.UTF8.GetBytes($"{Settings.Namespace}.{Settings.Name}"));
                _stateDirectory = IOPath.Combine(IOPath.GetTempPath(), hash);
            }

            if (!Directory.Exists(_stateDirectory))
            {
                Directory.CreateDirectory(_stateDirectory);
            }

            return _stateDirectory;
        }

        private class SourceDocumentDto
        {
            public string Name { get; set; } = default!;
            public string SourceText { get; set; } = default!;
            public SourceDocumentKind Kind { get; set; }
            public string? Hash { get; set; }
            public string? Path { get; set; }
        }
    }
}
