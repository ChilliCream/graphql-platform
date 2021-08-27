using System;
using HotChocolate.Analyzers.Configuration;
using HotChocolate.Analyzers.Diagnostics;
using HotChocolate.CodeGeneration;
using HotChocolate.CodeGeneration.EntityFramework;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Analyzers
{
    [Generator]
    public partial class EntityFrameworkSourceGenerator : ISourceGenerator
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
                var codeGenerator = new EntityFrameworkCodeGenerator();

                foreach (GraphQLConfig config in context.GetConfigurations())
                {
                    if (config.Extensions.EF is { } settings &&
                        context.GetSchemaDocuments(config) is { Count: > 0 } schemaDocuments)
                    {
                        var codeGeneratorContext = new CodeGeneratorContext(
                            settings.Name,
                            settings.DatabaseName,
                            settings.Namespace ?? throw new Exception("Namespace is required"), // TODO: Review
                            schemaDocuments);

                        CodeGenerationResult? result = codeGenerator.Generate(codeGeneratorContext);
                        foreach (SourceFile? sourceFile in result.SourceFiles)
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
