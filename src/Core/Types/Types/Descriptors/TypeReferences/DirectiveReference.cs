using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public static class DirectiveReference
    {
        internal static IDirectiveReference FromDescription(
            DirectiveDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.ParsedDirective != null)
            {
                return new NameDirectiveReference(
                    definition.ParsedDirective.Name.Value);
            }

            return new ClrTypeDirectiveReference(
                definition.CustomDirective.GetType());
        }
    }
}
