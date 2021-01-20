using System;
using System.IO;
using System.Linq;
using System.Text;
using HotChocolate;
using HotChocolate.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    [Generator]
    public class CSharpClientGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                var documents = context.AdditionalFiles
                    .Select(t => t.Path)
                    .Where(t => t.EndsWith(".graphql"))
                    .Select(t => Utf8GraphQLParser.Parse(File.ReadAllBytes(t)))
                    .ToList();

                var typeSystemDocs = documents.GetTypeSystemDocuments().ToList();
                var executableDocs = documents.GetExecutableDocuments().ToList();

                if (typeSystemDocs.Count == 0 || executableDocs.Count == 0)
                {
                    return;
                }

                ISchema schema = SchemaHelper.Load(typeSystemDocs);

                var analyzer = new DocumentAnalyzer();
                analyzer.SetSchema(schema);

                foreach (DocumentNode executableDocument in executableDocs)
                {
                    analyzer.AddDocument(executableDocument);
                }

                ClientModel clientModel = analyzer.Analyze();

                var executor = new CSharpGeneratorExecutor();

                foreach (CSharpDocument document in executor.Generate(clientModel, "Foo"))
                {
                    context.AddSource(
                        document.Name + ".cs",
                        SourceText.From(document.Source, Encoding.UTF8));
                }

            }
            catch(Exception ex)
            {
                context.AddSource(
                    "error.cs",
                    "/* " + ex.Message + " */");
            }
        }
    }
}
