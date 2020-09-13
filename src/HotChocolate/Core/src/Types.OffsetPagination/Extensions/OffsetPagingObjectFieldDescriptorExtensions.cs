using System;
using System.Linq;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Utilities.ThrowHelper;
using static HotChocolate.Types.Pagination.Properties.OffsetResources;

namespace HotChocolate.Types
{
    public static class OffsetPagingObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UseOffsetPaging(
            this IObjectFieldDescriptor descriptor,
            Type type,
            Type? entityType = null,
            GetOffsetPagingProvider? resolvePagingProvider = null,
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
                    OffsetPagingObjectFieldDescriptorExtensions_SchemaTypeNotValid,
                    nameof(descriptor));
            }

            resolvePagingProvider ??= ResolvePagingProvider;

            descriptor.AddOffsetPagingArguments();

            PagingHelper.UsePaging(
                descriptor,
                type,
                entityType,
                (services, source) => resolvePagingProvider(services, source),
                settings);

            descriptor
                .Extend()
                .OnBeforeCreate((c, d) => d.Type = CreateTypeRef(c, type, settings));

            return descriptor;
        }

        public static IInterfaceFieldDescriptor UseOffsetPaging<TSchemaType>(
            this IInterfaceFieldDescriptor descriptor,
            PagingSettings settings = default)
            where TSchemaType : class, IOutputType =>
            UseOffsetPaging(descriptor, typeof(TSchemaType), settings);

        public static IInterfaceFieldDescriptor UseOffsetPaging(
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
                    OffsetPagingObjectFieldDescriptorExtensions_SchemaTypeNotValid,
                    nameof(descriptor));
            }

            descriptor
                .AddOffsetPagingArguments()
                .Extend()
                .OnBeforeCreate((c, d) => d.Type = CreateTypeRef(c, type, settings));

            return descriptor;
        }

        public static IObjectFieldDescriptor AddOffsetPagingArguments(
            this IObjectFieldDescriptor descriptor)
        {
            return descriptor
                .Argument(OffsetPagingArgumentNames.Skip, a => a.Type<IntType>())
                .Argument(OffsetPagingArgumentNames.Take, a => a.Type<IntType>());
        }

        public static IInterfaceFieldDescriptor AddOffsetPagingArguments(
            this IInterfaceFieldDescriptor descriptor)
        {
            return descriptor
                .Argument(OffsetPagingArgumentNames.Skip, a => a.Type<IntType>())
                .Argument(OffsetPagingArgumentNames.Take, a => a.Type<IntType>());
        }

        private static ITypeReference CreateTypeRef(
            IDescriptorContext context,
            Type schemaType,
            PagingSettings settings)
        {
            settings = context.GetSettings(settings);

            Type connectionType = settings.IncludeTotalCount ?? false
                ? typeof(CollectionSegmentCountType<>).MakeGenericType(schemaType)
                : typeof(CollectionSegmentType<>).MakeGenericType(schemaType);
            IExtendedType extendedType = context.TypeInspector.GetType (connectionType);

            if (!extendedType.IsSchemaType ||
                !context.TypeInspector.TryCreateTypeInfo(extendedType, out ITypeInfo? typeInfo) ||
                !typeInfo.IsOutputType())
            {
                throw OffsetPagingObjectFieldDescriptorExtensions_InvalidType();
            }

            return TypeReference.Create(extendedType, TypeContext.Output);
        }

        private static OffsetPagingProvider ResolvePagingProvider(
            IServiceProvider services,
            IExtendedType source) =>
            services.GetServices<OffsetPagingProvider>().FirstOrDefault(p => p.CanHandle(source)) ??
                new QueryableOffsetPagingProvider();
    }
}
