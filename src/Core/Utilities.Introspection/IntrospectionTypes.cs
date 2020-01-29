using System.Collections.Generic;
using HotChocolate.Language;
using static HotChocolate.Utilities.Introspection.WellKnownTypes;
using static HotChocolate.Utilities.Introspection.WellKnownDirectives;

namespace HotChocolate.Utilities.Introspection
{
    internal static class BuiltInTypes
    {
        private static readonly HashSet<string> _typeNames =
            new HashSet<string>
            {
                __Directive,
                __DirectiveLocation,
                __EnumValue,
                __Field,
                __InputValue,
                __Schema,
                __Type,
                __TypeKind,
                String,
                Boolean,
                Float,
                ID,
                Int,
            };

        private static readonly HashSet<string> _directiveNames =
            new HashSet<string>
            {
                Skip,
                Include,
                Deprecated
            };

        public static DocumentNode RemoveBuiltInTypes(this DocumentNode schema)
        {
            var definitions = new List<IDefinitionNode>();

            foreach (IDefinitionNode definition in schema.Definitions)
            {
                if (definition is INamedSyntaxNode type)
                {
                    if (!_typeNames.Contains(type.Name.Value))
                    {
                        definitions.Add(definition);
                    }
                }
                else if (definition is DirectiveDefinitionNode directive)
                {
                    if (!_directiveNames.Contains(directive.Name.Value))
                    {
                        definitions.Add(definition);
                    }
                }
                else
                {
                    definitions.Add(definition);
                }
            }

            return new DocumentNode(definitions);
        }
    }
}
