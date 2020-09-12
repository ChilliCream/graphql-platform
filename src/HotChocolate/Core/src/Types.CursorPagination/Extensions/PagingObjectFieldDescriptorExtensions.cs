using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using static HotChocolate.Types.Properties.CursorResources;
using static HotChocolate.Utilities.ThrowHelper;

namespace HotChocolate.Types
{
    public static class PagingObjectFieldDescriptorExtensions
    {
        private static readonly Type _middleware = typeof(ConnectionMiddleware<,>);

        public static IObjectFieldDescriptor UsePaging<TSchemaType, TEntity>(
            this IObjectFieldDescriptor descriptor,
            ConnectionSettings settings = default)
            where TSchemaType : class, IOutputType =>
            UsePaging(descriptor, typeof(TSchemaType), typeof(TEntity), settings);

        public static IObjectFieldDescriptor UsePaging<TSchemaType>(
            this IObjectFieldDescriptor descriptor,
            ConnectionSettings settings = default)
            where TSchemaType : class, IOutputType =>
            UsePaging(descriptor, typeof(TSchemaType), settings: settings);

        public static IObjectFieldDescriptor UsePaging(
            this IObjectFieldDescriptor descriptor,
            Type schemaType,
            Type? entityType = null,
            ConnectionSettings settings = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (schemaType is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (!typeof(IOutputType).IsAssignableFrom(schemaType) || !schemaType.IsClass)
            {
                throw new ArgumentException(
                    PagingObjectFieldDescriptorExtensions_SchemaTypeNotValid,
                    nameof(descriptor));
            }

            FieldMiddleware placeholder = next => context => default;

            descriptor
                .AddPagingArguments()
                .Use(placeholder);

            descriptor
                .Extend()
                .OnBeforeCreate(
                    (c, d) => d.Type = CreateConnectionTypeRef(c, schemaType, settings));

            descriptor
                .Extend()
                .OnBeforeCompletion((c, d) =>
                {
                    settings = c.GetSettings(settings);

                    Type connectionType = settings.WithTotalCount ?? false
                        ? typeof(ConnectionCountType<>).MakeGenericType(schemaType)
                        : typeof(ConnectionType<>).MakeGenericType(schemaType);
                    ITypeReference typeRef = c.TypeInspector.GetOutputTypeRef(connectionType);

                    if (entityType is null)
                    {
                        IOutputType type = c.GetType<IOutputType>(typeRef);
                        entityType = type.ToRuntimeType();
                    }

                    MemberInfo member = d.ResolverMember ?? d.Member;
                    Type resultType = d.Resolver is not null && d.ResultType is not null
                        ? d.ResultType
                        : c.TypeInspector.GetReturnType(member, true).Source;
                    resultType = UnwrapType(resultType);

                    FieldMiddleware middleware = CreateMiddleware(resultType, entityType, settings);
                    var index = d.MiddlewareComponents.IndexOf(placeholder);
                    d.MiddlewareComponents[index] = middleware;
                })
                .DependsOn(schemaType);

            return descriptor;
        }

        public static IInterfaceFieldDescriptor UsePaging<TSchemaType>(
            this IInterfaceFieldDescriptor descriptor,
            ConnectionSettings settings = default)
            where TSchemaType : class, IOutputType =>
            UsePaging(descriptor, typeof(TSchemaType), settings);

        public static IInterfaceFieldDescriptor UsePaging(
            this IInterfaceFieldDescriptor descriptor,
            Type schemaType,
            ConnectionSettings settings = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (schemaType is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (!typeof(IOutputType).IsAssignableFrom(schemaType) || !schemaType.IsClass)
            {
                throw new ArgumentException(
                    PagingObjectFieldDescriptorExtensions_SchemaTypeNotValid,
                    nameof(descriptor));
            }

            descriptor
                .AddPagingArguments()
                .Extend()
                .OnBeforeCreate(
                    (c, d) => d.Type = CreateConnectionTypeRef(c, schemaType, settings));

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
                .Argument(PaginationArguments.First, a => a.Type<IntType>())
                .Argument(PaginationArguments.After, a => a.Type<StringType>())
                .Argument(PaginationArguments.Last, a => a.Type<IntType>())
                .Argument(PaginationArguments.Before, a => a.Type<StringType>());
        }

        public static IInterfaceFieldDescriptor AddPagingArguments(
            this IInterfaceFieldDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return descriptor
                .Argument(PaginationArguments.First, a => a.Type<IntType>())
                .Argument(PaginationArguments.After, a => a.Type<StringType>())
                .Argument(PaginationArguments.Last, a => a.Type<IntType>())
                .Argument(PaginationArguments.Before, a => a.Type<StringType>());
        }

        private static FieldMiddleware CreateMiddleware(
            Type sourceType,
            Type entityType,
            ConnectionSettings settings)
        {
            Type middlewareType = _middleware.MakeGenericType(sourceType, entityType);
            return FieldClassMiddlewareFactory.Create(middlewareType, settings);
        }

        internal static Type UnwrapType(Type resultType)
        {
            if (resultType == null)
            {
                throw new ArgumentNullException(nameof(resultType));
            }

            if (resultType.IsGenericType &&
                resultType.GetGenericTypeDefinition() == typeof(IConnectionResolver<>))
            {
                return resultType.GetGenericArguments()[0];
            }

            if (typeof(IConnectionResolver).IsAssignableFrom(resultType))
            {
                Type[] interfaces = resultType.GetInterfaces();
                for (var i = 0; i < interfaces.Length; i++)
                {
                    Type type = interfaces[i];
                    if (type.IsGenericType &&
                        type.GetGenericTypeDefinition() == typeof(IConnectionResolver<>))
                    {
                        return type.GetGenericArguments()[0];
                    }
                }

                return typeof(object);
            }

            return resultType;
        }

        private static ITypeReference CreateConnectionTypeRef(
            IDescriptorContext context,
            Type schemaType,
            ConnectionSettings settings)
        {
            settings = context.GetSettings(settings);

            Type connectionType = settings.WithTotalCount ?? false
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

        private static ConnectionSettings GetSettings(
            this ITypeCompletionContext context,
            ConnectionSettings settings) =>
            context.DescriptorContext.GetSettings(settings);

        private static ConnectionSettings GetSettings(
            this IDescriptorContext context,
            ConnectionSettings settings)
        {
            ConnectionSettings global = default;
            if (context.ContextData.TryGetValue(ConnectionSettings.GetKey(), out object? o) &&
                o is ConnectionSettings casted)
            {
                global = casted;
            }

            return new ConnectionSettings
            {
                DefaultPageSize = settings.DefaultPageSize ?? global.DefaultPageSize,
                MaxPageSize = settings.MaxPageSize ?? global.MaxPageSize,
                WithTotalCount = settings.WithTotalCount ?? global.WithTotalCount,
            };
        }
    }
}
