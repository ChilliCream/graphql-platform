using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Utilities;
using static HotChocolate.WellKnownMiddleware;

#nullable enable

namespace HotChocolate.Types.Pagination;

public static class PagingHelper
{
    public static IObjectFieldDescriptor UsePaging(
        IObjectFieldDescriptor descriptor,
        Type? entityType,
        GetPagingProvider resolvePagingProvider,
        PagingOptions options)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        FieldMiddlewareDefinition placeholder = new(_ => _ => default, key: Paging);

        var definition = descriptor.Extend().Definition;
        definition.MiddlewareDefinitions.Add(placeholder);
        definition.Configurations.Add(
            new CompleteConfiguration<ObjectFieldDefinition>(
                (c, d) => ApplyConfiguration(
                    c,
                    d,
                    entityType,
                    options.ProviderName,
                    resolvePagingProvider,
                    options,
                    placeholder),
                definition,
                ApplyConfigurationOn.BeforeCompletion));

        return descriptor;
    }

    private static void ApplyConfiguration(
        ITypeCompletionContext context,
        ObjectFieldDefinition definition,
        Type? entityType,
        string? name,
        GetPagingProvider resolvePagingProvider,
        PagingOptions options,
        FieldMiddlewareDefinition placeholder)
    {
        options = context.GetSettings(options);
        entityType ??= context.GetType<IOutputType>(definition.Type!).ToRuntimeType();

        var source = GetSourceType(context.TypeInspector, definition, entityType);
        var pagingProvider = resolvePagingProvider(context.Services, source, name);
        var pagingHandler = pagingProvider.CreateHandler(source, options);
        var middleware = CreateMiddleware(pagingHandler);

        var index = definition.MiddlewareDefinitions.IndexOf(placeholder);
        definition.MiddlewareDefinitions[index] = new(middleware, key: Paging);
    }

    private static IExtendedType GetSourceType(
        ITypeInspector typeInspector,
        ObjectFieldDefinition definition,
        Type entityType)
    {
        // if an explicit result type is defined we will type it since it expresses the
        // intend.
        if (definition.ResultType is not null)
        {
            return typeInspector.GetType(definition.ResultType);
        }

        // Otherwise we will look at specified members and extract the return type.
        var member = definition.ResolverMember ?? definition.Member;
        if (member is not null)
        {
            return typeInspector.GetReturnType(member, true);
        }

        // if we were not able to resolve the source type we will assume that it is
        // an enumerable of the entity type.
        return typeInspector.GetType(typeof(IEnumerable<>).MakeGenericType(entityType));
    }

    private static FieldMiddleware CreateMiddleware(
        IPagingHandler handler) =>
        FieldClassMiddlewareFactory.Create(
            typeof(PagingMiddleware),
            (typeof(IPagingHandler), handler));

    public static IExtendedType GetSchemaType(
        IDescriptorContext context,
        MemberInfo? member,
        Type? type = null)
    {
        var typeInspector = context.TypeInspector;

        if (type is null &&
            member is not null &&
            typeInspector.GetOutputReturnTypeRef(member) is ExtendedTypeReference r &&
            typeInspector.TryCreateTypeInfo(r.Type, out var typeInfo))
        {
            // if the member has already associated a schema type we will just take it.
            // Since we want the entity element we are going to take
            // the element type of the list or array as our entity type.
            if (r.Type.IsSchemaType && r.Type.IsArrayOrList)
            {
                return r.Type.ElementType!;
            }

            // if the member type is unknown we will try to infer it by extracting
            // the named type component from it and running the type inference.
            // It might be that we either are unable to infer or get the wrong type
            // in special cases. In the case we are getting it wrong the user has
            // to explicitly bind the type.
            if (context.TryInferSchemaType(
                r.WithType(typeInspector.GetType(typeInfo.NamedType)),
                out var schemaTypeRefs) &&
                schemaTypeRefs is { Length:> 0, } &&
                schemaTypeRefs[0] is ExtendedTypeReference schemaTypeRef)
            {
                // if we are able to infer the type we will reconstruct its structure so that
                // we can correctly extract from it the element type with the correct
                // nullability information.
                var current = schemaTypeRef.Type.Type;

                foreach (var component in typeInfo.Components.Reverse().Skip(1))
                {
                    if (component.Kind == TypeComponentKind.NonNull)
                    {
                        current = typeof(NonNullType<>).MakeGenericType(current);
                    }
                    else if (component.Kind == TypeComponentKind.List)
                    {
                        current = typeof(ListType<>).MakeGenericType(current);
                    }
                }

                if (typeInspector.GetType(current) is { IsArrayOrList: true, } schemaType)
                {
                    return schemaType.ElementType!;
                }
            }
        }

        if (type is null || !typeof(IType).IsAssignableFrom(type))
        {
            throw ThrowHelper.UsePagingAttribute_NodeTypeUnknown(member);
        }

        return typeInspector.GetType(type);
    }

    public static bool TryGetNamedType(
        ITypeInspector typeInspector,
        MemberInfo? member,
        [NotNullWhen(true)] out Type? namedType)
    {
        if (member is not null &&
            typeInspector.GetReturnType(member) is { } returnType &&
            typeInspector.TryCreateTypeInfo(returnType, out var typeInfo))
        {
            namedType = typeInfo.NamedType;
            return true;
        }

        namedType = null;
        return false;
    }

    public static PagingOptions GetSettings(
        this ITypeCompletionContext context,
        PagingOptions options) =>
        context.DescriptorContext.GetSettings(options);

    public static PagingOptions GetSettings(
        this IDescriptorContext context,
        PagingOptions options)
    {
        options = options.Copy();

        if (context.ContextData.TryGetValue(typeof(PagingOptions).FullName!, out var o) &&
            o is PagingOptions global)
        {
            options.Merge(global);
        }

        return options;
    }
}
