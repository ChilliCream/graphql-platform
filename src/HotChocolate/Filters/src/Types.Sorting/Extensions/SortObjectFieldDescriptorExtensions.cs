using System;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Sorting;

namespace HotChocolate.Types;

[Obsolete("Use HotChocolate.Data.")]
public static class SortObjectFieldDescriptorExtensions
{
    private static readonly Type _middlewareDefinition = typeof(QueryableSortMiddleware<>);

    [Obsolete("Use HotChocolate.Data.")]
    public static IObjectFieldDescriptor UseSorting(
        this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return UseSorting(descriptor, null);
    }

    [Obsolete("Use HotChocolate.Data.")]
    public static IObjectFieldDescriptor UseSorting<T>(
        this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        var sortType =
            typeof(ISortInputType).IsAssignableFrom(typeof(T))
                ? typeof(T)
                : typeof(SortInputType<>).MakeGenericType(typeof(T));

        return UseSorting(descriptor, sortType);
    }

    [Obsolete("Use HotChocolate.Data.")]
    public static IObjectFieldDescriptor UseSorting<T>(
        this IObjectFieldDescriptor descriptor,
        Action<ISortInputTypeDescriptor<T>> configure)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var sortType = new SortInputType<T>(configure);

        return UseSorting(descriptor, sortType.GetType(), sortType);
    }

    [Obsolete("Use HotChocolate.Data.")]
    public static IObjectFieldDescriptor UseSorting(
        this IObjectFieldDescriptor descriptor,
        Type? sortType,
        ITypeSystemMember? sortTypeInstance = null)
    {
        FieldMiddlewareDefinition placeholder =
            new(_ => _ => default, key: WellKnownMiddleware.Sorting);

        var argumentPlaceholder =
            "_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        descriptor.Extend().Definition.MiddlewareDefinitions.Add(placeholder);

        descriptor
            .Extend()
            .OnBeforeCreate((c,definition) =>
            {
                var argumentType = GetArgumentType(definition, sortType, c.TypeInspector);

                TypeReference argumentTypeReference = sortTypeInstance is null
                    ? c.TypeInspector.GetTypeRef(
                        argumentType,
                        TypeContext.Input)
                    : TypeReference.Create(sortTypeInstance);

                var argumentDefinition = new ArgumentDefinition
                {
                    Name = argumentPlaceholder,
                    Type = c.TypeInspector.GetTypeRef(argumentType, TypeContext.Input)
                };

                argumentDefinition.Configurations.Add(
                    new CompleteConfiguration<ArgumentDefinition>(
                        (context, d) =>
                        {
                            var convention =
                                context.DescriptorContext.GetSortingNamingConvention();
                            d.Name = convention.ArgumentName;
                        },
                        argumentDefinition,
                        ApplyConfigurationOn.
                        BeforeCompletion));

                definition.Arguments.Add(argumentDefinition);
                definition.Configurations.Add(
                    new CompleteConfiguration<ObjectFieldDefinition>(
                        (context, d) =>
                            CompileMiddleware(
                                context,
                                d,
                                argumentTypeReference,
                                placeholder),
                        definition,
                        ApplyConfigurationOn.BeforeCompletion,
                        argumentTypeReference,
                        TypeDependencyFulfilled.Completed));
            });

        return descriptor;
    }

    private static Type GetArgumentType(
        ObjectFieldDefinition definition,
        Type? filterType,
        ITypeInspector typeInspector)
    {
        var argumentType = filterType;

        if (argumentType is null)
        {
            if (definition.ResultType is null ||
                definition.ResultType == typeof(object) ||
                !typeInspector.TryCreateTypeInfo(
                    definition.ResultType, out var typeInfo))
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage("Cannot handle the specified type.")
                        .SetExtension("fieldName", definition.Name)
                        .Build());
            }

            argumentType = typeof(SortInputType<>).MakeGenericType(typeInfo.NamedType);
        }

        if (argumentType == typeof(object))
        {
            // TODO : resources
            throw new SchemaException(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The sort type cannot be " +
                        "inferred from `System.Object`.")
                    .SetCode(ErrorCodes.Filtering.FilterObjectType)
                    .Build());
        }

        return argumentType;
    }

    private static void CompileMiddleware(
        ITypeCompletionContext context,
        ObjectFieldDefinition definition,
        TypeReference argumentTypeReference,
        FieldMiddlewareDefinition placeholder)
    {
        var convention =
            context.DescriptorContext.GetSortingNamingConvention();

        var type = context.GetType<ISortInputType>(argumentTypeReference);
        var middlewareType = _middlewareDefinition.MakeGenericType(type.EntityType);

        var middleware =
            FieldClassMiddlewareFactory.Create(
                middlewareType,
                (
                    typeof(SortMiddlewareContext),
                    SortMiddlewareContext.Create(convention.ArgumentName
                    )));

        var index = definition.MiddlewareDefinitions.IndexOf(placeholder);
        definition.MiddlewareDefinitions[index] =
            new(middleware, key: WellKnownMiddleware.Sorting);
    }
}
