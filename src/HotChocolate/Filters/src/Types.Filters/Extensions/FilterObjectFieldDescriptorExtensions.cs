using System;
using System.Globalization;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Filters;
using HotChocolate.Types.Filters.Properties;

namespace HotChocolate.Types;

[Obsolete("Use HotChocolate.Data.")]
public static class FilterObjectFieldDescriptorExtensions
{
    private const string _whereArgumentNamePlaceholder = "placeholder";
    private static readonly Type _middlewareDefinition =
        typeof(QueryableFilterMiddleware<>);

    [Obsolete("Use HotChocolate.Data.")]
    public static IObjectFieldDescriptor UseFiltering(
        this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return UseFiltering(descriptor, null);
    }

    [Obsolete("Use HotChocolate.Data.")]
    public static IObjectFieldDescriptor UseFiltering<T>(
        this IObjectFieldDescriptor descriptor)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        var filterType =
            typeof(IFilterInputType).IsAssignableFrom(typeof(T))
                ? typeof(T)
                : typeof(FilterInputType<>).MakeGenericType(typeof(T));

        return UseFiltering(descriptor, filterType);
    }

    [Obsolete("Use HotChocolate.Data.")]
    public static IObjectFieldDescriptor UseFiltering<T>(
        this IObjectFieldDescriptor descriptor,
        Action<IFilterInputTypeDescriptor<T>> configure)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var filterType = new FilterInputType<T>(configure);
        return UseFiltering(descriptor, filterType.GetType(), filterType);
    }

    private static IObjectFieldDescriptor UseFiltering(
        IObjectFieldDescriptor descriptor,
        Type? filterType,
        ITypeSystemMember? filterTypeInstance = null)
    {
        FieldMiddlewareDefinition placeholder =
            new(_ => _ => default, key: WellKnownMiddleware.Filtering);

        var argumentPlaceholder =
            "_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        descriptor.Extend().Definition.MiddlewareDefinitions.Add(placeholder);

        descriptor
            .Extend()
            .OnBeforeCreate((c, definition) =>
            {
                var argumentType = filterType;

                if (argumentType is null)
                {
                    if (definition.ResultType is null ||
                        definition.ResultType == typeof(object) ||
                        !c.TypeInspector.TryCreateTypeInfo(
                            definition.ResultType, out var typeInfo))
                    {
                        throw new ArgumentException(
                            FilterResources.FilterObjectFieldDescriptor_InvalidType,
                            nameof(descriptor));
                    }

                    argumentType = typeof(FilterInputType<>)
                        .MakeGenericType(typeInfo.NamedType);
                }

                TypeReference argumentTypeReference = filterTypeInstance is null
                    ? c.TypeInspector.GetTypeRef(
                        argumentType,
                        TypeContext.Input)
                    : TypeReference.Create(filterTypeInstance);

                if (argumentType == typeof(object))
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage(
                                FilterResources.FilterObjectFieldDescriptor_InvalidType_Msg)
                            .SetCode(ErrorCodes.Filtering.FilterObjectType)
                            .Build());
                }

                var argumentDefinition = new ArgumentDefinition
                {
                    Name = argumentPlaceholder,
                    Type = c.TypeInspector.GetTypeRef(argumentType, TypeContext.Input)
                };

                argumentDefinition.ConfigureArgumentName();
                definition.Arguments.Add(argumentDefinition);

                var fieldConfig = new CompleteConfiguration<ObjectFieldDefinition>(
                    (context, d) =>
                        CompileMiddleware(
                            context,
                            d,
                            argumentTypeReference,
                            placeholder),
                    definition,
                    ApplyConfigurationOn.BeforeCompletion,
                    argumentTypeReference,
                    TypeDependencyFulfilled.Completed);

                definition.Configurations.Add(fieldConfig);
            });

        return descriptor;
    }

    private static void CompileMiddleware(
        ITypeCompletionContext context,
        ObjectFieldDefinition definition,
        TypeReference argumentTypeReference,
        FieldMiddlewareDefinition placeholder)
    {
        var convention =
            context.DescriptorContext.GetFilterNamingConvention();
        var type = context.GetType<IFilterInputType>(argumentTypeReference);
        var middlewareType = _middlewareDefinition.MakeGenericType(type.EntityType);
        var middleware =
            FieldClassMiddlewareFactory.Create(
                middlewareType,
                (
                    typeof(FilterMiddlewareContext),
                    FilterMiddlewareContext.Create(convention.ArgumentName)
                ));
        var index = definition.MiddlewareDefinitions.IndexOf(placeholder);
        definition.MiddlewareDefinitions[index] =
            new(middleware, key: WellKnownMiddleware.Filtering);
    }

    private static IObjectFieldDescriptor AddFilterArguments(
        this IObjectFieldDescriptor descriptor,
        Type filterType)
    {
        return descriptor.Argument(_whereArgumentNamePlaceholder, a =>
            a.Extend()
                .OnBeforeCreate((c, d) =>
                    d.ConfigureArgumentName().Type =
                        c.TypeInspector.GetTypeRef(filterType, TypeContext.Input)));
    }

    [Obsolete("Use HotChocolate.Data.")]
    public static IObjectFieldDescriptor AddFilterArguments<TFilter>(
        this IObjectFieldDescriptor descriptor)
        where TFilter : class, IInputType, IFilterInputType
    {
        return descriptor.Argument(_whereArgumentNamePlaceholder,
            a => a.Type<TFilter>().Extend().ConfigureArgumentName());
    }

    [Obsolete("Use HotChocolate.Data.")]
    public static IInterfaceFieldDescriptor AddFilterArguments<TFilter>(
        this IInterfaceFieldDescriptor descriptor)
        where TFilter : class, IInputType, IFilterInputType
    {
        return descriptor.Argument(_whereArgumentNamePlaceholder,
            a => a.Type<TFilter>().Extend().ConfigureArgumentName());
    }

    private static IDescriptorExtension<ArgumentDefinition> ConfigureArgumentName(
        this IDescriptorExtension<ArgumentDefinition> descriptor)
    {
        descriptor.OnBeforeCreate(x => x.ConfigureArgumentName());
        return descriptor;
    }

    private static ArgumentDefinition ConfigureArgumentName(
        this ArgumentDefinition definition)
    {
        var argumentConfig = new CompleteConfiguration<ArgumentDefinition>(
            (context, d) =>
            {
                var convention =
                    context.DescriptorContext.GetFilterNamingConvention();
                d.Name = convention.ArgumentName;
            },
            definition,
            ApplyConfigurationOn.BeforeCompletion);

        definition.Configurations.Add(argumentConfig);
        return definition;
    }
}
