using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.WellKnownArgumentNames;
using static HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableObjectTypeDefinitionExtensions
{
    public static void ApplyShareableDirective(this MutableObjectTypeDefinition type)
    {
        type.Directives.Add(new Directive(new MutableDirectiveDefinition(Shareable)));
    }

    public static bool ExistsInSchema(this MutableObjectTypeDefinition type, string schemaName)
    {
        return type.Directives.AsEnumerable().Any(
            d => d.Name == FusionType && (string)d.Arguments[Schema].Value! == schemaName);
    }

    public static IEnumerable<IDirective> GetFusionLookupDirectives(
        this MutableObjectTypeDefinition type,
        string schemaName,
        IEnumerable<MutableUnionTypeDefinition> unionTypes)
    {
        var lookupDirectives =
            type.Directives
                .AsEnumerable()
                .Where(
                    d =>
                        d.Name == FusionLookup
                        && (string)d.Arguments[Schema].Value! == schemaName)
                .ToList();

        // To use an abstract lookup, the type must exist in the source schema.
        if (type.ExistsInSchema(schemaName))
        {
            // Interface lookups.
            foreach (var interfaceType in type.Implements)
            {
                lookupDirectives.AddRange(
                    interfaceType.Directives
                        .AsEnumerable()
                        .Where(d =>
                            d.Name == FusionLookup
                            && (string)d.Arguments[Schema].Value! == schemaName));
            }

            // Union lookups.
            foreach (var unionType in unionTypes)
            {
                lookupDirectives.AddRange(
                    unionType.Directives
                        .AsEnumerable()
                        .Where(d =>
                            d.Name == FusionLookup
                            && (string)d.Arguments[Schema].Value! == schemaName));
            }
        }

        return lookupDirectives;
    }

    public static IEnumerable<IDirective> GetFusionLookupDirectivesById(
        this MutableObjectTypeDefinition type,
        IEnumerable<MutableUnionTypeDefinition> unionTypes)
    {
        var lookups = new List<IDirective>();
        var sourceSchemaNames = type.Directives.AsEnumerable()
            .Where(d => d.Name == FusionType)
            .Select(d => (string)d.Arguments[Schema].Value!);
        unionTypes = unionTypes.ToList();

        foreach (var sourceSchemaName in sourceSchemaNames)
        {
            foreach (var lookupDirective in type.GetFusionLookupDirectives(sourceSchemaName, unionTypes))
            {
                if (lookupDirective.Arguments[Map] is ListValueNode { Items.Count: 1 } mapArg
                    && mapArg.Items[0].Value?.Equals(Id) == true)
                {
                    lookups.Add(lookupDirective);
                }
            }
        }

        return lookups;
    }

    public static bool HasInternalDirective(this MutableObjectTypeDefinition type)
    {
        return type.Directives.ContainsName(Internal);
    }
}
