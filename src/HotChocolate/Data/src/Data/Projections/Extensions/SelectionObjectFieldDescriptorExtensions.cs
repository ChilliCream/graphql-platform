using System;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Projections;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Data.Projections.ProjectionProvider;
using static HotChocolate.Execution.Processing.OperationCompilerOptimizerHelper;

namespace HotChocolate.Types;

public static class ProjectionObjectFieldDescriptorExtensions
{
    private static readonly MethodInfo _factoryTemplate =
        typeof(ProjectionObjectFieldDescriptorExtensions)
            .GetMethod(nameof(CreateMiddleware), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// Configure if this field should be projected by <see cref="UseProjection{T}"/> or if it
    /// should be skipped
    ///
    /// if <paramref name="isProjected"/> is false, this field will never be projected even if
    /// it is in the selection set
    /// if <paramref name="isProjected"/> is true, this field will always be projected even it
    /// it is not in the selection set
    /// </summary>
    /// <param name="descriptor">The descriptor</param>
    /// <param name="isProjected">
    /// If false the field will never be projected, if true it will always be projected
    /// </param>
    /// <returns>The descriptor passed in by <paramref name="descriptor"/></returns>
    public static IObjectFieldDescriptor IsProjected(
        this IObjectFieldDescriptor descriptor,
        bool isProjected = true)
    {
        descriptor
            .Extend()
            .OnBeforeCreate(
                x => x.ContextData[ProjectionConvention.IsProjectedKey] = isProjected);

        return descriptor;
    }

    /// <summary>
    /// Projects the selection set of the request onto the field. Registers a middleware that
    /// uses the registered <see cref="ProjectionConvention"/> to apply the projections
    /// </summary>
    /// <param name="descriptor">The descriptor</param>
    /// <param name="scope">
    /// Specify which <see cref="ProjectionConvention"/> is used, based on the value passed in
    /// <see cref="ProjectionsSchemaBuilderExtensions.AddProjections{T}"/>
    /// </param>
    /// <returns>The descriptor passed in by <paramref name="descriptor"/></returns>
    /// <exception cref="ArgumentNullException">
    /// In case the descriptor is null
    /// </exception>
    public static IObjectFieldDescriptor UseProjection(
        this IObjectFieldDescriptor descriptor,
        string? scope = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return UseProjection(descriptor, null, scope);
    }

    /// <summary>
    /// Projects the selection set of the request onto the field. Registers a middleware that
    /// uses the registered <see cref="ProjectionConvention"/> to apply the projections
    /// </summary>
    /// <param name="descriptor">The descriptor</param>
    /// <param name="scope">
    /// Specify which <see cref="ProjectionConvention"/> is used, based on the value passed in
    /// <see cref="ProjectionsSchemaBuilderExtensions.AddProjections{T}"/>
    /// </param>
    /// <typeparam name="T">
    /// The <see cref="Type"/> of the resolved field
    /// </typeparam>
    /// <returns>The descriptor passed in by <paramref name="descriptor"/></returns>
    /// <exception cref="ArgumentNullException">
    /// In case the descriptor is null
    /// </exception>
    public static IObjectFieldDescriptor UseProjection<T>(
        this IObjectFieldDescriptor descriptor,
        string? scope = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return UseProjection(descriptor, typeof(T), scope);
    }

    /// <summary>
    /// Projects the selection set of the request onto the field. Registers a middleware that
    /// uses the registered <see cref="ProjectionConvention"/> to apply the projections
    /// </summary>
    /// <param name="descriptor">The descriptor</param>
    /// <param name="scope">
    /// Specify which <see cref="ProjectionConvention"/> is used, based on the value passed in
    /// <see cref="ProjectionsSchemaBuilderExtensions.AddProjections{T}"/>
    /// </param>
    /// <param name="type">
    /// The <see cref="Type"/> of the resolved field
    /// </param>
    /// <returns>The descriptor passed in by <paramref name="descriptor"/></returns>
    /// <exception cref="ArgumentNullException">
    /// In case the descriptor is null
    /// </exception>
    public static IObjectFieldDescriptor UseProjection(
        this IObjectFieldDescriptor descriptor,
        Type? type,
        string? scope = null)
    {
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        FieldMiddlewareDefinition placeholder =
            new(_ => _ => default, key: WellKnownMiddleware.Projection);

        descriptor.Extend().Definition.MiddlewareDefinitions.Add(placeholder);

        descriptor
            .Extend()
            .OnBeforeCreate(
                (context, definition) =>
                {
                    var selectionType = type;

                    if (selectionType is null)
                    {
                        if (definition.ResultType is null ||
                            !context.TypeInspector.TryCreateTypeInfo(
                                definition.ResultType,
                                out var typeInfo))
                        {
                            throw new ArgumentException(
                                DataResources.UseProjection_CannotHandleType_,
                                nameof(descriptor));
                        }

                        selectionType = typeInfo.NamedType;
                    }

                    definition.Configurations.Add(
                        new CompleteConfiguration<ObjectFieldDefinition>(
                            (c, d) =>
                                CompileMiddleware(
                                    selectionType,
                                    d,
                                    placeholder,
                                    c,
                                    scope),
                            definition,
                            ApplyConfigurationOn.BeforeCompletion));
                });

        return descriptor;
    }

    private static void CompileMiddleware(
        Type type,
        ObjectFieldDefinition definition,
        FieldMiddlewareDefinition placeholder,
        ITypeCompletionContext context,
        string? scope)
    {
        var convention =
            context.DescriptorContext.GetProjectionConvention(scope);
        RegisterOptimizer(definition.ContextData, convention.CreateOptimizer());

        definition.ContextData[ProjectionContextIdentifier] = true;

        var factory = _factoryTemplate.MakeGenericMethod(type);
        var middleware = (FieldMiddleware)factory.Invoke(null, new object[] { convention })!;
        var index = definition.MiddlewareDefinitions.IndexOf(placeholder);
        definition.MiddlewareDefinitions[index] =
            new(middleware, key: WellKnownMiddleware.Projection);
    }

    private static FieldMiddleware CreateMiddleware<TEntity>(
        IProjectionConvention convention) =>
        convention.CreateExecutor<TEntity>();
}
