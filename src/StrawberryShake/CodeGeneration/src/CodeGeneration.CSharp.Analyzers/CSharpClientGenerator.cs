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
                    .Where(t => t.EndsWith(".graphql"));

                var generator = new CSharpGenerator();

                var generatorResult = generator.Generate(documents);
                if (generatorResult.HasErrors())
                {
                    // TODO
                }

                foreach (CSharpDocument document in generatorResult.CSharpDocuments)
                {
                    context.AddSource(
                        document.Name + ".cs",
                        SourceText.From(document.SourceText, Encoding.UTF8));
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
