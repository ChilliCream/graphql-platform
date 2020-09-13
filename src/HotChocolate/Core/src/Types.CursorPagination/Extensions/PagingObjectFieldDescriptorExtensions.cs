using System;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Types.Properties.CursorResources;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types
{
    public static class PagingObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UsePaging<TSchemaType, TEntity>(
            this IObjectFieldDescriptor descriptor,
            GetCursorPagingProvider? resolvePagingProvider = null,
            PagingSettings settings = default)
            where TSchemaType : class, IOutputType =>
            UsePaging<TSchemaType>(descriptor, typeof(TEntity), resolvePagingProvider, settings);

        public static IObjectFieldDescriptor UsePaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor,
            Type? entityType = null,
            GetCursorPagingProvider? resolvePagingProvider = null,
            PagingSettings settings = default)
            where TSchemaType : class, IOutputType =>
            UsePaging(descriptor, typeof(TSchemaType), entityType, resolvePagingProvider, settings);

        public static IObjectFieldDescriptor UsePaging(
            this IObjectFieldDescriptor descriptor,
            Type type,
            Type? entityType = null,
            GetCursorPagingProvider? resolvePagingProvider = null,
            PagingSettings settings = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (!typeof(IOutputType).IsAssignableFrom(type) || !type.IsClass)
            {
                throw new ArgumentException(
                    PagingObjectFieldDescriptorExtensions_SchemaTypeNotValid,
                    nameof(descriptor));
            }

            resolvePagingProvider ??= ResolvePagingProvider;

            descriptor.AddPagingArguments();

            PagingHelper.UsePaging(
                descriptor,
                type,
                entityType,
                (services, source) => resolvePagingProvider(services, source),
                settings);

            descriptor
                .Extend()
                .OnBeforeCreate(
                    (c, d) => d.Type = CreateConnectionTypeRef(c, type, settings));

            return descriptor;
        }

        public static IInterfaceFieldDescriptor UsePaging<TSchemaType>(
            this IInterfaceFieldDescriptor descriptor,
            PagingSettings settings = default)
            where TSchemaType : class, IOutputType =>
            UsePaging(descriptor, typeof(TSchemaType), settings);

        public static IInterfaceFieldDescriptor UsePaging(
            this IInterfaceFieldDescriptor descriptor,
            Type type,
            PagingSettings settings = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (type is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (!typeof(IOutputType).IsAssignableFrom(type) || !type.IsClass)
            {
                throw new ArgumentException(
                    PagingObjectFieldDescriptorExtensions_SchemaTypeNotValid,
                    nameof(descriptor));
            }

            descriptor
                .AddPagingArguments()
                .Extend()
                .OnBeforeCreate(
                    (c, d) => d.Type = CreateConnectionTypeRef(c, type, settings));

            return descriptor;
        }

        public static IObjectFieldDescriptor AddPagingArguments(
            this IObjectFieldDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor
                .Argument(CursorPagingArgumentNames.First, a => a.Type<IntType>())
                .Argument(CursorPagingArgumentNames.After, a => a.Type<StringType>())
                .Argument(CursorPagingArgumentNames.Last, a => a.Type<IntType>())
                .Argument(CursorPagingArgumentNames.Before, a => a.Type<StringType>());
        }

        public static IInterfaceFieldDescriptor AddPagingArguments(
            this IInterfaceFieldDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor
                .Argument(CursorPagingArgumentNames.First, a => a.Type<IntType>())
                .Argument(CursorPagingArgumentNames.After, a => a.Type<StringType>())
                .Argument(CursorPagingArgumentNames.Last, a => a.Type<IntType>())
                .Argument(CursorPagingArgumentNames.Before, a => a.Type<StringType>());
        }

        private static ITypeReference CreateConnectionTypeRef(
            IDescriptorContext context,
            Type schemaType,
            PagingSettings settings)
        {
            settings = context.GetSettings(settings);

            Type connectionType = settings.IncludeTotalCount ?? false
                ? typeof(ConnectionCountType<>).MakeGenericType(schemaType)
                : typeof(ConnectionType<>).MakeGenericType(schemaType);
            IExtendedType extendedType = context.TypeInspector.GetType (connectionType);

            if (!extendedType.IsSchemaType ||
                !context.TypeInspector.TryCreateTypeInfo(extendedType, out ITypeInfo typeInfo) ||
                !typeInfo.IsOutputType())
            {
                throw PagingObjectFieldDescriptorExtensions_InvalidType();
            }

            return TypeReference.Create(extendedType, TypeContext.Output);
        }

        private static CursorPagingProvider ResolvePagingProvider(
            IServiceProvider services,
            IExtendedType source) =>
            services.GetServices<CursorPagingProvider>().First(p => p.CanHandle(source));
    }
}
