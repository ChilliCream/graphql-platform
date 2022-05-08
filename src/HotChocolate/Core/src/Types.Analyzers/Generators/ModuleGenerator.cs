using System.Text;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using static HotChocolate.Types.Analyzers.StringConstants;
using static HotChocolate.Types.Analyzers.WellKnownFileNames;
using TypeInfo = HotChocolate.Types.Analyzers.Inspectors.TypeInfo;

namespace HotChocolate.Types.Analyzers.Generators;

public class ModuleGenerator : ISyntaxGenerator
{
    public bool Consume(ISyntaxInfo syntaxInfo)
        => syntaxInfo is TypeInfo or TypeExtensionInfo or DataLoaderInfo or ModuleInfo;

    public void Generate(
        SourceProductionContext context,
        Compilation compilation,
        IReadOnlyCollection<ISyntaxInfo> syntaxInfos)
    {
        ModuleInfo module =
            syntaxInfos.OfType<ModuleInfo>().FirstOrDefault() ??
            new ModuleInfo(
                compilation.AssemblyName is null
                    ? "AssemblyTypes"
                    : compilation.AssemblyName?.Split('.').Last() + "Types",
                ModuleOptions.Default);

        var batch = new List<ISyntaxInfo>(syntaxInfos.Where(static t => t is not ModuleInfo));
        if (batch.Count == 0)
        {
            return;
        }

        var code = new StringBuilder();
        code.AppendLine("using System;");
        code.AppendLine("using HotChocolate.Execution.Configuration;");

        code.AppendLine();
        code.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
        code.AppendLine("{");

        code.Append(Indent)
            .Append("public static class ")
            .Append(module.ModuleName)
            .AppendLine("RequestExecutorBuilderExtensions");

        code.Append(Indent)
            .AppendLine("{");

        code.Append(Indent)
            .Append(Indent)
            .Append("public static IRequestExecutorBuilder Add")
            .Append(module.ModuleName)
            .AppendLine("(this IRequestExecutorBuilder builder)");

        code.Append(Indent).Append(Indent).AppendLine("{");

        foreach (ISyntaxInfo syntaxInfo in batch.Distinct())
        {
            switch (syntaxInfo)
            {
                case TypeInfo type:
                    if ((module.Options & ModuleOptions.RegisterTypes) ==
                        ModuleOptions.RegisterTypes)
                    {
                        code.Append(Indent)
                            .Append(Indent)
                            .Append(Indent)
                            .Append("builder.AddType<")
                            .Append(type.Name)
                            .AppendLine(">();");
                    }
                    break;

                case TypeExtensionInfo extension:
                    if ((module.Options & ModuleOptions.RegisterTypes) ==
                        ModuleOptions.RegisterTypes)
                    {
                        code.Append(Indent)
                            .Append(Indent)
                            .Append(Indent)
                            .Append("builder.AddTypeExtension<")
                            .Append(extension.Name)
                            .AppendLine(">();");
                    }
                    break;

                case DataLoaderInfo dataLoader:
                    if ((module.Options & ModuleOptions.RegisterDataLoader) ==
                        ModuleOptions.RegisterDataLoader)
                    {
                        code.Append(Indent)
                            .Append(Indent)
                            .Append(Indent)
                            .Append("builder.AddDataLoader<")
                            .Append(dataLoader.Name)
                            .AppendLine(">();");
                    }
                    break;
            }
        }
        code.Append(Indent).Append(Indent).Append(Indent).AppendLine("return builder;");
        code.Append(Indent).Append(Indent).AppendLine("}");
        code.Append(Indent).AppendLine("}");
        code.AppendLine("}");

        context.AddSource(TypeModuleFile, SourceText.From(code.ToString(), Encoding.UTF8));
    }
}
