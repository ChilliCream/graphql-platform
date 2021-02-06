using System;
using System.Linq;
using System.Reflection;
using System.Text;
using HotChocolate;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    [Generator]
    public class CSharpClientGenerator : ISourceGenerator
    {
        private static string _location = System.IO.Path.GetDirectoryName(
            typeof(CSharpClientGenerator).Assembly.Location);

        public void Initialize(GeneratorInitializationContext context)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
        }

        private static Assembly? CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                AssemblyName assemblyName = new AssemblyName(args.Name);
                if (assemblyName.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase)
                    && !assemblyName.CultureName.EndsWith("neutral", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                string path = System.IO.Path.Combine(_location, assemblyName.Name + ".dll");
                return Assembly.LoadFrom(path);
            }
            catch
            {
                return null;
            }
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
                        string title = (generatorResultError
                                           .Extensions?[CodeGenerationThrowHelper
                                               .TitleExtensionKey] as string)
                                       ?? throw new ArgumentNullException();

                        string code = generatorResultError.Code ??
                                      throw new ArgumentNullException();

                        if (generatorResultError.Extensions is not null
                            && generatorResultError.Extensions.TryGetValue(
                                CodeGenerationThrowHelper.FileExtensionKey,
                                out var file)
                            && file is string fileString)
                        {
                            HotChocolate.Location location =
                                generatorResultError.Locations?.First() ??
                                throw new ArgumentNullException();

                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    new DiagnosticDescriptor(
                                        id: code,
                                        title: title,
                                        messageFormat: "The .graphql file '{0}' has errors.",
                                        category: "StrawberryShakeGenerator",
                                        DiagnosticSeverity.Error,
                                        isEnabledByDefault: true,
                                        description: generatorResultError.Message),
                                    Microsoft.CodeAnalysis.Location.Create(
                                        fileString,
                                        TextSpan.FromBounds(
                                            1,
                                            2),
                                        new LinePositionSpan(
                                            new LinePosition(
                                                location.Line,
                                                location.Column),
                                            new LinePosition(
                                                location.Line,
                                                location.Column + 1)))));
                        }
                        else
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    new DiagnosticDescriptor(
                                        id: code,
                                        title: title,
                                        messageFormat: "An error occured during generation.",
                                        category: "StrawberryShakeGenerator",
                                        DiagnosticSeverity.Error,
                                        isEnabledByDefault: true,
                                        description: generatorResultError.Message),
                                    Microsoft.CodeAnalysis.Location.None));
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
                    "/* 12 " + ex.Message + " " + ex.StackTrace + "  */");
            }
        }
    }
}
