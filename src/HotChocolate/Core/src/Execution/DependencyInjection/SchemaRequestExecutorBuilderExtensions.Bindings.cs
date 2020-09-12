using System;
using HotChocolate;
using HotChocolate.Configuration.Bindings;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder BindResolver<TResolver>(
            this IRequestExecutorBuilder builder)
            where TResolver : class =>
            builder.ConfigureSchema(
                b => b.BindResolver<TResolver>(BindingBehavior.Implicit, null));

        public static IRequestExecutorBuilder BindResolver<TResolver>(
            this IRequestExecutorBuilder builder,
            Action<IBindResolver<TResolver>> configure)
            where TResolver : class =>
            builder.ConfigureSchema(
                b => b.BindResolver<TResolver>(BindingBehavior.Implicit, configure));

        public static IRequestExecutorBuilder BindResolver<TResolver>(
            this IRequestExecutorBuilder builder,
            BindingBehavior bindingBehavior,
            Action<IBindResolver<TResolver>> configure)
            where TResolver : class =>
            builder.ConfigureSchema(
                b => b.BindResolver<TResolver>(bindingBehavior, configure));

        public static IRequestExecutorBuilder BindComplexType<T>(
            this IRequestExecutorBuilder builder)
            where T : class =>
            builder.ConfigureSchema(b => b.BindComplexType<T>());

        public static IRequestExecutorBuilder BindComplexType<T>(
            this IRequestExecutorBuilder builder,
            Action<IBindType<T>> configure)
            where T : class =>
            builder.ConfigureSchema(b => b.BindComplexType(configure));

        public static IRequestExecutorBuilder BindComplexType<T>(
            this IRequestExecutorBuilder builder,
            BindingBehavior bindingBehavior,
            Action<IBindType<T>> configure)
            where T : class =>
            builder.ConfigureSchema(b => b.BindComplexType(bindingBehavior, configure));
    }
}