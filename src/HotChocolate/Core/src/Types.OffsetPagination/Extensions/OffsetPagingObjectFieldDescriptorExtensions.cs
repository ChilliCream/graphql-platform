using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using static HotChocolate.Utilities.ThrowHelper;
using static HotChocolate.Types.Pagination.Properties.OffsetResources;

namespace HotChocolate.Types
{
    public static class OffsetPagingObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UseOffsetPaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor,
            Type? entityType = null,
            GetOffsetPagingProvider? resolvePagingProvider = null,
            PagingSettings settings = default)
            where TSchemaType : IOutputType =>
            UseOffsetPaging(
                descriptor, 
                typeof(TSchemaType), 
                entityType, 
                resolvePagingProvider, 
                settings);

        public static IObjectFieldDescriptor UseOffsetPaging(
            this IObjectFieldDescriptor descriptor,
            Type? type = null,
            Type? entityType = null,
            GetOffsetPagingProvider? resolvePagingProvider = null,
            PagingSettings settings = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
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
                .OnBeforeCreate((c, d) => 
                {
                    d.Type = CreateTypeRef(c, d.ResolverMember ?? d.Member, type, settings);
                });

            return descriptor;
        }

        public static IInterfaceFieldDescriptor UseOffsetPaging<TSchemaType>(
            this IInterfaceFieldDescriptor descriptor,
            PagingSettings settings = default)
            where TSchemaType : class, IOutputType =>
            UseOffsetPaging(descriptor, typeof(TSchemaType), settings);

        public static IInterfaceFieldDescriptor UseOffsetPaging(
            this IInterfaceFieldDescriptor descriptor,
            Type? type,
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
                .OnBeforeCreate((c, d) => d.Type = CreateTypeRef(c, d.Member, type, settings));

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
            MemberInfo? resolverMember,
            Type? type,
            PagingSettings settings)
        {
            // first we will try and infer the schema type of the collection.
            IExtendedType schemaType = PagingHelper.GetSchemaType(
                context.TypeInspector,
                resolverMember,
                type);

            // we need to ensure that the schema type is a valid output type. For this we create a
            // type info which decomposes the type into its logical type components and is able 
            // to check if the named type component is really an output type.
            if (!context.TypeInspector.TryCreateTypeInfo(schemaType, out ITypeInfo? typeInfo) ||
                !typeInfo.IsOutputType())
            {
                throw OffsetPagingObjectFieldDescriptorExtensions_InvalidType();
            }

            settings = context.GetSettings(settings);

            // once we have identified the correct type we will create the 
            // paging result type from it.
            IExtendedType connectionType = context.TypeInspector.GetType(
                settings.IncludeTotalCount ?? false
                    ? typeof(CollectionSegmentCountType<>).MakeGenericType(schemaType.Source)
                    : typeof(CollectionSegmentType<>).MakeGenericType(schemaType.Source));

            // last but not leas we create a type reference that can be put on the field definition
            // to tell the type discovery that this field needs this result type.
            return TypeReference.Create(connectionType, TypeContext.Output);
        }

        private static OffsetPagingProvider ResolvePagingProvider(
            IServiceProvider services,
            IExtendedType source) =>
            services.GetServices<OffsetPagingProvider>().FirstOrDefault(p => p.CanHandle(source)) ??
                new QueryableOffsetPagingProvider();
    }
}
