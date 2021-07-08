using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Factories
{
    internal static class SdlToTypeSystemHelper
    {
        public static void AddDirectives(
            IHasDirectiveDefinition owner,
            HotChocolate.Language.IHasDirectives ownerSyntax)
        {
            foreach (DirectiveNode directive in ownerSyntax.Directives)
            {
                if (!directive.IsDeprecationReason())
                {
                    owner.Directives.Add(new(directive));
                }
            }
        }
    }
}
