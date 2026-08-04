using HotChocolate.Features;
using HotChocolate.Fusion.Features;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class ComplexTypeDefinitionExtensions
{
    extension(IComplexTypeDefinition complexType)
    {
        public IEnumerable<IDirective> GetKeyDirectives()
        {
            return complexType.Directives.AsEnumerable().Where(d => d.Name == DirectiveNames.Key);
        }

        public Dictionary<Directive, KeyInfo> KeyInfoByDirective
            => complexType.Features.GetOrSet<SourceComplexTypeMetadata>().KeyInfoByDirective;
    }
}
