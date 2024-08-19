using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.FileBuilders;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class MiddlewareGenerator : ISyntaxGenerator
{
    private const string _namespace = "HotChocolate.Execution.Generated";

    public void Generate(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        if (syntaxInfos.IsEmpty)
        {
            return;
        }

        var module = syntaxInfos.GetModuleInfo(compilation.AssemblyName, out _);

        // the generator is disabled.
        if(module.Options == ModuleOptions.Disabled)
        {
            return;
        }

        if((module.Options & ModuleOptions.RegisterTypes) != ModuleOptions.RegisterTypes)
        {
            return;
        }

        // if there is only the module info we do not need to generate a module.
        if (!syntaxInfos.Any(t => t is RequestMiddlewareInfo))
        {
            return;
        }

        using var generator = new RequestMiddlewareFileBuilder(module.ModuleName, _namespace);

        generator.WriteHeader();
        generator.WriteBeginNamespace();

        generator.WriteBeginClass();

        var i = 0;
        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is not RequestMiddlewareInfo middleware)
            {
                continue;
            }

            generator.WriteFactory(i, middleware);
            generator.WriteInterceptMethod(i, middleware.Location);
            i++;
        }

        generator.WriteEndClass();

        generator.WriteEndNamespace();

        context.AddSource(WellKnownFileNames.MiddlewareFile, generator.ToSourceText());
    }
}
