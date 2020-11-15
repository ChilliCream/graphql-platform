using System;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder TryAddTypeInterceptor(
            this IRequestExecutorBuilder builder,
            ITypeInitializationInterceptor typeInterceptor)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (typeInterceptor is null)
            {
                throw new ArgumentNullException(nameof(typeInterceptor));
            }

            return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeInterceptor));
        }

        public static IRequestExecutorBuilder TryAddTypeInterceptor(
            this IRequestExecutorBuilder builder,
            Type typeInterceptor)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (typeInterceptor is null)
            {
                throw new ArgumentNullException(nameof(typeInterceptor));
            }

            return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeInterceptor));
        }

        public static IRequestExecutorBuilder TryAddTypeInterceptor<T>(
            this IRequestExecutorBuilder builder)
            where T : ITypeInitializationInterceptor
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.TryAddTypeInterceptor(typeof(T)));
        }

        public static IRequestExecutorBuilder TryAddSchemaInterceptor(
            this IRequestExecutorBuilder builder,
            ISchemaInterceptor typeInterceptor)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (typeInterceptor is null)
            {
                throw new ArgumentNullException(nameof(typeInterceptor));
            }

            return builder.ConfigureSchema(b => b.TryAddSchemaInterceptor(typeInterceptor));
        }

        public static IRequestExecutorBuilder TryAddSchemaInterceptor(
            this IRequestExecutorBuilder builder,
            Type typeInterceptor)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (typeInterceptor is null)
            {
                throw new ArgumentNullException(nameof(typeInterceptor));
            }

            return builder.ConfigureSchema(b => b.TryAddSchemaInterceptor(typeInterceptor));
        }

        public static IRequestExecutorBuilder TryAddSchemaInterceptor<T>(
            this IRequestExecutorBuilder builder)
            where T : ISchemaInterceptor
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.ConfigureSchema(b => b.TryAddSchemaInterceptor<T>());
        }

        public static IRequestExecutorBuilder OnBeforeRegisterDependencies(
            this IRequestExecutorBuilder builder,
            OnInitializeType onBeforeRegisterDependencies,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onBeforeRegisterDependencies is null)
            {
                throw new ArgumentNullException(nameof(onBeforeRegisterDependencies));
            }

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInterceptor(
                    canHandle,
                    onBeforeRegisterDependencies: onBeforeRegisterDependencies)));
        }

        public static IRequestExecutorBuilder OnBeforeRegisterDependencies<T>(
            this IRequestExecutorBuilder builder,
            OnInitializeType<T> onBeforeRegisterDependencies,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
            where T : DefinitionBase
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onBeforeRegisterDependencies is null)
            {
                throw new ArgumentNullException(nameof(onBeforeRegisterDependencies));
            }

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInitializationInterceptor<T>(
                    canHandle,
                    onBeforeRegisterDependencies: onBeforeRegisterDependencies)));
        }

        public static IRequestExecutorBuilder OnAfterRegisterDependencies(
            this IRequestExecutorBuilder builder,
            OnInitializeType onAfterRegisterDependencies,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onAfterRegisterDependencies is null)
            {
                throw new ArgumentNullException(nameof(onAfterRegisterDependencies));
            }

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInterceptor(
                    canHandle,
                    onAfterRegisterDependencies: onAfterRegisterDependencies)));
        }

        public static IRequestExecutorBuilder OnAfterRegisterDependencies<T>(
            this IRequestExecutorBuilder builder,
            OnInitializeType<T> onAfterRegisterDependencies,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
            where T : DefinitionBase
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onAfterRegisterDependencies is null)
            {
                throw new ArgumentNullException(nameof(onAfterRegisterDependencies));
            }

            canHandle ??= ctx => true;

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInitializationInterceptor<T>(
                    canHandle,
                    onAfterRegisterDependencies: onAfterRegisterDependencies)));
        }

        public static IRequestExecutorBuilder OnBeforeCompleteName(
            this IRequestExecutorBuilder builder,
            OnCompleteType onBeforeCompleteName,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onBeforeCompleteName is null)
            {
                throw new ArgumentNullException(nameof(onBeforeCompleteName));
            }

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInterceptor(
                    canHandle,
                    onBeforeCompleteName: onBeforeCompleteName)));
        }

        public static IRequestExecutorBuilder OnBeforeCompleteName<T>(
            this IRequestExecutorBuilder builder,
            OnCompleteType<T> onBeforeCompleteName,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
            where T : DefinitionBase
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onBeforeCompleteName is null)
            {
                throw new ArgumentNullException(nameof(onBeforeCompleteName));
            }

            canHandle ??= ctx => true;

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInitializationInterceptor<T>(
                    canHandle,
                    onBeforeCompleteName: onBeforeCompleteName)));
        }

        public static IRequestExecutorBuilder OnAfterCompleteName(
            this IRequestExecutorBuilder builder,
            OnCompleteType onAfterCompleteName,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onAfterCompleteName is null)
            {
                throw new ArgumentNullException(nameof(onAfterCompleteName));
            }

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInterceptor(
                    canHandle,
                    onAfterCompleteName: onAfterCompleteName)));
        }

        public static IRequestExecutorBuilder OnAfterCompleteName<T>(
            this IRequestExecutorBuilder builder,
            OnCompleteType<T> onAfterCompleteName,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
            where T : DefinitionBase
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onAfterCompleteName is null)
            {
                throw new ArgumentNullException(nameof(onAfterCompleteName));
            }

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInitializationInterceptor<T>(
                    canHandle,
                    onAfterCompleteName: onAfterCompleteName)));
        }

        public static IRequestExecutorBuilder OnBeforeCompleteType(
            this IRequestExecutorBuilder builder,
            OnCompleteType onBeforeCompleteType,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onBeforeCompleteType is null)
            {
                throw new ArgumentNullException(nameof(onBeforeCompleteType));
            }

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInterceptor(
                    canHandle,
                    onBeforeCompleteType: onBeforeCompleteType)));
        }

        public static IRequestExecutorBuilder OnBeforeCompleteType<T>(
            this IRequestExecutorBuilder builder,
            OnCompleteType<T> onBeforeCompleteType,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
            where T : DefinitionBase
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onBeforeCompleteType is null)
            {
                throw new ArgumentNullException(nameof(onBeforeCompleteType));
            }

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInitializationInterceptor<T>(
                    canHandle,
                    onBeforeCompleteType: onBeforeCompleteType)));
        }

        public static IRequestExecutorBuilder OnAfterCompleteType(
            this IRequestExecutorBuilder builder,
            OnCompleteType onAfterCompleteType,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onAfterCompleteType is null)
            {
                throw new ArgumentNullException(nameof(onAfterCompleteType));
            }

            return builder.ConfigureSchema(b => b
                .TryAddTypeInterceptor(new DelegateTypeInterceptor(
                    canHandle,
                    onAfterCompleteType: onAfterCompleteType)));
        }

        public static IRequestExecutorBuilder OnAfterCompleteType<T>(
            this IRequestExecutorBuilder builder,
            OnCompleteType<T> onAfterCompleteType,
            Func<ITypeSystemObjectContext, bool>? canHandle = null)
            where T : DefinitionBase
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onAfterCompleteType is null)
            {
                throw new ArgumentNullException(nameof(onAfterCompleteType));
            }

            return builder.ConfigureSchema(
                b => b.TryAddTypeInterceptor(
                    new DelegateTypeInitializationInterceptor<T>(
                        canHandle,
                        onAfterCompleteType: onAfterCompleteType)));
        }

        public static IRequestExecutorBuilder OnSchemaError(
            this IRequestExecutorBuilder builder,
            Action<IDescriptorContext, Exception> onError)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (onError is null)
            {
                throw new ArgumentNullException(nameof(onError));
            }

            return builder.ConfigureSchema(
                b => b.TryAddSchemaInterceptor(
                    new DelegateSchemaInterceptor(onError: onError)));
        }
    }
}
