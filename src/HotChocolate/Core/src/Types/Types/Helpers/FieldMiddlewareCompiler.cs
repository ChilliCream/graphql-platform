#nullable enable

using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types.Helpers;

internal static class FieldMiddlewareCompiler
{
    public static FieldDelegate? Compile(
        IReadOnlyList<FieldMiddleware> globalComponents,
        IReadOnlyList<FieldMiddlewareConfiguration> fieldComponents,
        IReadOnlyList<ResultFormatterConfiguration> resultConverters,
        FieldResolverDelegate? fieldResolver,
        bool skipMiddleware)
    {
        if (skipMiddleware
            || (globalComponents.Count == 0
            && fieldComponents.Count == 0
            && resultConverters.Count == 0))
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
        IReadOnlyList<ResultFormatterConfiguration> resultConverters,
        PureFieldDelegate fieldResolver,
        bool skipMiddleware)
        => skipMiddleware || resultConverters.Count == 0
            ? fieldResolver
            : CompileResultConverters(resultConverters, fieldResolver);

    private static FieldDelegate CompilePipeline(
        IReadOnlyList<FieldMiddleware> components,
        IReadOnlyList<FieldMiddlewareConfiguration> mappedComponents,
        IReadOnlyList<ResultFormatterConfiguration> resultConverters,
        FieldResolverDelegate? fieldResolver)
        => CompileMiddlewareComponents(components,
            CompileMiddlewareComponents(mappedComponents,
                CompileResultConverters(resultConverters,
                    CreateResolverMiddleware(fieldResolver))));

    private static FieldDelegate CompileMiddlewareComponents(
        IReadOnlyList<FieldMiddleware> components,
        FieldDelegate first)
    {
        var next = first;

        for (var i = components.Count - 1; i >= 0; i--)
        {
            next = components[i](next);
        }

        return next;
    }

    private static FieldDelegate CompileMiddlewareComponents(
        IReadOnlyList<FieldMiddlewareConfiguration> components,
        FieldDelegate first)
    {
        var next = first;

        for (var i = components.Count - 1; i >= 0; i--)
        {
            next = components[i].Middleware(next);
        }

        return next;
    }

    private static FieldDelegate CompileResultConverters(
        IReadOnlyList<ResultFormatterConfiguration> components,
        FieldDelegate first)
    {
        var next = first;

        for (var i = components.Count - 1; i >= 0; i--)
        {
            next = CreateConverterMiddleware(components[i].Formatter)(next);
        }

        return next;
    }

    private static PureFieldDelegate CompileResultConverters(
        IReadOnlyList<ResultFormatterConfiguration> components,
        PureFieldDelegate first)
    {
        var next = first;

        for (var i = components.Count - 1; i >= 0; i--)
        {
            next = CreatePureConverterMiddleware(components[i].Formatter, next);
        }

        return next;
    }

    private static FieldMiddleware CreateConverterMiddleware(ResultFormatterDelegate convert)
        => n => async c =>
        {
            await n(c);
            c.Result = convert(c, c.Result);
        };

    private static PureFieldDelegate CreatePureConverterMiddleware(
        ResultFormatterDelegate convert,
        PureFieldDelegate next)
        => c =>
        {
            var result = next(c);
            return convert(c, result);
        };

    private static FieldDelegate CreateResolverMiddleware(FieldResolverDelegate? fieldResolver)
        => async ctx =>
        {
            if (!ctx.IsResultModified && fieldResolver is not null)
            {
                ctx.Result = await fieldResolver(ctx).ConfigureAwait(false);
            }
        };
}
