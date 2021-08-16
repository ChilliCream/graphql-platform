using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;
using static HotChocolate.Utilities.ThrowHelper;
using static HotChocolate.Types.Pagination.PagingDefaults;
using static HotChocolate.Types.Pagination.CursorPagingArgumentNames;
using static HotChocolate.Types.Properties.CursorResources;

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
            Type? schemaType = null,
            Type? entityType = null,
            GetCursorPagingProvider? resolvePagingProvider = null,
            PagingOptions options = default)
            => UsePagingInternal(descriptor, null, schemaType, entityType, resolvePagingProvider, options);

        private static IObjectFieldDescriptor UsePagingInternal(
            this IObjectFieldDescriptor descriptor,
            NameString? connectionName = null,
            object? nodeType = null,
            Type? entityType = null,
            GetCursorPagingProvider? resolvePagingProvider = null,
            PagingOptions options = default)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            resolvePagingProvider ??= ResolvePagingProvider;

            PagingHelper.UsePaging(
                descriptor,
                entityType,
                (services, source) => resolvePagingProvider(services, source),
                options);

            descriptor
                .Extend()
                .OnBeforeCreate((c, d) =>
                {
                    var pagingOptions = c.GetSettings(options);
                    var backward = pagingOptions.AllowBackwardPagination ?? AllowBackwardPagination;

                    var field = ObjectFieldDescriptor.From(c, d);
                    field.AddPagingArguments(backward);
                    field.CreateDefinition();

                    if (connectionName is null or { IsEmpty: true })
                    {
                        connectionName =
                            pagingOptions.InferConnectionNameFromField ??
                                InferConnectionNameFromField
                                    ? EnsureConnectionNameCasing(d.Name)
                                    : throw new NotImplementedException("We still need something on the type system for this.");
                    }

                    ITypeReference? typeRef = nodeType switch
                    {
                        Type type => c.TypeInspector.GetTypeRef(type),
                        ITypeReference typeReference => typeReference,
                        _ => null
                    };

                    MemberInfo? resolverMember = d.ResolverMember ?? d.Member;
                    d.Type = CreateConnectionTypeRef(c, resolverMember, connectionName.Value, typeRef, options);
                    d.CustomSettings.Add(typeof(Connection));
                });

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
                .Extend()
                .OnBeforeCreate((c, d) =>
                {
                    if (!(c.ContextData.TryGetValue(typeof(PagingOptions).FullName!, out var obj) &&
                       obj is PagingOptions pagingOptions))
                    {
                        pagingOptions = default;
                    }

                    var backward = pagingOptions.AllowBackwardPagination ?? AllowBackwardPagination;

                    var field = InterfaceFieldDescriptor.From(c, d);
                    field.AddPagingArguments(backward);
                    field.CreateDefinition();
                });


            return descriptor;
        }

        public static IObjectFieldDescriptor AddPagingArguments(
            this IObjectFieldDescriptor descriptor)
            => AddPagingArguments(descriptor, true);

        public static IObjectFieldDescriptor AddPagingArguments(
            this IObjectFieldDescriptor descriptor,
            bool allowBackwardPagination)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            CreatePagingArguments(
                descriptor.Extend().Definition.Arguments,
                allowBackwardPagination);

            return descriptor;
        }

        public static IInterfaceFieldDescriptor AddPagingArguments(
            this IInterfaceFieldDescriptor descriptor)
            => AddPagingArguments(descriptor, true);

        public static IInterfaceFieldDescriptor AddPagingArguments(
            this IInterfaceFieldDescriptor descriptor,
            bool allowBackwardPagination)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            CreatePagingArguments(
                descriptor.Extend().Definition.Arguments,
                allowBackwardPagination);

            return descriptor;
        }

        private static void CreatePagingArguments(
            IList<ArgumentDefinition> arguments,
            bool allowBackwardPagination)
        {
            SyntaxTypeReference intType = TypeReference.Parse("Int");
            SyntaxTypeReference stringType = TypeReference.Parse("String");

            arguments.AddOrUpdate(First, PagingArguments_First_Description, intType);
            arguments.AddOrUpdate(After, PagingArguments_After_Description, stringType);

            if (allowBackwardPagination)
            {
                arguments.AddOrUpdate(Last, PagingArguments_Last_Description, intType);
                arguments.AddOrUpdate(Before, PagingArguments_Before_Description, stringType);
            }
        }

        private static void AddOrUpdate(
            this IList<ArgumentDefinition> arguments,
            NameString name,
            string description,
            ITypeReference type)
        {
            ArgumentDefinition? argument = arguments.FirstOrDefault(t => t.Name.Equals(name));

            if (argument is null)
            {
                argument = new(name);
                arguments.Add(argument);
            }

            argument.Description ??= description;
            argument.Type = type;
        }

        private static ITypeReference CreateConnectionTypeRef(
            IDescriptorContext context,
            MemberInfo? resolverMember,
            NameString connectionName,
            ITypeReference? nodeType,
            PagingOptions options)
        {
            if (nodeType is null)
            {
                // if there is no explict node type provided we will try and 
                // infer the schema type from the resolver member.
                nodeType = TypeReference.Create(
                    PagingHelper.GetSchemaType(
                        context.TypeInspector,
                        resolverMember,
                        null),
                    TypeContext.Output);
            }

            options = context.GetSettings(options);

            // last but not leas we create a type reference that can be put on the field definition
            // to tell the type discovery that this field needs this result type.
            return CreateConnectionType(
                connectionName,
                nodeType,
                options.IncludeTotalCount ?? false);
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

        private static ITypeReference CreateConnectionType(
            NameString connectionName,
            ITypeReference nodeType,
            bool withTotalCount)
        {
            return TypeReference.Create(
                connectionName + "Connection",
                TypeContext.Output,
                factory: _ => new ConnectionType(connectionName, nodeType, withTotalCount));
        }

        private static NameString EnsureConnectionNameCasing(string connectionName)
        {
            if (char.IsUpper(connectionName[0]))
            {
                return connectionName;
            }

            return string.Concat(char.ToUpper(connectionName[0]), connectionName.Substring(1));
        }
    }
}
