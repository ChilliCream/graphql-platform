using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder UseField<TMiddleware>(
        this IRequestExecutorBuilder builder)
        where TMiddleware : class
    {
        return builder.UseField(
            FieldClassMiddlewareFactory.Create<TMiddleware>());
    }

    public static IRequestExecutorBuilder UseField<TMiddleware>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        return builder.UseField(
            FieldClassMiddlewareFactory.Create(factory));
    }

    public static IRequestExecutorBuilder MapField(
        this IRequestExecutorBuilder builder,
        FieldReference fieldReference,
        FieldMiddleware middleware)
    {
        return builder.MapFieldMiddleware(fieldReference, middleware);
    }

    public static IRequestExecutorBuilder MapField<TMiddleware>(
        this IRequestExecutorBuilder builder,
        FieldReference fieldReference)
        where TMiddleware : class
    {
        FieldMiddleware classMiddleware =
            FieldClassMiddlewareFactory.Create<TMiddleware>();

        return builder.MapFieldMiddleware(fieldReference, classMiddleware);
    }

    public static IRequestExecutorBuilder MapField<TMiddleware>(
        this IRequestExecutorBuilder builder,
        FieldReference fieldReference,
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        FieldMiddleware classMiddleware =
            FieldClassMiddlewareFactory.Create(factory);

        return builder.MapFieldMiddleware(fieldReference, classMiddleware);
    }

    public static IRequestExecutorBuilder UseField(
        this IRequestExecutorBuilder builder,
        FieldMiddleware middleware)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        return builder.ConfigureSchema(b => b.Use(middleware));
    }

    private static IRequestExecutorBuilder MapFieldMiddleware(
        this IRequestExecutorBuilder builder,
        FieldReference fieldReference,
        FieldMiddleware middleware)
        => builder
            .TryAddTypeInterceptor(typeof(ApplyFieldMiddlewareInterceptor))
            .ConfigureSchema(b => b
                .SetContextData(
                    ApplyFieldMiddlewareInterceptor.ContextKey,
                    obj =>
                    {
                        if (obj is not FieldMiddlewareLookup lookup)
                        {
                            lookup = new FieldMiddlewareLookup();
                        }

                        lookup.RegisterFieldMiddleware(fieldReference, middleware);
                        return lookup;
                    }));

    private sealed class FieldMiddlewareLookup
    {
        private readonly IDictionary<string, List<FieldMiddlewareReference>> _lookup =
            new Dictionary<string, List<FieldMiddlewareReference>>();

        public bool HasFieldMiddleware(string typeName) =>
            _lookup.ContainsKey(typeName);

        public bool TryGetFieldMiddlewares(
            string type,
            string field,
            [NotNullWhen(true)] out IEnumerable<FieldMiddleware>? middleware)
        {
            middleware = null;
            if (!_lookup.TryGetValue(type, out var references))
            {
                return false;
            }

            middleware = references
                .Where(x => x.Reference.FieldName == field && x.Reference.TypeName == type)
                .Select(x => x.Middleware);

            return true;
        }

        public void RegisterFieldMiddleware(FieldReference reference, FieldMiddleware middleware)
        {
            if (!_lookup.TryGetValue(reference.TypeName.Value, out var middlewares))
            {
                middlewares = new List<FieldMiddlewareReference>();
                _lookup[reference.TypeName.Value] = middlewares;
            }

            middlewares.Add(new(reference, middleware));
        }

        private sealed class FieldMiddlewareReference
        {
            public FieldMiddlewareReference(
                FieldReference reference,
                FieldMiddleware middleware)
            {
                Reference = reference;
                Middleware = middleware;
            }

            public FieldReference Reference { get; }

            public FieldMiddleware Middleware { get; }
        }
    }

    private sealed class ApplyFieldMiddlewareInterceptor : TypeInterceptor
    {
        public const string ContextKey = "HotChocolate.Execution.FieldMiddlewareLookup";

        public override bool CanHandle(ITypeSystemObjectContext context) =>
            context.Type is ObjectType { Name.Value: { } typeName } &&
            context.ContextData.TryGetValue(ContextKey, out var value) &&
            value is FieldMiddlewareLookup lookup &&
            lookup.HasFieldMiddleware(typeName);

        public override void OnAfterCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (!completionContext.ContextData.TryGetValue(ContextKey, out var value) ||
                value is not FieldMiddlewareLookup lookup)
            {
                return;
            }

            if (definition is not ObjectTypeDefinition def)
            {
                return;
            }

            foreach (ObjectFieldDefinition field in def.Fields)
            {
                if (lookup.TryGetFieldMiddlewares(def.Name, field.Name.Value, out var middlewares))
                {
                    foreach (FieldMiddleware middleware in middlewares)
                    {
                        var middlewareDefinition = new FieldMiddlewareDefinition(middleware);
                        field.MiddlewareDefinitions.Add(middlewareDefinition);
                    }
                }
            }
        }
    }
}
