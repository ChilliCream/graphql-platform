using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using IOPath = System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class SingleFileDocumentWriter : IDocumentWriter
    {
        private StringBuilder _content = new();
        private string? _fileName;

        public void WriteDocument(ClientGeneratorContext context, SourceDocument document)
        {
            string documentName = $"{document.Name}.StrawberryShake.cs";
            context.Log.WriteDocument(documentName);

            context.Execution.AddSource(
                documentName,
                SourceText.From(document.SourceText, Encoding.UTF8));

            if (context.OutputFiles)
            {
                if (_fileName is null)
                {
                    _fileName = IOPath.Combine(
                        context.OutputDirectory,
                        "Generated.StrawberryShake.cs");
                    context.FileNames.Add(_fileName);
                }

                _content.AppendLine(documentName);
                _content.AppendLine(document.SourceText);
                _content.AppendLine();
            }
        }

        public void Flush()
        {
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
