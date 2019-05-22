using System;
using System.Linq.Expressions;
using HotChocolate.Resolvers;

namespace HotChocolate.Types
{
    public static class DirectiveTypeDescriptorExtensions
    {
        public static IDirectiveTypeDescriptor<T> Ignore<T>(
            this IDirectiveTypeDescriptor<T> descriptor,
            Expression<Func<T, object>> property)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            descriptor.Argument(property).Ignore();
            return descriptor;
        }

        public static IDirectiveTypeDescriptor Use<TMiddleware>(
            this IDirectiveTypeDescriptor descriptor)
            where TMiddleware : class
        {
            return descriptor.Use(
                DirectiveClassMiddlewareFactory.Create<TMiddleware>());
        }

        public static IDirectiveTypeDescriptor Use<TMiddleware>(
            this IDirectiveTypeDescriptor descriptor,
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return descriptor.Use(
                DirectiveClassMiddlewareFactory.Create(factory));
        }

        public static IDirectiveTypeDescriptor<T> Use<T, TMiddleware>(
           this IDirectiveTypeDescriptor<T> descriptor)
           where TMiddleware : class
        {
            return descriptor.Use(
                DirectiveClassMiddlewareFactory.Create<TMiddleware>());
        }

        public static IDirectiveTypeDescriptor<T> Use<T, TMiddleware>(
            this IDirectiveTypeDescriptor<T> descriptor,
            Func<IServiceProvider, FieldDelegate, TMiddleware> factory)
            where TMiddleware : class
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return descriptor.Use(
                DirectiveClassMiddlewareFactory.Create(factory));
        }
    }
}
