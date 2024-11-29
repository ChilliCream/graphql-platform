using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Types.Pagination.CursorPagingArgumentNames;
using static HotChocolate.Types.Pagination.PagingDefaults;
using static HotChocolate.Types.Properties.CursorResources;

// ReSharper disable once CheckNamespace
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
        string? connectionName = null,
        PagingOptions? options = null)
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
        string? connectionName = null,
        PagingOptions? options = null)
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
        string? connectionName = null,
        PagingOptions? options = null)
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
                var pagingOptions = c.GetPagingOptions(options);
                var backward = pagingOptions.AllowBackwardPagination ?? AllowBackwardPagination;
                d.State = d.State.Add(WellKnownContextData.PagingOptions, pagingOptions);
                d.Flags |= FieldFlags.Connection;

                CreatePagingArguments(d.Arguments, backward);

                if (string.IsNullOrEmpty(connectionName))
                {
                    connectionName =
                        pagingOptions.InferConnectionNameFromField ??
                        InferConnectionNameFromField
                            ? EnsureConnectionNameCasing(d.Name)
                            : null;
                }

                TypeReference? typeRef = nodeType is not null
                    ? c.TypeInspector.GetTypeRef(nodeType)
                    : null;

                if (typeRef is null &&
                    d.Type is SyntaxTypeReference syntaxTypeRef &&
                    syntaxTypeRef.Type.IsListType())
                {
                    typeRef = syntaxTypeRef.WithType(syntaxTypeRef.Type.ElementType());
                }

                if (typeRef is null &&
                    d.Type is ExtendedTypeReference extendedTypeRef &&
                    c.TypeInspector.TryCreateTypeInfo(extendedTypeRef.Type, out var typeInfo) &&
                    GetElementType(typeInfo) is { } elementType)
                {
                    typeRef = TypeReference.Create(elementType, TypeContext.Output);
                }

                var resolverMember = d.ResolverMember ?? d.Member;
                d.Type = CreateConnectionTypeRef(c, resolverMember, connectionName, typeRef, options);
            });

        return descriptor;
    }

    private static IExtendedType? GetElementType(ITypeInfo typeInfo)
    {
        var elementType = false;

        for (var i = 0; i < typeInfo.Components.Count; i++)
        {
            var component = typeInfo.Components[i];

            if (elementType)
            {
                return component.Type;
            }

            if (component.Kind is TypeComponentKind.List)
            {
                elementType = true;
            }
        }

        return null;
    }

    public static IInterfaceFieldDescriptor UsePaging<TNodeType>(
        this IInterfaceFieldDescriptor descriptor,
        string? connectionName = null,
        PagingOptions? options = null)
        where TNodeType : class, IOutputType =>
        UsePaging(descriptor, typeof(TNodeType), connectionName, options);

    public static IInterfaceFieldDescriptor UsePaging(
        this IInterfaceFieldDescriptor descriptor,
        Type? nodeType = null,
        string? connectionName = null,
        PagingOptions? options = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor
            .Extend()
            .OnBeforeCreate((c, d) =>
            {
                var pagingOptions = c.GetPagingOptions(options);
                var backward = pagingOptions.AllowBackwardPagination ?? AllowBackwardPagination;
                d.State = d.State.Add(WellKnownContextData.PagingOptions, pagingOptions);
                d.Flags |= FieldFlags.Connection;

                CreatePagingArguments(d.Arguments, backward);

                if (string.IsNullOrEmpty(connectionName))
                {
                    connectionName =
                        pagingOptions.InferConnectionNameFromField ??
                        InferConnectionNameFromField
                            ? EnsureConnectionNameCasing(d.Name)
                            : null;
                }

                TypeReference? typeRef = nodeType is not null
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
        var intType = TypeReference.Parse(ScalarNames.Int);
        var stringType = TypeReference.Parse(ScalarNames.String);

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
        string name,
        string description,
        TypeReference type)
    {
        var argument = arguments.FirstOrDefault(t => t.Name.EqualsOrdinal(name));

        if (argument is null)
        {
            argument = new(name);
            arguments.Add(argument);
        }

        argument.Description ??= description;
        argument.Type = type;
    }

    private static TypeReference CreateConnectionTypeRef(
        IDescriptorContext context,
        MemberInfo? resolverMember,
        string? connectionName,
        TypeReference? nodeType,
        PagingOptions? options)
    {
        var typeInspector = context.TypeInspector;

        if (nodeType is null)
        {
            // if there is no explicit node type provided we will try and
            // infer the schema type from the resolver member.
            nodeType = TypeReference.Create(
                PagingHelper.GetSchemaType(context, resolverMember),
                TypeContext.Output);
        }

        // if the node type is a syntax type reference we will try to preserve the actual
        // runtime type for later usage.
        if (nodeType.Kind == TypeReferenceKind.Syntax &&
            PagingHelper.TryGetNamedType(typeInspector, resolverMember, out var namedType))
        {
            context.TryBindRuntimeType(
                ((SyntaxTypeReference)nodeType).Type.NamedType().Name.Value,
                namedType);
        }

        options = context.GetPagingOptions(options);

        // last but not least we create a type reference that can be put on the field definition
        // to tell the type discovery that this field needs this result type.
        return CreateConnectionType(
            connectionName,
            nodeType,
            options.IncludeTotalCount ?? false,
            options.IncludeNodesField ?? IncludeNodesField);
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

            // if we find an application service provider we will prefer that one.
            var applicationServices = services.GetService<IApplicationServiceProvider>();

            if (applicationServices is not null)
            {
                foreach (var entry in applicationServices.GetServices<PagingProviderEntry>())
                {
                    // the first provider is expected to be the default provider.
                    defaultEntry ??= entry;

                    if (predicate(entry))
                    {
                        return entry.Provider;
                    }
                }
            }

            foreach (var entry in services.GetServices<PagingProviderEntry>())
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

    private static TypeReference CreateConnectionType(
        string? connectionName,
        TypeReference nodeType,
        bool includeTotalCount,
        bool includeNodesField)
    {
        return connectionName is null
            ? TypeReference.Create(
                "HotChocolate_Types_Connection",
                nodeType,
                _ => new ConnectionType(nodeType, includeTotalCount, includeNodesField),
                TypeContext.Output)
            : TypeReference.Create(
                connectionName + "Connection",
                TypeContext.Output,
                factory: _ => new ConnectionType(
                    connectionName,
                    nodeType,
                    includeTotalCount,
                    includeNodesField));
    }

    private static string EnsureConnectionNameCasing(string connectionName)
        => char.IsUpper(connectionName[0])
            ? connectionName
            : string.Concat(char.ToUpperInvariant(connectionName[0]), connectionName.Substring(1));
}
