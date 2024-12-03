using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Types.Pagination.PagingDefaults;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

/// <summary>
/// Provides offset paging extensions to <see cref="IObjectFieldDescriptor"/> and
/// <see cref="IInterfaceFieldDescriptor"/>.
/// </summary>
public static class OffsetPagingObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Applies the offset paging middleware to the current field.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="itemType">
    /// The item type.
    /// </param>
    /// <param name="resolvePagingProvider">
    /// A delegate allowing to dynamically define a paging resolver for a field.
    /// </param>
    /// <param name="collectionSegmentName">
    /// The name of the collection segment.
    /// </param>
    /// <param name="options">
    /// The paging settings that shall be applied to this field.
    /// </param>
    /// <typeparam name="TSchemaType">
    /// The schema type representation of the item type.
    /// </typeparam>
    /// <returns>
    /// Returns the field descriptor for chaining in other configurations.
    /// </returns>
    public static IObjectFieldDescriptor UseOffsetPaging<TSchemaType>(
        this IObjectFieldDescriptor descriptor,
        Type? itemType = null,
        GetOffsetPagingProvider? resolvePagingProvider = null,
        string? collectionSegmentName = null,
        PagingOptions? options = null)
        where TSchemaType : IOutputType =>
        UseOffsetPaging(
            descriptor,
            typeof(TSchemaType),
            itemType,
            resolvePagingProvider,
            collectionSegmentName,
            options);

    /// <summary>
    /// Applies the offset paging middleware to the current field.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="itemType">
    /// The schema type representation of the item.
    /// </param>
    /// <param name="entityType">
    /// The entity type represents the runtime type of the item.
    /// </param>
    /// <param name="resolvePagingProvider">
    /// A delegate allowing to dynamically define a paging resolver for a field.
    /// </param>
    /// <param name="collectionSegmentName">
    /// The name of the collection segment.
    /// </param>
    /// <param name="options">
    /// The paging settings that shall be applied to this field.
    /// </param>
    /// <returns>
    /// Returns the field descriptor for chaining in other configurations.
    /// </returns>
    public static IObjectFieldDescriptor UseOffsetPaging(
        this IObjectFieldDescriptor descriptor,
        Type? itemType = null,
        Type? entityType = null,
        GetOffsetPagingProvider? resolvePagingProvider = null,
        string? collectionSegmentName = null,
        PagingOptions? options = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        resolvePagingProvider ??= ResolvePagingProvider;

        descriptor.AddOffsetPagingArguments();

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
                if (string.IsNullOrEmpty(collectionSegmentName))
                {
                    collectionSegmentName =
                        pagingOptions.InferCollectionSegmentNameFromField ??
                        InferCollectionSegmentNameFromField
                            ? EnsureCollectionSegmentNameCasing(d.Name)
                            : null;
                }
                d.State = d.State.Add(WellKnownContextData.PagingOptions, pagingOptions);
                d.Flags |= FieldFlags.CollectionSegment;

                TypeReference? typeRef = itemType is not null
                    ? c.TypeInspector.GetTypeRef(itemType)
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
                d.Type = CreateTypeRef(c, resolverMember, collectionSegmentName, typeRef, options);
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

    /// <summary>
    /// Applies the offset paging middleware to the current field.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="collectionSegmentName">
    /// The name of the collection segment.
    /// </param>
    /// <param name="options">
    /// The paging settings that shall be applied to this field.
    /// </param>
    /// <typeparam name="TSchemaType">
    /// The schema type representation of the item type.
    /// </typeparam>
    /// <returns>
    /// Returns the field descriptor for chaining in other configurations.
    /// </returns>
    public static IInterfaceFieldDescriptor UseOffsetPaging<TSchemaType>(
        this IInterfaceFieldDescriptor descriptor,
        string? collectionSegmentName = null,
        PagingOptions? options = null)
        where TSchemaType : class, IOutputType =>
        UseOffsetPaging(descriptor, typeof(TSchemaType), collectionSegmentName, options);

    /// <summary>
    /// Applies the offset paging middleware to the current field.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="itemType">
    /// The schema type representation of the item type.
    /// </param>
    /// <param name="collectionSegmentName">
    /// The name of the collection segment.
    /// </param>
    /// <param name="options">
    /// The paging settings that shall be applied to this field.
    /// </param>
    /// <returns>
    /// Returns the field descriptor for chaining in other configurations.
    /// </returns>
    public static IInterfaceFieldDescriptor UseOffsetPaging(
        this IInterfaceFieldDescriptor descriptor,
        Type? itemType = null,
        string? collectionSegmentName = null,
        PagingOptions? options = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        descriptor.AddOffsetPagingArguments();

        descriptor
            .Extend()
            .OnBeforeCreate((c, d) =>
            {
                var pagingOptions = c.GetPagingOptions(options);
                if (string.IsNullOrEmpty(collectionSegmentName))
                {
                    collectionSegmentName =
                        pagingOptions.InferCollectionSegmentNameFromField ??
                        InferCollectionSegmentNameFromField
                            ? EnsureCollectionSegmentNameCasing(d.Name)
                            : null;
                }
                d.State = d.State.Add(WellKnownContextData.PagingOptions, pagingOptions);
                d.Flags |= FieldFlags.CollectionSegment;

                TypeReference? typeRef = itemType is not null
                    ? c.TypeInspector.GetTypeRef(itemType)
                    : null;

                if (typeRef is null &&
                    d.Type is SyntaxTypeReference syntaxTypeRef &&
                    syntaxTypeRef.Type.IsListType())
                {
                    typeRef = syntaxTypeRef.WithType(syntaxTypeRef.Type.ElementType());
                }

                var resolverMember =  d.Member;
                d.Type = CreateTypeRef(c, resolverMember, collectionSegmentName, typeRef, options);
            });

        return descriptor;
    }

    /// <summary>
    /// Adds the offset paging arguments to an object field.
    /// </summary>
    public static IObjectFieldDescriptor AddOffsetPagingArguments(
        this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        var skip = descriptor.Argument(OffsetPagingArgumentNames.Skip, a => a.Type<IntType>());
        skip.Extend().Definition.Flags |= FieldFlags.SkipArgument;

        return descriptor
            .Argument(OffsetPagingArgumentNames.Take, a => a.Type<IntType>());
    }

    /// <summary>
    /// Adds the offset paging arguments to an interface field.
    /// </summary>
    public static IInterfaceFieldDescriptor AddOffsetPagingArguments(
        this IInterfaceFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return descriptor
            .Argument(OffsetPagingArgumentNames.Skip, a => a.Type<IntType>())
            .Argument(OffsetPagingArgumentNames.Take, a => a.Type<IntType>());
    }

    private static TypeReference CreateTypeRef(
        IDescriptorContext context,
        MemberInfo? resolverMember,
        string? collectionSegmentName,
        TypeReference? itemsType,
        PagingOptions? options)
    {
        var typeInspector = context.TypeInspector;

        itemsType ??= TypeReference.Create(
            PagingHelper.GetSchemaType(context, resolverMember),
            TypeContext.Output);

        // if the node type is a syntax type reference we will try to preserve the actual
        // runtime type for later usage.
        if (itemsType.Kind == TypeReferenceKind.Syntax &&
            PagingHelper.TryGetNamedType(typeInspector, resolverMember, out var namedType))
        {
            context.TryBindRuntimeType(
                ((SyntaxTypeReference)itemsType).Type.NamedType().Name.Value,
                namedType);
        }

        options = context.GetPagingOptions(options);

        // last but not leas we create a type reference that can be put on the field definition
        // to tell the type discovery that this field needs this result type.
        return collectionSegmentName is null
            ? TypeReference.Create(
                "HotChocolate_Types_CollectionSegment",
                itemsType,
                _ => new CollectionSegmentType(
                    null,
                    itemsType,
                    options.IncludeTotalCount ?? false),
                TypeContext.Output)
            : TypeReference.Create(
                collectionSegmentName + "CollectionSegment",
                TypeContext.Output,
                factory: _ => new CollectionSegmentType(
                    collectionSegmentName,
                    itemsType,
                    options.IncludeTotalCount ?? false));
    }

    private static OffsetPagingProvider ResolvePagingProvider(
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
        return new QueryableOffsetPagingProvider();
    }

    private static string EnsureCollectionSegmentNameCasing(string collectionSegmentName)
    {
        if (char.IsUpper(collectionSegmentName[0]))
        {
            return collectionSegmentName;
        }

        return string.Concat(
            char.ToUpper(collectionSegmentName[0]),
            collectionSegmentName.Substring(1));
    }
}
