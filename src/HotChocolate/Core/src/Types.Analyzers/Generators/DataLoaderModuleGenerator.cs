using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.FileBuilders;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class DataLoaderModuleGenerator : ISyntaxGenerator
{
    public void Generate(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<SyntaxInfo> syntaxInfos)
    {
        var module = GetDataLoaderModuleInfo(syntaxInfos);
        var dataLoaderDefaults = syntaxInfos.GetDataLoaderDefaults();

        if (module is null || !syntaxInfos.Any(t => t is DataLoaderInfo or RegisterDataLoaderInfo))
        {
            return;
        }

        HashSet<(string InterfaceName, string ClassName)>? groups = null;
        var generator = new DataLoaderModuleFileBuilder(module.ModuleName);

        generator.WriteHeader();
        generator.WriteBeginNamespace();
        generator.WriteBeginClass();
        generator.WriteBeginRegistrationMethod();

        foreach (var syntaxInfo in syntaxInfos)
        {
            if(syntaxInfo.Diagnostics.Length > 0)
            {
                continue;
            }

            switch (syntaxInfo)
            {
                case RegisterDataLoaderInfo dataLoader:
                    generator.WriteAddDataLoader(dataLoader.Name);
                    break;

                case DataLoaderInfo dataLoader:
                    var typeName = $"{dataLoader.Namespace}.{dataLoader.Name}";
                    var interfaceTypeName = $"{dataLoader.Namespace}.{dataLoader.InterfaceName}";
                    generator.WriteAddDataLoader(typeName, interfaceTypeName, dataLoaderDefaults.GenerateInterfaces);

                    if(dataLoader.Groups.Count > 0)
                    {
                        groups ??= [];
                        foreach (var groupName in dataLoader.Groups)
                        {
                            groups.Add(($"{dataLoader.Namespace}.I{groupName}", $"{dataLoader.Namespace}.{groupName}"));
                        }
                    }
                    break;
            }
        }

        if (groups is not null)
        {
            foreach (var (interfaceName, className) in groups.OrderBy(t => t.ClassName))
            {
                generator.WriteAddDataLoaderGroup(className, interfaceName);
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
