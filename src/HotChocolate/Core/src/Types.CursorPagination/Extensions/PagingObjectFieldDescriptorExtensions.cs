using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using static HHotChocolate.Types.Pagination.Utilities.ThrowHelper;

namespace HotChocolate.Types
{
    public static class PagingObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UsePaging<TSchemaType, TEntity>(
            this IObjectFieldDescriptor descriptor,
            GetCursorPagingProvider? resolvePagingProvider = null,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UsePaging<TSchemaType>(descriptor, typeof(TEntity), resolvePagingProvider, options);

        public static IObjectFieldDescriptor UsePaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor,
            Type? entityType = null,
            GetCursorPagingProvider? resolvePagingProvider = null,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UsePaging(descriptor, typeof(TSchemaType), entityType, resolvePagingProvider, options);

        public static IObjectFieldDescriptor UsePaging(
            this IObjectFieldDescriptor descriptor,
            Type? type = null,
            Type? entityType = null,
            GetCursorPagingProvider? resolvePagingProvider = null,
            PagingOptions options = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            resolvePagingProvider ??= ResolvePagingProvider;

            descriptor.AddPagingArguments();

            PagingHelper.UsePaging(
                descriptor,
                type,
                entityType,
                (services, source) => resolvePagingProvider(services, source),
                options);

            descriptor
                .Extend()
                .OnBeforeCreate(
                    (c, d) => d.Type = CreateConnectionTypeRef(
                        c, d.ResolverMember ?? d.Member, type, options));

            return descriptor;
        }

        public static IInterfaceFieldDescriptor UsePaging<TSchemaType>(
            this IInterfaceFieldDescriptor descriptor,
            PagingOptions options = default)
            where TSchemaType : class, IOutputType =>
            UsePaging(descriptor, typeof(TSchemaType), options);

        public static IInterfaceFieldDescriptor UsePaging(
            this IInterfaceFieldDescriptor descriptor,
            Type? type = null,
            PagingOptions options = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor
                .AddPagingArguments()
                .Extend()
                .OnBeforeCreate(
                    (c, d) => d.Type = CreateConnectionTypeRef(c, d.Member, type, options));

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
            MemberInfo? resolverMember,
            Type? type,
            PagingOptions options)
        {
            // first we will try and infer the schema type from the collection.
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
                throw PagingObjectFieldDescriptorExtensions_InvalidType();
            }

            options = context.GetSettings(options);

            // once we have identified the correct type we will create the
            // paging result type from it.
            IExtendedType connectionType = context.TypeInspector.GetType(
                options.IncludeTotalCount ?? false
                    ? typeof(ConnectionCountType<>).MakeGenericType(schemaType.Source)
                    : typeof(ConnectionType<>).MakeGenericType(schemaType.Source));

            // last but not leas we create a type reference that can be put on the field definition
            // to tell the type discovery that this field needs this result type.
            return TypeReference.Create(connectionType, TypeContext.Output);
        }

        private static CursorPagingProvider ResolvePagingProvider(
            IServiceProvider services,
            IExtendedType source)
        {
            try
            {
                if (services.GetService<IEnumerable<CursorPagingProvider>>() is { } providers &&
                    providers.FirstOrDefault(p => p.CanHandle(source)) is { } provider)
                {
                    return provider;
                }
            }
            catch (InvalidOperationException)
            {
                // some containers will except if a service does not exist.
                // in this case we will ignore the exception and return the default provider.
            }

            return new QueryableCursorPagingProvider();
        }
    }
}
