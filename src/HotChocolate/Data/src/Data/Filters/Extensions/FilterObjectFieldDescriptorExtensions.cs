using System.Globalization;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Filters;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Types.UnwrapFieldMiddlewareHelper;

namespace HotChocolate.Types;

public static class FilterObjectFieldDescriptorExtensions
{
    private static readonly MethodInfo s_factoryTemplate =
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
        ArgumentNullException.ThrowIfNull(descriptor);

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
        ArgumentNullException.ThrowIfNull(descriptor);

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
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(configure);

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
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(type);

        var filterType =
            typeof(IFilterInputType).IsAssignableFrom(type)
                ? type
                : typeof(FilterInputType<>).MakeGenericType(type);

        return UseFiltering(descriptor, filterType, null, scope);
    }

    /// <summary>
    /// Adds filtering arguments to an interface field.
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments are applied to</param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="FilterConvention" /></param>
    public static IInterfaceFieldDescriptor UseFiltering(
        this IInterfaceFieldDescriptor descriptor,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return UseFiltering(descriptor, null, null, scope);
    }

    /// <summary>
    /// Adds filtering arguments to an interface field.
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments are applied to</param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="FilterConvention" /></param>
    /// <typeparam name="T">Either a runtime type or a <see cref="FilterInputType"/></typeparam>
    public static IInterfaceFieldDescriptor UseFiltering<T>(
        this IInterfaceFieldDescriptor descriptor,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var filterType =
            typeof(IFilterInputType).IsAssignableFrom(typeof(T))
                ? typeof(T)
                : typeof(FilterInputType<>).MakeGenericType(typeof(T));

        return UseFiltering(descriptor, filterType, null, scope);
    }

    /// <summary>
    /// Adds filtering arguments to an interface field.
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments are applied to</param>
    /// <param name="configure">Configures the filter input types that is used by the field
    /// </param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="FilterConvention" /></param>
    public static IInterfaceFieldDescriptor UseFiltering<T>(
        this IInterfaceFieldDescriptor descriptor,
        Action<IFilterInputTypeDescriptor<T>> configure,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(configure);

        var filterType = new FilterInputType<T>(configure);
        return UseFiltering(descriptor, filterType.GetType(), filterType, scope);
    }

    /// <summary>
    /// Adds filtering arguments to an interface field.
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments are applied to</param>
    /// <param name="type">Either a runtime type or a <see cref="FilterInputType"/></param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="FilterConvention" /></param>
    public static IInterfaceFieldDescriptor UseFiltering(
        this IInterfaceFieldDescriptor descriptor,
        Type type,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(type);

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
        FieldMiddlewareConfiguration placeholder = new(_ => _ => default);

        var argumentPlaceholder =
            "_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        descriptor.Extend().Configuration.MiddlewareConfigurations.Add(placeholder);

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

                        if (definition.ResultType is null
                            || definition.ResultType == typeof(object)
                            || !c.TypeInspector.TryCreateTypeInfo(definition.ResultType, out var typeInfo))
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

                    var argumentDefinition = new ArgumentConfiguration
                    {
                        Name = argumentPlaceholder,
                        Type = argumentTypeReference,
                        Flags = CoreFieldFlags.FilterArgument
                    };

                    definition.Arguments.Add(argumentDefinition);

                    definition.Tasks.Add(
                        new OnCompleteTypeSystemConfigurationTask<ObjectFieldConfiguration>(
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

                    argumentDefinition.Tasks.Add(
                        new OnCompleteTypeSystemConfigurationTask<ArgumentConfiguration>(
                            (context, argDef) =>
                                argDef.Name =
                                    context.GetFilterConvention(scope).GetArgumentName(),
                            argumentDefinition,
                            ApplyConfigurationOn.BeforeNaming));
                });

        return descriptor;
    }

    private static bool TryGetTypeInfo(
        IDescriptorContext context,
        InterfaceFieldConfiguration definition,
        [NotNullWhen(true)]
        out ITypeInfo? typeInfo)
    {
        if (definition.ResultType is not null
            && definition.ResultType != typeof(object)
            && context.TypeInspector.TryCreateTypeInfo(definition.ResultType, out typeInfo))
        {
            return true;
        }

        if (definition.Member is not null
            && context.TypeInspector.TryCreateTypeInfo(
                context.TypeInspector.GetReturnType(definition.Member),
                out typeInfo))
        {
            return true;
        }

        typeInfo = null;
        return false;
    }

    private static IInterfaceFieldDescriptor UseFiltering(
        IInterfaceFieldDescriptor descriptor,
        Type? filterType,
        ITypeSystemMember? filterTypeInstance,
        string? scope)
    {
        var argumentPlaceholder =
            "_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

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

                        if (!TryGetTypeInfo(c, definition, out var typeInfo))
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

                    var argumentDefinition = new ArgumentConfiguration
                    {
                        Name = argumentPlaceholder,
                        Type = argumentTypeReference,
                        Flags = CoreFieldFlags.FilterArgument
                    };

                    definition.Arguments.Add(argumentDefinition);

                    argumentDefinition.Tasks.Add(
                        new OnCompleteTypeSystemConfigurationTask<ArgumentConfiguration>(
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
        ObjectFieldConfiguration definition,
        TypeReference argumentTypeReference,
        FieldMiddlewareConfiguration placeholder,
        string? scope)
    {
        var type = context.GetType<IFilterInputType>(argumentTypeReference);
        var convention = context.DescriptorContext.GetFilterConvention(scope);

        var fieldDescriptor = ObjectFieldDescriptor.From(context.DescriptorContext, definition);
        convention.ConfigureField(fieldDescriptor);

        var factory = s_factoryTemplate.MakeGenericMethod(type.EntityType.Source);
        var middleware = CreateDataMiddleware((IQueryBuilder)factory.Invoke(null, [convention])!);

        var index = definition.MiddlewareConfigurations.IndexOf(placeholder);
        definition.MiddlewareConfigurations[index] = new(middleware, key: WellKnownMiddleware.Filtering);
    }

    private static IQueryBuilder CreateMiddleware<TEntity>(IFilterConvention convention) =>
        convention.CreateBuilder<TEntity>();
}
