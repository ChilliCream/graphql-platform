using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Analyzers.Configuration;
using HotChocolate.Analyzers.Diagnostics;
using HotChocolate.Analyzers.Types;
using HotChocolate.Analyzers.Types.EFCore;
using HotChocolate.CodeGeneration;
using HotChocolate.CodeGeneration.EntityFramework;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static HotChocolate.Analyzers.TypeNames;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace HotChocolate.Analyzers
{
    [Generator]
    public partial class EFCoreSourceGenerator : ISourceGenerator
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
                            settings.Namespace ?? throw new Exception("Namespace is required"), // TODO: Review in PR!!
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
