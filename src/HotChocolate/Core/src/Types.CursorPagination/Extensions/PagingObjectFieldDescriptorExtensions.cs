using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
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
            Type? type = null,
            Type? entityType = null,
            GetCursorPagingProvider? resolvePagingProvider = null,
            PagingSettings settings = default)
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
                settings);

            descriptor
                .Extend()
                .OnBeforeCreate(
                    (c, d) => d.Type = CreateConnectionTypeRef(
                        c, d.ResolverMember ?? d.Member, type, settings));

            return descriptor;
        }

        public static IInterfaceFieldDescriptor UsePaging<TSchemaType>(
            this IInterfaceFieldDescriptor descriptor,
            PagingSettings settings = default)
            where TSchemaType : class, IOutputType =>
            UsePaging(descriptor, typeof(TSchemaType), settings);

        public static IInterfaceFieldDescriptor UsePaging(
            this IInterfaceFieldDescriptor descriptor,
            Type? type,
            PagingSettings settings = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            descriptor
                .AddPagingArguments()
                .Extend()
                .OnBeforeCreate(
                    (c, d) => d.Type = CreateConnectionTypeRef(c, d.Member, type, settings));

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
                throw PagingObjectFieldDescriptorExtensions_InvalidType();
            }

            settings = context.GetSettings(settings);

            // once we have identified the correct type we will create the
            // paging result type from it.
            IExtendedType connectionType = context.TypeInspector.GetType(
                settings.IncludeTotalCount ?? false
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
