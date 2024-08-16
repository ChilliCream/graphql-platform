using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.FileBuilders;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public class DataLoaderModuleGenerator : ISyntaxGenerator
{
    public void Generate(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        var module = GetDataLoaderModuleInfo(syntaxInfos);

        if (module is null || !syntaxInfos.Any(t => t is DataLoaderInfo or RegisterDataLoaderInfo))
        {
            return;
        }

        var generator = new DataLoaderModuleFileBuilder(module.ModuleName);

        generator.WriteHeader();
        generator.WriteBeginNamespace();
        generator.WriteBeginClass();
        generator.WriteBeginRegistrationMethod();

        foreach (var syntaxInfo in syntaxInfos)
        {
            switch (syntaxInfo)
            {
                case RegisterDataLoaderInfo dataLoader:
                    generator.WriteAddDataLoader(dataLoader.Name);
                    break;

                case DataLoaderInfo dataLoader:
                    var typeName = $"{dataLoader.Namespace}.{dataLoader.Name}";
                    var interfaceTypeName = $"{dataLoader.Namespace}.{dataLoader.InterfaceName}";
                    generator.WriteAddDataLoader(typeName, interfaceTypeName);
                    break;
            }
        }

        generator.WriteEndRegistrationMethod();
        generator.WriteEndClass();
        generator.WriteEndNamespace();

        context.AddSource(WellKnownFileNames.DataLoaderModuleFile, generator.ToSourceText());
    }

    private static DataLoaderModuleInfo? GetDataLoaderModuleInfo(
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        foreach (var syntaxInfo in syntaxInfos)
        {
            if (syntaxInfo is DataLoaderModuleInfo module)
            {
                return module;
            }
        }

        return null;
    }
}
