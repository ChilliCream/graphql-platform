using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            configuration.RegisterDirective<SchemaDirectiveType>();
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
                return dict.TryGetValue(WellKnownFieldNames.TypeName,
                    out object value)
                    && value is string name
                    && objectType.Name.Equals(name);
            }
            else if (objectType.ClrType == typeof(object))
            {
                return IsOfTypeWithName(objectType, context, resolverResult);
            }

            return IsOfTypeWithClrType(objectType, context, resolverResult);
        }

        private static bool IsOfTypeWithClrType(
            ObjectType objectType,
            IResolverContext context,
            object result)
        {
            if (result == null)
            {
                return true;
            }
            return objectType.ClrType.IsInstanceOfType(result);
        }

        private static bool IsOfTypeWithName(
            ObjectType objectType,
            IResolverContext context,
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
