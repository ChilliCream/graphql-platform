using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    internal static class TypeDependencyHelper
    {
        public static void RegisterDependencies(
            this IInitializationContext context,
            ObjectTypeDefinition definition)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            context.RegisterDependencyRange(
                definition.Interfaces,
                TypeDependencyKind.Default);

            context.RegisterDependencyRange(
                definition.Fields
                    .Where(t => t.Type != null)
                    .Select(t => t.Type),
                TypeDependencyKind.Default);

            context.RegisterDependencyRange(
                definition.Fields.SelectMany(t => t.Arguments)
                    .Where(t => t.Type != null)
                    .Select(t => t.Type),
                TypeDependencyKind.Completed);

            context.RegisterDependencyRange(
                definition.Directives.Select(t => t.TypeReference),
                TypeDependencyKind.Completed);

            context.RegisterDependencyRange(
                definition.Fields.SelectMany(t => t.Directives)
                    .Select(t => t.TypeReference),
                TypeDependencyKind.Completed);

            context.RegisterDependencyRange(
                definition.Fields.SelectMany(t => t.Arguments)
                    .SelectMany(t => t.Directives)
                    .Select(t => t.TypeReference),
                TypeDependencyKind.Completed);
        }
    }
}
