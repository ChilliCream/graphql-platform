using HotChocolate.Features;
using HotChocolate.Fusion.Features;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableComplexTypeDefinitionExtensions
{
    public static void ApplyKeyDirective(this MutableComplexTypeDefinition type, string keyFields)
    {
        var keyDirectiveExists =
            type.Directives.AsEnumerable().Any(
                d =>
                    d.Name == DirectiveNames.Key
                    && ((StringValueNode)d.Arguments[ArgumentNames.Fields]).Value.Equals(keyFields));

        if (!keyDirectiveExists)
        {
            type.Directives.Add(
                new Directive(
                    FusionBuiltIns.SourceSchemaDirectives[DirectiveNames.Key],
                    new ArgumentAssignment(ArgumentNames.Fields, keyFields)));
        }
    }

    public static IEnumerable<Directive> GetKeyDirectives(this MutableComplexTypeDefinition type)
    {
        return type.Directives.AsEnumerable().Where(d => d.Name == DirectiveNames.Key);
    }

    extension(MutableComplexTypeDefinition complexType)
    {
        public Dictionary<Directive, KeyInfo> KeyInfoByDirective
            => complexType.Features.GetOrSet<SourceComplexTypeMetadata>().KeyInfoByDirective;
    }
}
