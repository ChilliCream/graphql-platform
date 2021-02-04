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
        private static readonly DiagnosticDescriptor GraphQlSyntaxError = new DiagnosticDescriptor(
            id: "SS0001",
            title: "GraphQL syntax error",
            messageFormat: "The .graphql file '{0}' has syntax errors.",
            category: "StrawberryShakeGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor SchemaValidationError =
            new DiagnosticDescriptor(
                id: "SS0002",
                title: "Schema validation error",
                messageFormat: "The .graphql file '{0}' has syntax errors.",
                category: "StrawberryShakeGenerator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

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
                    foreach (IError generatorResultError in generatorResult.Errors)
                    {
                        if (generatorResultError.Extensions is not null
                            && generatorResultError.Extensions.TryGetValue(
                                CodeGenerationThrowHelper.FileExtensionKey,
                                out var file)
                            && file is string fileString)
                        {
                            // context.ReportDiagnostic(Diagnostic.Create(
                            //     GraphQlSyntaxError,
                            //     Microsoft.CodeAnalysis.Location.Create(
                            //         fileString,
                            //         TextSpan.FromBounds(
                            //             1,
                            //             1),
                            //         new LinePositionSpan(
                            //             new LinePosition(
                            //                 1,
                            //                 1))))
                        }
                        else
                        {

                        }
                    }
                }

                foreach (CSharpDocument document in generatorResult.CSharpDocuments)
                {
                    context.AddSource(
                        document.Name + ".cs",
                        SourceText.From(
                            document.SourceText,
                            Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                context.AddSource(
                    "error.cs",
                    "/* " + ex.Message + " */");
            }
        }
    }
}
