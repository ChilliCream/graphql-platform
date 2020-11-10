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
            this IRequestExecutorBuilder builder,
            Action<IBindResolver<TResolver>>? configure = default,
            BindingBehavior bindingBehavior = BindingBehavior.Implicit)
            where TResolver : class =>
            builder.ConfigureSchema(
                b => b.BindResolver<TResolver>(bindingBehavior, configure));

        public static IRequestExecutorBuilder BindComplexType<T>(
            this IRequestExecutorBuilder builder,
            Action<IBindType<T>>? configure = default,
            BindingBehavior bindingBehavior = BindingBehavior.Implicit)
            where T : class =>
            builder.ConfigureSchema(b => b.BindComplexType(bindingBehavior, configure));

        public static IRequestExecutorBuilder BindEnumType<T>(
            this IRequestExecutorBuilder builder,
            Action<IEnumTypeBindingDescriptor>? configure = default) =>
            builder.ConfigureSchema(b => b.BindEnumType<T>(configure));

        public static IRequestExecutorBuilder BindEnumType(
            this IRequestExecutorBuilder builder,
            Type runtimeType,
            Action<IEnumTypeBindingDescriptor>? configure = default) =>
            builder.ConfigureSchema(b => b.BindEnumType(runtimeType, configure));
    }
}