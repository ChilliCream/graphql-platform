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
        ImmutableArray<ISyntaxInfo> syntaxInfos)
    {
        if (syntaxInfos.IsEmpty)
        {
            return;
        }

        var module = syntaxInfos.GetModuleInfo(compilation.AssemblyName, out var defaultModule);

        // if there is only the module info we do not need to generate a module.
        if (!defaultModule && syntaxInfos.Length == 1)
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
