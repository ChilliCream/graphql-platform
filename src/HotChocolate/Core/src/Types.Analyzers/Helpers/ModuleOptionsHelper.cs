using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using static System.StringComparison;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers.Helpers;

internal static class ModuleOptionsHelper
{
    public static bool IncludeInternalMembers(this Compilation compilation)
        => (GetModuleOptions(compilation) & ModuleOptions.IncludeInternalMembers)
            == ModuleOptions.IncludeInternalMembers;

    private static ModuleOptions GetModuleOptions(Compilation compilation)
    {
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (!string.Equals(attribute.AttributeClass?.ToDisplayString(), ModuleAttribute, Ordinal))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length > 1
                && attribute.ConstructorArguments[1].Value is int optionsValue)
            {
                return (ModuleOptions)optionsValue;
            }

            return ModuleOptions.Default;
        }

        return ModuleOptions.Default;
    }
}
