using System.Collections.Generic;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    internal static class FieldMiddlewareCompiler
    {
        public static FieldDelegate Compile(
            IReadOnlyList<FieldMiddleware> globalComponents,
            IReadOnlyList<FieldMiddleware> fieldComponents,
            IReadOnlyList<ResultConverterDelegate> resultConverters,
            FieldResolverDelegate fieldResolver,
            bool skipMiddleware)
        {
            if (skipMiddleware ||
                globalComponents.Count is 0 &&
                fieldComponents.Count is 0 &&
                resultConverters.Count is 0)
            {
                return fieldResolver is null
                    ? null
                    : CreateResolverMiddleware(fieldResolver);
            }

            return CompilePipeline(
                globalComponents,
                fieldComponents,
                resultConverters,
                fieldResolver);
        }

        public static PureFieldDelegate Compile(
            IReadOnlyList<ResultConverterDelegate> resultConverters,
            PureFieldDelegate fieldResolver,
            bool skipMiddleware)
            => skipMiddleware || resultConverters.Count == 0
                ? fieldResolver
                : CompileResultConverters(resultConverters, fieldResolver);

        private static FieldDelegate CompilePipeline(
            IReadOnlyList<FieldMiddleware> components,
            IReadOnlyList<FieldMiddleware> mappedComponents,
            IReadOnlyList<ResultConverterDelegate> resultConverters,
            FieldResolverDelegate fieldResolver)
            => CompileMiddlewareComponents(components,
                CompileMiddlewareComponents(mappedComponents,
                    CompileResultConverters(resultConverters,
                        CreateResolverMiddleware(fieldResolver))));

        private static FieldDelegate CompileMiddlewareComponents(
            IReadOnlyList<FieldMiddleware> components,
            FieldDelegate first)
        {
            FieldDelegate next = first;

            for (var i = components.Count - 1; i >= 0; i--)
            {
                next = components[i](next);
            }

            return next;
        }

        private static FieldDelegate CompileResultConverters(
            IReadOnlyList<ResultConverterDelegate> components,
            FieldDelegate first)
        {
            FieldDelegate next = first;

            for (var i = components.Count - 1; i >= 0; i--)
            {
                next = CreateConverterMiddleware(components[i])(next);
            }

            return next;
        }

        private static PureFieldDelegate CompileResultConverters(
            IReadOnlyList<ResultConverterDelegate> components,
            PureFieldDelegate first)
        {
            PureFieldDelegate next = first;

            for (var i = components.Count - 1; i >= 0; i--)
            {
                next = CreatePureConverterMiddleware(components[i], next);
            }

            return next;
        }

        private static FieldMiddleware CreateConverterMiddleware(ResultConverterDelegate convert)
            => n => async c =>
            {
                await n(c);
                c.Result = convert(c, c.Result);
            };

        private static PureFieldDelegate CreatePureConverterMiddleware(
            ResultConverterDelegate convert,
            PureFieldDelegate next)
            => c =>
            {
                var result = next(c);
                return convert(c, result);
            };

        private static FieldDelegate CreateResolverMiddleware(FieldResolverDelegate fieldResolver)
            => async ctx =>
            {
                if (!ctx.IsResultModified && fieldResolver is not null)
                {
                    ctx.Result = await fieldResolver(ctx).ConfigureAwait(false);
                }
            };
    }
}
