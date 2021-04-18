using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using IOPath = System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class FileDocumentWriter : IDocumentWriter
    {
        private readonly HashSet<string> _directories = new();

        public void WriteDocument(ClientGeneratorContext context, SourceDocument document)
        {
            string documentName = $"{document.Name}.{context.Settings.Name}.StrawberryShake.cs";
            context.Log.WriteDocument(documentName);

            var directory = document.Path is null
                ? context.OutputDirectory
                : IOPath.Combine(context.OutputDirectory, document.Path);

            if (context.Settings.EmitGeneratedCode &&
                _directories.Add(directory) &&
                !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var fileName = IOPath.Combine(directory, documentName);

            context.Execution.AddSource(
                documentName,
                SourceText.From(document.SourceText, Encoding.UTF8));

            if (context.Settings.EmitGeneratedCode)
            {
                context.FileNames.Add(fileName);
                WriteFile(fileName, document.SourceText);
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

        public void Flush()
        {
        }
    }
}
