using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableComplexTypeDefinitionExtensions
{
    extension(MutableComplexTypeDefinition complexType)
    {
        public void ApplyKeyDirective(string keyFields)
        {
            var keyDirectiveExists =
                complexType.Directives.AsEnumerable().Any(
                    d =>
                        d.Name == DirectiveNames.Key
                        && ((StringValueNode)d.Arguments[ArgumentNames.Fields]).Value.Equals(keyFields));

            if (!keyDirectiveExists)
            {
                complexType.Directives.Add(
                    new Directive(
                        FusionBuiltIns.SourceSchemaDirectives[DirectiveNames.Key],
                        new ArgumentAssignment(ArgumentNames.Fields, keyFields)));
            }
        }
    }
}
