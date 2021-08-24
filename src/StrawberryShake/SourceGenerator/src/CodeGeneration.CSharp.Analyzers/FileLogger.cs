using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using StrawberryShake.CodeGeneration.CSharp.Analyzers.Properties;
using StrawberryShake.Tools.Configuration;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class FileLogger : ILogger
    {
        private readonly JsonSerializerOptions _settings = new() { WriteIndented = true };
        private readonly StringBuilder _log = new();
        private readonly string _logFile;
        private DateTime _allStart;
        private DateTime _generateStart;
        private DateTime _cleanStart;

        public FileLogger(string logFile)
        {
            if (string.IsNullOrEmpty(logFile))
            {
                throw new ArgumentException(
                    string.Format(
                        AnalyzerResources.FileLogger_FileLogger_LogFileCannotBeEmpty,
                        nameof(logFile)),
                    nameof(logFile));
            }

            _logFile = logFile;
        }

        public void SetLocation(string location)
        {
            _log.AppendLine(location);
        }

        public void Begin(GraphQLConfig config, ClientGeneratorContext context)
        {
            _allStart = DateTime.UtcNow;
            _log.AppendLine($"Begin {context.Settings.Name}.");
            _log.AppendLine(config.ToString());
        }

        public void ClientDocuments(IReadOnlyList<string> documents)
        {
            foreach (var file in documents)
            {
                _log.AppendLine(file);
            }
        }

        public void BeginGenerateCode()
        {
            _generateStart = DateTime.UtcNow;
            _log.AppendLine($"Begin generate code.");
        }

        public void SetGeneratorSettings(CSharpGeneratorSettings settings)
        {
            _log.AppendLine(JsonSerializer.Serialize(settings, _settings));
        }

        public void SetPersistedQueryLocation(string? location)
        {
            _log.AppendLine("PersistedQueryLocation: " + location);
        }

        public void EndGenerateCode()
        {
            _log.AppendLine($"End generate code {DateTime.UtcNow - _generateStart}.");
        }

        public void WriteDocument(string documentName)
        {
            _log.AppendLine($"Write document {documentName}.");
        }

        public void BeginClean()
        {
            _cleanStart = DateTime.UtcNow;
            _log.AppendLine($"Begin clean.");
        }

        public void RemoveFile(string fileName)
        {
            _log.AppendLine($"Remove file {fileName}.");
        }

        public void EndClean()
        {
            _log.AppendLine($"End clean {DateTime.UtcNow - _cleanStart}.");
        }

        public void Error(Exception exception)
        {
            _log.AppendLine($"Error: {exception.GetType().FullName}");
            _log.AppendLine(exception.Message);
            _log.AppendLine(exception.StackTrace);
        }

        public void End()
        {
            _log.AppendLine($"End {DateTime.UtcNow - _allStart}.");
        }

        public void Flush()
        {
            File.AppendAllText(_logFile, _log.ToString(), Encoding.UTF8);
        }

        public void Dispose()
        {
            Flush();
        }
    }
}
