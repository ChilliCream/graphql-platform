using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.Extensions;

internal static class MutableInputFieldDefinitionExtensions
{
    extension(MutableInputFieldDefinition field)
    {
        public void ApplyIsDirective(string fieldName)
        {
            var isDirectiveExists =
                field.Directives.AsEnumerable().Any(d => d.Name == DirectiveNames.Is);

            if (!isDirectiveExists)
            {
                field.Directives.Add(
                    new Directive(
                        FusionBuiltIns.SourceSchemaDirectives[DirectiveNames.Is],
                        new ArgumentAssignment(ArgumentNames.Field, fieldName)));
            }
        }
    }
}
