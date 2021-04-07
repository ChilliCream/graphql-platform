using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using HotChocolate.Language;
using IOPath = System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class SingleFileDocumentWriter : IDocumentWriter
    {
        private readonly StringBuilder _content = new();
        private readonly MD5DocumentHashProvider _hashProvider = new();
        private GeneratorExecutionContext? _execution;
        private string? _fileName;
        private string? _hashFile;
        private bool _emitCode;

        public void WriteDocument(ClientGeneratorContext context, SourceDocument document)
        {
            string documentName = $"{document.Name}.cs";
            context.Log.WriteDocument(documentName);

            _content.AppendLine("// " + documentName);
            _content.AppendLine(document.SourceText);
            _content.AppendLine();

            if (context.OutputFiles && _fileName is null)
            {
                _fileName = IOPath.Combine(
                    context.OutputDirectory,
                    $"{context.Settings.Name}.StrawberryShake.cs");
                _hashFile = IOPath.Combine(
                    context.GetStateDirectory(),
                    context.Settings.Name + ".md5");
                _emitCode = context.Settings.EmitGeneratedCode;
                context.FileNames.Add(_fileName);
            }

            if (!_execution.HasValue)
            {
                _execution = context.Execution;
            }
        }

        public void Flush()
        {
            if (_execution is null)
            {
                return;
            }

            _execution.Value.AddSource(
                IOPath.GetFileName(_fileName),
                SourceText.From(_content.ToString(), Encoding.UTF8));

            if (_emitCode && _fileName is not null)
            {
                string? hash = _hashFile is not null && File.Exists(_hashFile)
                    ? File.ReadAllText(_hashFile)
                    : null;

                string currentHash = _hashProvider.ComputeHash(
                    Encoding.UTF8.GetBytes(_content.ToString()));

                bool fileExists = File.Exists(_fileName);

                // we only write the file if it has changed so we do not trigger a loop on
                // dotnet watch.
                if (!fileExists || !currentHash.Equals(hash, StringComparison.Ordinal))
                {
                    string directory = IOPath.GetDirectoryName(_fileName);

                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (fileExists)
                    {
                        File.Delete(_fileName);
                    }

                    File.WriteAllText(_fileName, _content.ToString(), Encoding.UTF8);
                    File.WriteAllText(_hashFile, currentHash, Encoding.UTF8);
                }
            }
        }
    }
}
