using System.Globalization;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Types.UnwrapFieldMiddlewareHelper;

namespace HotChocolate.Types;

public static class FilterObjectFieldDescriptorExtensions
{
    private static readonly MethodInfo _factoryTemplate =
        typeof(FilterObjectFieldDescriptorExtensions)
            .GetMethod(nameof(CreateMiddleware), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// Registers the middleware and adds the arguments for filtering
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments and middleware are
    /// applied to</param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="FilterConvention" /></param>
    public static IObjectFieldDescriptor UseFiltering(
        this IObjectFieldDescriptor descriptor,
        string? scope = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return UseFiltering(descriptor, null, null, scope);
    }

    /// <summary>
    /// Registers the middleware and adds the arguments for filtering
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments and middleware are
    /// applied to</param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="FilterConvention" /></param>
    /// <typeparam name="T">Either a runtime type or a <see cref="FilterInputType"/></typeparam>
    public static IObjectFieldDescriptor UseFiltering<T>(
        this IObjectFieldDescriptor descriptor,
        string? scope = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        var filterType =
            typeof(IFilterInputType).IsAssignableFrom(typeof(T))
                ? typeof(T)
                : typeof(FilterInputType<>).MakeGenericType(typeof(T));

        return UseFiltering(descriptor, filterType, null, scope);
    }

    /// <summary>
    /// Registers the middleware and adds the arguments for filtering
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments and middleware are
    /// applied to</param>
    /// <param name="configure">Configures the filter input types that is used by the field
    /// </param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="FilterConvention" /></param>
    public static IObjectFieldDescriptor UseFiltering<T>(
        this IObjectFieldDescriptor descriptor,
        Action<IFilterInputTypeDescriptor<T>> configure,
        string? scope = null)
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
        return UseFiltering(descriptor, filterType.GetType(), filterType, scope);
    }

    /// <summary>
    /// Registers the middleware and adds the arguments for filtering
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments and middleware are
    /// applied to</param>
    /// <param name="type">Either a runtime type or a <see cref="FilterInputType"/></param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="FilterConvention" /></param>
    public static IObjectFieldDescriptor UseFiltering(
        this IObjectFieldDescriptor descriptor,
        Type type,
        string? scope = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var filterType =
            typeof(IFilterInputType).IsAssignableFrom(type)
                ? type
                : typeof(FilterInputType<>).MakeGenericType(type);

        return UseFiltering(descriptor, filterType, null, scope);
    }

    private static IObjectFieldDescriptor UseFiltering(
        IObjectFieldDescriptor descriptor,
        Type? filterType,
        ITypeSystemMember? filterTypeInstance,
        string? scope)
    {
        FieldMiddlewareDefinition placeholder = new(_ => _ => default);

        var argumentPlaceholder =
            "_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        descriptor.Extend().Definition.MiddlewareDefinitions.Add(placeholder);

        descriptor
            .Extend()
            .OnBeforeCreate(
                (c, definition) =>
                {
                    TypeReference argumentTypeReference;

                    if (filterTypeInstance is not null)
                    {
                        argumentTypeReference = TypeReference.Create(filterTypeInstance, scope);
                    }
                    else if (filterType is null)
                    {
                        var convention = c.GetFilterConvention(scope);

                        if (definition.ResultType is null ||
                            definition.ResultType == typeof(object) ||
                            !c.TypeInspector.TryCreateTypeInfo(definition.ResultType, out var typeInfo))
                        {
                            throw new ArgumentException(
                                FilterObjectFieldDescriptorExtensions_UseFiltering_CannotHandleType,
                                nameof(descriptor));
                        }

                        argumentTypeReference = convention.GetFieldType(typeInfo.NamedType);
                    }
                    else
                    {
                        argumentTypeReference = c.TypeInspector.GetTypeRef(
                            filterType,
                            TypeContext.Input,
                            scope);
                    }

                    var argumentDefinition = new ArgumentDefinition
                    {
                        Name = argumentPlaceholder,
                        Type = argumentTypeReference,
                        Flags = FieldFlags.FilterArgument
                    };

                    definition.Arguments.Add(argumentDefinition);

                    definition.Configurations.Add(
                        new CompleteConfiguration<ObjectFieldDefinition>(
                            (ctx, d) =>
                                CompileMiddleware(
                                    ctx,
                                    d,
                                    argumentTypeReference,
                                    placeholder,
                                    scope),
                            definition,
                            ApplyConfigurationOn.BeforeCompletion,
                            argumentTypeReference,
                            TypeDependencyFulfilled.Completed));

                    argumentDefinition.Configurations.Add(
                        new CompleteConfiguration<ArgumentDefinition>(
                            (context, argDef) =>
                                argDef.Name =
                                    context.GetFilterConvention(scope).GetArgumentName(),
                            argumentDefinition,
                            ApplyConfigurationOn.BeforeNaming));
                });

        return descriptor;
    }

    private static void CompileMiddleware(
        ITypeCompletionContext context,
        ObjectFieldDefinition definition,
        TypeReference argumentTypeReference,
        FieldMiddlewareDefinition placeholder,
        string? scope)
    {
        var type = context.GetType<IFilterInputType>(argumentTypeReference);
        var convention = context.DescriptorContext.GetFilterConvention(scope);

        var fieldDescriptor = ObjectFieldDescriptor.From(context.DescriptorContext, definition);
        convention.ConfigureField(fieldDescriptor);

        var factory = _factoryTemplate.MakeGenericMethod(type.EntityType.Source);
        var middleware = CreateDataMiddleware((IQueryBuilder)factory.Invoke(null, [convention,])!);

        var index = definition.MiddlewareDefinitions.IndexOf(placeholder);
        definition.MiddlewareDefinitions[index] = new(middleware, key: WellKnownMiddleware.Filtering);
    }

    private static IQueryBuilder CreateMiddleware<TEntity>(IFilterConvention convention) =>
        convention.CreateBuilder<TEntity>();
}
