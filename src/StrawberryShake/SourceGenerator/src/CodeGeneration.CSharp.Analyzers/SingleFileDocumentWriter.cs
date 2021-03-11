using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using IOPath = System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class SingleFileDocumentWriter : IDocumentWriter
    {
        private StringBuilder _content = new();
        private GeneratorExecutionContext? _execution;
        private string? _fileName;

        public void WriteDocument(ClientGeneratorContext context, SourceDocument document)
        {
            string documentName = $"{document.Name}.StrawberryShake.cs";
            context.Log.WriteDocument(documentName);

            _content.AppendLine(documentName);
            _content.AppendLine(document.SourceText);
            _content.AppendLine();

            if (context.OutputFiles && _fileName is null)
            {
                _fileName = IOPath.Combine(
                    context.OutputDirectory,
                    "Generated.StrawberryShake.cs");
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
                throw new InvalidOperationException();
            }

            _execution.Value.AddSource(
                IOPath.GetFileName(_fileName),
                SourceText.From(_content.ToString(), Encoding.UTF8));

            if (_fileName is not null)
            {
                string directory = IOPath.GetDirectoryName(_fileName);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (File.Exists(_fileName))
                {
                    File.Delete(_fileName);
                }

                File.WriteAllText(_fileName, _content.ToString(), Encoding.UTF8);
            }
        }
    }
}
