using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal static class FieldMiddlewareCompiler
    {
        public static FieldDelegate Compile(
            IReadOnlyList<FieldMiddleware> globalComponents,
            IReadOnlyList<FieldMiddleware> fieldComponents,
            FieldResolverDelegate fieldResolver,
            bool skipMiddleware)
        {
            if (globalComponents == null)
            {
                throw new ArgumentNullException(nameof(globalComponents));
            }

            if (fieldComponents == null)
            {
                throw new ArgumentNullException(nameof(fieldComponents));
            }

            if (skipMiddleware
                || (globalComponents.Count == 0
                    && fieldComponents.Count == 0))
            {
                if (fieldResolver == null)
                {
                    return null;
                }
                return CreateResolverMiddleware(fieldResolver);
            }

            return BuildMiddleware(
                globalComponents,
                fieldComponents,
                fieldResolver);
        }

        private static FieldDelegate BuildMiddleware(
            IReadOnlyList<FieldMiddleware> components,
            IReadOnlyList<FieldMiddleware> mappedComponents,
            FieldResolverDelegate fieldResolver)
        {
            return IntegrateComponents(components,
                IntegrateComponents(mappedComponents,
                    CreateResolverMiddleware(fieldResolver)));
        }

        private static FieldDelegate IntegrateComponents(
            IReadOnlyList<FieldMiddleware> components,
            FieldDelegate first)
        {
            FieldDelegate next = first;

            for (int i = components.Count - 1; i >= 0; i--)
            {
                next = components[i].Invoke(next);
            }

            return next;
        }

        private static FieldDelegate CreateResolverMiddleware(
            FieldResolverDelegate fieldResolver)
        {
            return async ctx =>
            {
                if (!ctx.IsResultModified && fieldResolver != null)
                {
                    Task<object> task = fieldResolver.Invoke(ctx);
                    ctx.Result = task == null
                        ? null
                        : await task.ConfigureAwait(false);
                }
            };
        }
    }

}
