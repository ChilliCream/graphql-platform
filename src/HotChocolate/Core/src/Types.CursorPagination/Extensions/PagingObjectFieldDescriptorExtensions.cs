using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Types.Pagination.CursorPagingArgumentNames;
using static HotChocolate.Types.Pagination.PagingDefaults;
using static HotChocolate.Types.Properties.CursorResources;

namespace HotChocolate.Types;

public static class PagingObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Applies a cursor paging middleware to the field and rewrites the 
    /// field type to a connection.
    /// </summary>
    /// <param name="descriptor">The field descriptor.</param>
    /// <param name="resolvePagingProvider">
    /// A delegate that can resolve the correct paging provider for the field.
    /// </param>
    /// <param name="connectionName">
    /// The name of the connection.
    /// </param>
    /// <param name="options">
    /// The paging options.
    /// </param>
    /// <returns>
    /// Returns the field descriptor to allow configuration chaining.
    /// </returns>
    public static IObjectFieldDescriptor UsePaging<TNodeType, TEntity>(
        this IObjectFieldDescriptor descriptor,
        GetCursorPagingProvider? resolvePagingProvider = null,
        NameString? connectionName = null,
        PagingOptions options = default)
        where TNodeType : class, IOutputType =>
        UsePaging<TNodeType>(
            descriptor,
            typeof(TEntity),
            resolvePagingProvider,
            connectionName,
            options);

    /// <summary>
    /// Applies a cursor paging middleware to the field and rewrites the 
    /// field type to a connection.
    /// </summary>
    /// <param name="descriptor">The field descriptor.</param>
    /// <param name="entityType">
    /// The entity type represents the runtime type of the node.
    /// </param>
    /// <param name="resolvePagingProvider">
    /// A delegate that can resolve the correct paging provider for the field.
    /// </param>
    /// <param name="connectionName">
    /// The name of the connection.
    /// </param>
    /// <param name="options">
    /// The paging options.
    /// </param>
    /// <returns>
    /// Returns the field descriptor to allow configuration chaining.
    /// </returns>
    public static IObjectFieldDescriptor UsePaging<TNodeType>(
        this IObjectFieldDescriptor descriptor,
        Type? entityType = null,
        GetCursorPagingProvider? resolvePagingProvider = null,
        NameString? connectionName = null,
        PagingOptions options = default)
        where TNodeType : class, IOutputType =>
        UsePaging(
            descriptor,
            typeof(TNodeType),
            entityType,
            resolvePagingProvider,
            connectionName,
            options);

    /// <summary>
    /// Applies a cursor paging middleware to the field and rewrites the 
    /// field type to a connection.
    /// </summary>
    /// <param name="descriptor">The field descriptor.</param>
    /// <param name="nodeType">
    /// The schema type of the node.
    /// </param>
    /// <param name="entityType">
    /// The entity type represents the runtime type of the node.
    /// </param>
    /// <param name="resolvePagingProvider">
    /// A delegate that can resolve the correct paging provider for the field.
    /// </param>
    /// <param name="connectionName">
    /// The name of the connection.
    /// </param>
    /// <param name="options">
    /// The paging options.
    /// </param>
    /// <returns>
    /// Returns the field descriptor to allow configuration chaining.
    /// </returns>
    public static IObjectFieldDescriptor UsePaging(
        this IObjectFieldDescriptor descriptor,
        Type? nodeType = null,
        Type? entityType = null,
        GetCursorPagingProvider? resolvePagingProvider = null,
        NameString? connectionName = null,
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
            (services, source, name) => resolvePagingProvider(services, source, name),
            options);

        descriptor
            .Extend()
            .OnBeforeCreate((c, d) =>
            {
                PagingOptions pagingOptions = c.GetSettings(options);
                var backward = pagingOptions.AllowBackwardPagination ?? AllowBackwardPagination;

                CreatePagingArguments(d.Arguments, backward, pagingOptions.LegacySupport ?? false);

                if (connectionName is null or { IsEmpty: true })
                {
                    connectionName =
                        pagingOptions.InferConnectionNameFromField ??
                        InferConnectionNameFromField
                            ? (NameString?)EnsureConnectionNameCasing(d.Name)
                            : null;
                }

                ITypeReference? typeRef = nodeType is not null
                    ? c.TypeInspector.GetTypeRef(nodeType)
                    : null;

                if (typeRef is null &&
                    d.Type is SyntaxTypeReference syntaxTypeRef &&
                    syntaxTypeRef.Type.IsListType())
                {
                    typeRef = syntaxTypeRef.WithType(syntaxTypeRef.Type.ElementType());
                }

                MemberInfo? resolverMember = d.ResolverMember ?? d.Member;
                d.Type = CreateConnectionTypeRef(c, resolverMember, connectionName, typeRef, options);
                d.CustomSettings.Add(typeof(Connection));
            });

        return descriptor;
    }

    public static IInterfaceFieldDescriptor UsePaging<TNodeType>(
        this IInterfaceFieldDescriptor descriptor,
        NameString? connectionName = null,
        PagingOptions options = default)
        where TNodeType : class, IOutputType =>
        UsePaging(descriptor, typeof(TNodeType), connectionName, options);

    public static IInterfaceFieldDescriptor UsePaging(
        this IInterfaceFieldDescriptor descriptor,
        Type? nodeType = null,
        NameString? connectionName = null,
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
                PagingOptions pagingOptions = c.GetSettings(options);
                var backward = pagingOptions.AllowBackwardPagination ?? AllowBackwardPagination;

                CreatePagingArguments(d.Arguments, backward, pagingOptions.LegacySupport ?? false);

                if (connectionName is null or { IsEmpty: true })
                {
                    connectionName =
                        pagingOptions.InferConnectionNameFromField ??
                        InferConnectionNameFromField
                            ? (NameString?)EnsureConnectionNameCasing(d.Name)
                            : null;
                }

                ITypeReference? typeRef = nodeType is not null
                    ? c.TypeInspector.GetTypeRef(nodeType)
                    : null;

                if (typeRef is null &&
                    d.Type is SyntaxTypeReference syntaxTypeRef &&
                    syntaxTypeRef.Type.IsListType())
                {
                    typeRef = syntaxTypeRef.WithType(syntaxTypeRef.Type.ElementType());
                }

                d.Type = CreateConnectionTypeRef(c, d.Member, connectionName, typeRef, options);
            });


        return descriptor;
    }

    public static IObjectFieldDescriptor AddPagingArguments(
        this IObjectFieldDescriptor descriptor)
        => AddPagingArguments(descriptor, true);
    
    public static IObjectFieldDescriptor AddPagingArguments(
        this IObjectFieldDescriptor descriptor,
        bool allowBackwardPagination)
        => AddPagingArguments(descriptor, allowBackwardPagination, false);

    public static IObjectFieldDescriptor AddPagingArguments(
        this IObjectFieldDescriptor descriptor,
        bool allowBackwardPagination,
        bool legacySupport)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        CreatePagingArguments(
            descriptor.Extend().Definition.Arguments,
            allowBackwardPagination,
            legacySupport);

        return descriptor;
    }

    public static IInterfaceFieldDescriptor AddPagingArguments(
        this IInterfaceFieldDescriptor descriptor)
        => AddPagingArguments(descriptor, true);

    public static IInterfaceFieldDescriptor AddPagingArguments(
        this IInterfaceFieldDescriptor descriptor,
        bool allowBackwardPagination)
        => AddPagingArguments(descriptor, allowBackwardPagination, false);

    public static IInterfaceFieldDescriptor AddPagingArguments(
        this IInterfaceFieldDescriptor descriptor,
        bool allowBackwardPagination,
        bool legacySupport)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        CreatePagingArguments(
            descriptor.Extend().Definition.Arguments,
            allowBackwardPagination,
            legacySupport);

        return descriptor;
    }

    private static void CreatePagingArguments(
        IList<ArgumentDefinition> arguments,
        bool allowBackwardPagination, 
        bool legacySupport)
    {
        SyntaxTypeReference intType = 
            legacySupport 
                ? TypeReference.Parse(ScalarNames.PaginationAmount)
                : TypeReference.Parse(ScalarNames.Int);
                
        SyntaxTypeReference stringType = TypeReference.Parse(ScalarNames.String);

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
        NameString? connectionName,
        ITypeReference? nodeType,
        PagingOptions options)
    {
        ITypeInspector typeInspector = context.TypeInspector;

        if (nodeType is null)
        {
            // if there is no explicit node type provided we will try and
            // infer the schema type from the resolver member.
            nodeType = TypeReference.Create(
                PagingHelper.GetSchemaType(typeInspector, resolverMember),
                TypeContext.Output);
        }

        // if the node type is a syntax type reference we will try to preserve the actual
        // runtime type for later usage.
        if (nodeType.Kind == TypeReferenceKind.Syntax &&
            PagingHelper.TryGetNamedType(typeInspector, resolverMember, out Type? namedType))
        {
            context.TryBindRuntimeType(
                ((SyntaxTypeReference)nodeType).Type.NamedType().Name.Value,
                namedType);
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
        IExtendedType source,
        string? providerName)
    {
        try
        {
            Func<PagingProviderEntry, bool> predicate =
                providerName is null
                    ? entry => entry.Provider.CanHandle(source)
                    : entry => providerName.Equals(entry.Name, StringComparison.Ordinal);
            PagingProviderEntry? defaultEntry = null;


            foreach (PagingProviderEntry? entry in services.GetServices<PagingProviderEntry>())
            {
                // the first provider is expected to be the default provider.
                defaultEntry ??= entry;

                if (predicate(entry))
                {
                    return entry.Provider;
                }
            }

            if (defaultEntry is not null)
            {
                return defaultEntry.Provider;
            }
        }
        catch (InvalidOperationException)
        {
            // some containers will except if a service does not exist.
            // in this case we will ignore the exception and return the default provider.
        }

        // if no provider was added we will fallback to the queryable paging provider.
        return new QueryableCursorPagingProvider();
    }

    private static ITypeReference CreateConnectionType(
        NameString? connectionName,
        ITypeReference nodeType,
        bool withTotalCount)
    {
        return connectionName is null
            ? TypeReference.Create(
                "HotChocolate_Types_Connection",
                nodeType,
                _ => new ConnectionType(nodeType, withTotalCount),
                TypeContext.Output)
            : TypeReference.Create(
                connectionName.Value + "Connection",
                TypeContext.Output,
                factory: _ => new ConnectionType(
                    connectionName.Value,
                    nodeType,
                    withTotalCount));
    }

    private static NameString EnsureConnectionNameCasing(string connectionName)
    {
        if (char.IsUpper(connectionName[0]))
        {
            return connectionName;
        }

        return string.Concat(char.ToUpperInvariant(connectionName[0]), connectionName.Substring(1));
    }
}
