using System.Collections.Immutable;
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

        /// <summary>
        /// Gets the merged definitions of the types that set each context the argument
        /// references in the source schema that declares the argument, keyed by context name.
        /// </summary>
        public Dictionary<string, ImmutableArray<ITypeDefinition>> GetContextTypes(
            MutableSchemaDefinition sourceSchema,
            MutableSchemaDefinition mergedSchema)
        {
            var contextTypes = new Dictionary<string, ImmutableArray<ITypeDefinition>>();

            if (field.RequireInfo is not { ContextNames.Length: > 0 } requireInfo)
            {
                return contextTypes;
            }

            foreach (var contextName in requireInfo.ContextNames)
            {
                var types = ImmutableArray.CreateBuilder<ITypeDefinition>();

                foreach (var sourceType in sourceSchema.Types)
                {
                    if (sourceType.SetsContext(contextName)
                        && mergedSchema.Types.TryGetType(sourceType.Name, out var mergedType))
                    {
                        types.Add(mergedType);
                    }
                }

                contextTypes[contextName] = types.ToImmutable();
            }

            return contextTypes;
        }
    }
}
