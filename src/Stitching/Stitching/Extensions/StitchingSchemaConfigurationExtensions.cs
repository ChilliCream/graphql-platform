using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Stitching;
using HotChocolate.Types;

namespace HotChocolate
{
    public static class StitchingSchemaConfigurationExtensions
    {
        public static ISchemaConfiguration UseNullResolver(
            this ISchemaConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.Use(next => context => Task.CompletedTask);
            return configuration;
        }

        public static ISchemaConfiguration UseSchemaStitching(
            this ISchemaConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            configuration.RegisterDirective<DelegateDirectiveType>();
            configuration.RegisterDirective<ComputedDirectiveType>();
            configuration.RegisterDirective<SourceDirectiveType>();
            configuration.RegisterIsOfType(IsOfTypeFallback);
            configuration.Use<DelegateToRemoteSchemaMiddleware>();
            configuration.Use<DictionaryResultMiddleware>();
            return configuration;
        }

        private static bool IsOfTypeFallback(
            ObjectType objectType,
            IResolverContext context,
            object resolverResult)
        {
            if (resolverResult is IReadOnlyDictionary<string, object> dict)
            {
                if (dict.TryGetValue(WellKnownFieldNames.TypeName, out object v)
                    && v is string typeName)
                {
                    if (objectType.Directives.Contains(DirectiveNames.Source)
                        && context.ScopedContextData.TryGetValue(
                            WellKnownProperties.SchemaName, out object o)
                        && o is NameString schemaName
                        && objectType.TryGetSourceDirective(schemaName,
                            out SourceDirective sourceDirective))
                    {
                        return sourceDirective.Name.Equals(typeName);
                    }

                    return objectType.Name.Equals(typeName);
                }
            }
            else if (objectType.ClrType == typeof(object))
            {
                return IsOfTypeWithName(objectType, resolverResult);
            }

            return IsOfTypeWithClrType(objectType, resolverResult);
        }

        private static bool IsOfTypeWithClrType(
            IHasClrType objectType,
            object result)
        {
            if (result == null)
            {
                return true;
            }
            return objectType.ClrType.IsInstanceOfType(result);
        }

        private static bool IsOfTypeWithName(
            IHasName objectType,
            object result)
        {
            if (result == null)
            {
                return true;
            }
            return objectType.Name.Equals(result.GetType().Name);
        }
    }
}
