using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class FileLogger : ILogger
    {
        private readonly JsonSerializerOptions _settings = new() { WriteIndented = true };
        private readonly StringBuilder _log = new();
        private readonly string _logFile;
        private ClientGeneratorContext? _context;
        private DateTime _allStart;
        private DateTime _generateStart;
        private DateTime _cleanStart;

        public FileLogger(string logFile)
        {
            if (string.IsNullOrEmpty(logFile))
            {
                throw new ArgumentException(
                    $"'{nameof(logFile)}' cannot be null or empty.", nameof(logFile));
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
            _context = context;
            _log.AppendLine($"Begin {context.Settings.Name}.");
            _log.AppendLine(JsonSerializer.Serialize(config, _settings));
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

        public void Dispose()
        {
            File.AppendAllText(_logFile, _log.ToString(), Encoding.UTF8);
        }
    }
}
