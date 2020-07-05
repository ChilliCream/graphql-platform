using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Resolvers;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
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
            return builder.UseField(
                FieldClassMiddlewareFactory.Create(
                    (s, n) => new MapMiddleware(
                        n, fieldReference, middleware(n))));
        }

        public static IRequestExecutorBuilder MapField<TMiddleware>(
            this IRequestExecutorBuilder builder,
            FieldReference fieldReference)
            where TMiddleware : class
        {
            return builder.UseField(
                FieldClassMiddlewareFactory.Create(
                    (s, n) =>
                    {
                        FieldMiddleware classMiddleware =
                            FieldClassMiddlewareFactory.Create<TMiddleware>();
                        return new MapMiddleware(
                            n, fieldReference, classMiddleware(n));
                    }));
        }

        public static IRequestExecutorBuilder MapField<TMiddleware>(
            this IRequestExecutorBuilder builder,
            FieldReference fieldReference,
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            return builder.UseField(
                FieldClassMiddlewareFactory.Create(
                    (s, n) =>
                    {
                        FieldMiddleware classMiddleware =
                            FieldClassMiddlewareFactory
                                .Create(factory);
                        return new MapMiddleware(
                            n, fieldReference, classMiddleware(n));
                    }));
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

        private sealed class MapMiddleware
        {
            private readonly FieldDelegate _next;
            private readonly FieldReference _fieldReference;
            private readonly FieldDelegate _fieldDelegate;

            public MapMiddleware(
                FieldDelegate next,
                FieldReference fieldReference,
                FieldDelegate fieldDelegate)
            {
                _next = next
                    ?? throw new ArgumentNullException(nameof(next));
                _fieldReference = fieldReference
                    ?? throw new ArgumentNullException(nameof(fieldReference));
                _fieldDelegate = fieldDelegate
                    ?? throw new ArgumentNullException(nameof(fieldDelegate));
            }

            public ValueTask InvokeAsync(IMiddlewareContext context)
            {
                return IsField(context.ObjectType.Name, context.Field.Name)
                    ? _fieldDelegate(context)
                    : _next(context);
            }

            private bool IsField(NameString typeName, NameString fieldName)
            {
                return _fieldReference.TypeName.Equals(typeName)
                    && _fieldReference.FieldName.Equals(fieldName);
            }
        }
    }
}