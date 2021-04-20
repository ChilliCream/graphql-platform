using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HotChocolate.Language;
using Microsoft.CodeAnalysis.Text;
using IOPath = System.IO.Path;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class FileDocumentWriter : IDocumentWriter
    {
        private readonly MD5DocumentHashProvider _hashProvider = new(HashFormat.Hex);
        private readonly HashSet<string> _directories = new();
        private readonly bool _keepFileName;

        public FileDocumentWriter(bool keepFileName)
        {
            _keepFileName = keepFileName;
        }

        public void WriteDocument(ClientGeneratorContext context, SourceDocument document)
        {
            string documentName = CreateDocumentName(
                document.Name,
                context.Settings.Name,
                document.Kind);

            string hashName = IOPath.Combine(
                context.GetStateDirectory(),
                CreateHashName(document.Name, context.Settings.Name, document.Kind));

            context.Log.WriteDocument(documentName);

            var directory = document.Path is null
                ? context.OutputDirectory
                : IOPath.Combine(context.OutputDirectory, document.Path);

            var fileName = IOPath.Combine(directory, documentName);

            if (document.Kind == SourceDocumentKind.CSharp)
            {
                context.Execution.AddSource(
                    documentName,
                    SourceText.From(document.SourceText, Encoding.UTF8));
            }

            if (context.Settings.EmitGeneratedCode)
            {
                context.FileNames.Add(fileName);

                var hash = File.Exists(hashName)
                    ? File.ReadAllText(hashName)
                    : null;

                string currentHash = _hashProvider.ComputeHash(
                    Encoding.UTF8.GetBytes(document.SourceText));

                var fileExists = File.Exists(fileName);

                if (!fileExists || !currentHash.Equals(hash, StringComparison.Ordinal))
                {
                    if (_directories.Add(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (fileExists)
                    {
                        File.Delete(fileName);
                    }

                    File.WriteAllText(fileName, document.SourceText, Encoding.UTF8);
                    File.WriteAllText(hashName, currentHash, Encoding.UTF8);
                }
            }
        }

        private string CreateDocumentName(
            string fileName,
            string clientName,
            SourceDocumentKind kind)
        {
            if (kind == SourceDocumentKind.Razor)
            {
                return _keepFileName
                    ? $"{fileName}.Components.cs"
                    : $"{fileName}.{clientName}.Component.cs";
            }

            return _keepFileName
                ? $"{fileName}.StrawberryShake.cs"
                : $"{fileName}.{clientName}.StrawberryShake.cs";
        }

        private string CreateHashName(
            string fileName,
            string clientName,
            SourceDocumentKind kind)
        {
            return $"{clientName}.{fileName}.{kind}.md5";
        }

        public void Flush()
        {
        }
    }
}
