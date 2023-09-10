using System;
using HotChocolate.Analyzers.Configuration;
using HotChocolate.Analyzers.Diagnostics;
using HotChocolate.CodeGeneration;
using HotChocolate.CodeGeneration.Neo4J;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Analyzers
{
    [Generator]
    public partial class Neo4JSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {
            _location = context.GetBinDirectory();
            ExecuteInternal(context);
        }

        private void ExecuteInternal(GeneratorExecutionContext context)
        {
            try
            {
                var codeGenerator = new Neo4JCodeGenerator();

                foreach (var config in context.GetConfigurations())
                {

                    if (config.Extensions.Neo4J is { } settings &&
                        context.GetSchemaDocuments(config) is { Count: > 0 } schemaDocuments)
                    {
                        var codeGeneratorContext = new CodeGeneratorContext(
                            settings.Name,
                            settings.DatabaseName,
                            // TODO: Review in PR!!
                            settings.Namespace ?? throw new Exception("Namespace is required"),
                            schemaDocuments);

                        var result = codeGenerator.Generate(codeGeneratorContext);
                        foreach (var sourceFile in result.SourceFiles)
                        {
                            context.AddSource(sourceFile.Name, sourceFile.Source);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                context.ReportError(ex);
            }
        }
    }
}
