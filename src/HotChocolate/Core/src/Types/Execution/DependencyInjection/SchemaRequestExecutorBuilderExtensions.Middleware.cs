using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;
using HotChocolate.Features;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Applies a field middleware to all fields of the schema.
    /// </summary>
    /// <typeparam name="TMiddleware">
    /// The type of the middleware.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <remarks>
    /// The middleware will be applied to all fields and will wrap all fields in an asynchronous
    /// field delegate which will have big performance implications. This extension point is
    /// meant for testing scenarios. Use a type interceptor if you want to more control over
    /// where a field middleware is applied.
    /// </remarks>
    public static IRequestExecutorBuilder UseField<TMiddleware>(
        this IRequestExecutorBuilder builder)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseField(
            FieldClassMiddlewareFactory.Create<TMiddleware>());
    }

    /// <summary>
    /// Applies a field middleware to a specific field.
    /// </summary>
    /// <typeparam name="TMiddleware">
    /// The type of the middleware.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="factory">
    /// The factory that creates the middleware.
    /// </param>
    /// <returns>The request executor builder.</returns>
    /// <remarks>
    /// The middleware will be applied to all fields and will wrap all fields in an asynchronous
    /// field delegate which will have big performance implications. This extension point is
    /// meant for testing scenarios. Use a type interceptor if you want to more control over
    /// where a field middleware is applied.
    /// </remarks>
    public static IRequestExecutorBuilder UseField<TMiddleware>(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.UseField(
            FieldClassMiddlewareFactory.Create(factory));
    }

    /// <summary>
    /// Applies a field middleware to all fields of the schema.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="middleware">
    /// The middleware.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// <paramref name="middleware"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// The middleware will be applied to all fields and will wrap all fields in an asynchronous
    /// field delegate which will have big performance implications. This extension point is
    /// meant for testing scenarios. Use a type interceptor if you want to more control over
    /// where a field middleware is applied.
    /// </remarks>
    public static IRequestExecutorBuilder UseField(
        this IRequestExecutorBuilder builder,
        FieldMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(middleware);

        return builder.ConfigureSchema(b => b.Use(middleware));
    }

    /// <summary>
    /// Applies a field middleware to a specific field.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="fieldReference">
    /// The field reference.
    /// </param>
    /// <param name="middleware">
    /// The middleware.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    public static IRequestExecutorBuilder MapField(
        this IRequestExecutorBuilder builder,
        FieldReference fieldReference,
        FieldMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(fieldReference);
        ArgumentNullException.ThrowIfNull(middleware);

        return builder.MapFieldMiddleware(fieldReference, middleware);
    }

    /// <summary>
    /// Applies a field middleware to a specific field.
    /// </summary>
    /// <typeparam name="TMiddleware">
    /// The type of the middleware.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="fieldReference">
    /// The field reference.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    public static IRequestExecutorBuilder MapField<TMiddleware>(
        this IRequestExecutorBuilder builder,
        FieldReference fieldReference)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(fieldReference);

        var classMiddleware = FieldClassMiddlewareFactory.Create<TMiddleware>();
        return builder.MapFieldMiddleware(fieldReference, classMiddleware);
    }

    /// <summary>
    /// Applies a field middleware to a specific field.
    /// </summary>
    /// <typeparam name="TMiddleware">
    /// The type of the middleware.
    /// </typeparam>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="fieldReference">
    /// The field reference.
    /// </param>
    /// <param name="factory">
    /// The factory that creates the middleware.
    /// </param>
    /// <returns>
    /// The request executor builder.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// <paramref name="fieldReference"/> is <c>null</c>.
    /// <paramref name="factory"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder MapField<TMiddleware>(
        this IRequestExecutorBuilder builder,
        FieldReference fieldReference,
        Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
        where TMiddleware : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(fieldReference);
        ArgumentNullException.ThrowIfNull(factory);

        var classMiddleware = FieldClassMiddlewareFactory.Create(factory);
        return builder.MapFieldMiddleware(fieldReference, classMiddleware);
    }

    private static IRequestExecutorBuilder MapFieldMiddleware(
        this IRequestExecutorBuilder builder,
        FieldReference fieldReference,
        FieldMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(fieldReference);
        ArgumentNullException.ThrowIfNull(middleware);

        return builder
            .TryAddTypeInterceptor<ApplyFieldMiddlewareInterceptor>()
            .ConfigureSchema(b => b.Features
                .GetOrSet<FieldMiddlewareLookup>()
                .RegisterFieldMiddleware(fieldReference, middleware));
    }

    private sealed class FieldMiddlewareLookup
    {
        private readonly IDictionary<string, List<FieldMiddlewareReference>> _lookup =
            new Dictionary<string, List<FieldMiddlewareReference>>();

        public bool HasFieldMiddleware(string typeName) =>
            _lookup.ContainsKey(typeName);

        public bool TryGetFieldMiddlewares(
            string type,
            [NotNullWhen(true)] out List<FieldMiddlewareReference>? middlewareReferences)
        {
            middlewareReferences = null;
            if (!_lookup.TryGetValue(type, out var references))
            {
                return false;
            }

            middlewareReferences = references;
            return true;
        }

        public void RegisterFieldMiddleware(FieldReference reference, FieldMiddleware middleware)
        {
            if (!_lookup.TryGetValue(reference.TypeName, out var middlewares))
            {
                middlewares = [];
                _lookup[reference.TypeName] = middlewares;
            }

            middlewares.Add(new(reference, middleware));
        }
    }

    private sealed class ApplyFieldMiddlewareInterceptor : TypeInterceptor
    {
        public const string ContextKey = "HotChocolate.Execution.FieldMiddlewareLookup";

        private static bool CanHandle(ITypeSystemObjectContext context)
            => context.Type is ObjectType { Name: { } typeName }
                && context.Features.TryGet<FieldMiddlewareLookup>(out var lookup)
                && lookup.HasFieldMiddleware(typeName);

        public override void OnAfterCompleteName(
            ITypeCompletionContext completionContext,
            TypeSystemConfiguration configuration)
        {
            if (!CanHandle(completionContext))
            {
                return;
            }

            if (configuration is not ObjectTypeConfiguration def)
            {
                return;
            }

            var lookup = completionContext.Features.GetRequired<FieldMiddlewareLookup>();

            foreach (var field in def.Fields)
            {
                if (lookup.TryGetFieldMiddlewares(def.Name, out var refs))
                {
                    foreach (var middlewareRef in refs)
                    {
                        if (middlewareRef.Reference.FieldName.Equals(field.Name))
                        {
                            var middlewareDefinition = new FieldMiddlewareConfiguration(
                                middlewareRef.Middleware);
                            field.MiddlewareConfigurations.Add(middlewareDefinition);
                        }
                    }
                }
            }
        }
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
