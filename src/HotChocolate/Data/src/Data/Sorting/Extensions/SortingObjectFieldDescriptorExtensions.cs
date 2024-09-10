using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Sorting;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.ThrowHelper;
using static HotChocolate.Types.UnwrapFieldMiddlewareHelper;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;

public static class SortingObjectFieldDescriptorExtensions
{
    private static readonly MethodInfo _factoryTemplate =
        typeof(SortingObjectFieldDescriptorExtensions)
            .GetMethod(nameof(CreateBuilder), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// Registers the middleware and adds the arguments for sorting
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments and middleware are
    /// applied to</param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="SortConvention" /></param>
    public static IObjectFieldDescriptor UseSorting(
        this IObjectFieldDescriptor descriptor,
        string? scope = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return UseSortingInternal(descriptor, null, null, scope);
    }

    /// <summary>
    /// Registers the middleware and adds the arguments for sorting
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments and middleware are
    /// applied to</param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="SortConvention" /></param>
    /// <typeparam name="T">Either a runtime type or a <see cref="SortInputType"/></typeparam>
    public static IObjectFieldDescriptor UseSorting<T>(
        this IObjectFieldDescriptor descriptor,
        string? scope = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        var sortType =
            typeof(ISortInputType).IsAssignableFrom(typeof(T))
                ? typeof(T)
                : typeof(SortInputType<>).MakeGenericType(typeof(T));

        return UseSorting(descriptor, sortType, scope);
    }

    /// <summary>
    /// Registers the middleware and adds the arguments for sorting
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments and middleware are
    /// applied to</param>
    /// <param name="type">Either a runtime type or a <see cref="SortInputType"/></param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="SortConvention" /></param>
    public static IObjectFieldDescriptor UseSorting(
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

        var sortType =
            typeof(ISortInputType).IsAssignableFrom(type)
                ? type
                : typeof(SortInputType<>).MakeGenericType(type);

        return UseSortingInternal(descriptor, sortType, null, scope);
    }

    /// <summary>
    /// Registers the middleware and adds the arguments for sorting
    /// </summary>
    /// <param name="descriptor">The field descriptor where the arguments and middleware are
    /// applied to</param>
    /// <param name="configure">Configures the sort input types that is used by the field
    /// </param>
    /// <param name="scope">Specifies what scope should be used for the
    /// <see cref="SortConvention" /></param>
    public static IObjectFieldDescriptor UseSorting<T>(
        this IObjectFieldDescriptor descriptor,
        Action<ISortInputTypeDescriptor<T>> configure,
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

        var filterType = new SortInputType<T>(configure);
        return UseSortingInternal(descriptor, filterType.GetType(), filterType, scope);
    }

    private static IObjectFieldDescriptor UseSortingInternal(
        IObjectFieldDescriptor descriptor,
        Type? sortType,
        ITypeSystemMember? sortTypeInstance,
        string? scope)
    {
        FieldMiddlewareDefinition sortQuery = new(_ => _ => default, key: WellKnownMiddleware.Sorting);

        var argumentPlaceholder = "_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var fieldDefinition = descriptor.Extend().Definition;

        fieldDefinition.MiddlewareDefinitions.Add(sortQuery);

        descriptor
            .Extend()
            .OnBeforeCreate(
                (c, definition) =>
                {
                    TypeReference argumentTypeReference;
                    if (sortTypeInstance is not null)
                    {
                        argumentTypeReference =
                            TypeReference.Create(sortTypeInstance, scope);
                    }
                    else if (sortType is null)
                    {
                        var convention = c.GetSortConvention(scope);

                        if (definition.ResultType is null ||
                            definition.ResultType == typeof(object) ||
                            !c.TypeInspector.TryCreateTypeInfo(definition.ResultType, out var typeInfo))
                        {
                            throw new ArgumentException(
                                SortObjectFieldDescriptorExtensions_UseSorting_CannotHandleType,
                                nameof(descriptor));
                        }

                        argumentTypeReference = convention.GetFieldType(typeInfo.NamedType);
                    }
                    else
                    {
                        argumentTypeReference = c.TypeInspector.GetTypeRef(
                            sortType,
                            TypeContext.Input,
                            scope);
                    }

                    var argumentDefinition = new ArgumentDefinition
                    {
                        Name = argumentPlaceholder,
                        Type = argumentTypeReference,
                        Flags = FieldFlags.SortArgument
                    };

                    argumentDefinition.Configurations.Add(
                        new CompleteConfiguration<ArgumentDefinition>((context, def) =>
                        {
                            var namedType = context.GetType<INamedType>(argumentTypeReference);
                            def.Type = TypeReference.Parse($"[{namedType.Name}!]");
                        },
                        argumentDefinition,
                        ApplyConfigurationOn.BeforeNaming,
                        argumentTypeReference,
                        TypeDependencyFulfilled.Named));

                    definition.Arguments.Add(argumentDefinition);

                    definition.Configurations.Add(
                        new CompleteConfiguration<ObjectFieldDefinition>(
                            (context, def) =>
                                CompileMiddleware(
                                    context,
                                    def,
                                    argumentDefinition,
                                    sortQuery,
                                    scope),
                            definition,
                            ApplyConfigurationOn.BeforeCompletion,
                            argumentTypeReference,
                            TypeDependencyFulfilled.Completed));

                    argumentDefinition.Configurations.Add(
                        new CompleteConfiguration<ArgumentDefinition>(
                            (context, argDef) => argDef.Name = context.GetSortConvention(scope).GetArgumentName(),
                            argumentDefinition,
                            ApplyConfigurationOn.BeforeNaming));
                });

        return descriptor;
    }

    private static void CompileMiddleware(
        ITypeCompletionContext context,
        ObjectFieldDefinition definition,
        ArgumentDefinition argumentDefinition,
        FieldMiddlewareDefinition placeholder,
        string? scope)
    {
        var resolvedType = context.GetType<IType>(argumentDefinition.Type!);
        if (resolvedType.ElementType().NamedType() is not ISortInputType type)
        {
            throw Sorting_TypeOfInvalidFormat(resolvedType);
        }

        var convention = context.DescriptorContext.GetSortConvention(scope);

        var fieldDescriptor = ObjectFieldDescriptor.From(context.DescriptorContext, definition);
        convention.ConfigureField(fieldDescriptor);

        var factory = _factoryTemplate.MakeGenericMethod(type.EntityType.Source);
        var middleware = CreateDataMiddleware((IQueryBuilder)factory.Invoke(null, [convention,])!);

        var index = definition.MiddlewareDefinitions.IndexOf(placeholder);
        definition.MiddlewareDefinitions[index] = new(middleware, key: WellKnownMiddleware.Sorting);
    }

    private static IQueryBuilder CreateBuilder<TEntity>(
        ISortConvention convention) =>
        convention.CreateBuilder<TEntity>();
}
