using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Projections;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Execution.Processing.OperationCompilerOptimizerHelper;
using static HotChocolate.Types.UnwrapFieldMiddlewareHelper;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Types;

public static class ProjectionObjectFieldDescriptorExtensions
{
    private static readonly MethodInfo s_factoryTemplate =
        typeof(ProjectionObjectFieldDescriptorExtensions)
            .GetMethod(nameof(CreateMiddleware), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// <para>
    /// Configure if this field should be projected by <see cref="UseProjection{T}"/> or if it
    /// should be skipped
    /// </para>
    /// <para>
    /// if <paramref name="isProjected"/> is false, this field will never be projected even if
    /// it is in the selection set
    /// if <paramref name="isProjected"/> is true, this field will always be projected even it
    /// it is not in the selection set
    /// </para>
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
        ArgumentNullException.ThrowIfNull(descriptor);

        descriptor
            .Extend()
            .OnBeforeCreate(
                c => c.Features.Update<ProjectionFeature>(
                    f => f is null
                        ? new ProjectionFeature(AlwaysProjected: isProjected)
                        : f with { AlwaysProjected = isProjected }));
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
        ArgumentNullException.ThrowIfNull(descriptor);

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
        ArgumentNullException.ThrowIfNull(descriptor);

        return UseProjection(descriptor, typeof(T), scope);
    }

    /// <summary>
    /// Projects the selection set of the request onto the field. Registers a middleware that
    /// uses the registered <see cref="ProjectionConvention"/> to apply the projections
    /// </summary>
    /// <param name="descriptor">The descriptor</param>
    /// <param name="type">
    /// The <see cref="Type"/> of the resolved field
    /// </param>
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
        Type? type,
        string? scope = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        FieldMiddlewareConfiguration placeholder =
            new(_ => _ => default, key: WellKnownMiddleware.Projection);

        var extension = descriptor.Extend();

        extension.Configuration.MiddlewareConfigurations.Add(placeholder);
        extension.Configuration.Flags |= CoreFieldFlags.UsesProjections;

        extension
            .OnBeforeCreate(
                (context, definition) =>
                {
                    var selectionType = type;

                    if (selectionType is null)
                    {
                        if (definition.ResultType is null ||
                            !context.TypeInspector.TryCreateTypeInfo(definition.ResultType, out var typeInfo))
                        {
                            throw new ArgumentException(
                                UseProjection_CannotHandleType,
                                nameof(descriptor));
                        }

                        selectionType = typeInfo.NamedType;
                    }

                    definition.Tasks.Add(
                        new OnCompleteTypeSystemConfigurationTask<ObjectFieldConfiguration>(
                            (c, d) => CompileMiddleware(selectionType, d, placeholder, c, scope),
                            definition,
                            ApplyConfigurationOn.BeforeCompletion));
                });

        return descriptor;
    }

    private static void CompileMiddleware(
        Type type,
        ObjectFieldConfiguration definition,
        FieldMiddlewareConfiguration placeholder,
        ITypeCompletionContext context,
        string? scope)
    {
        var convention = context.DescriptorContext.GetProjectionConvention(scope);
        RegisterOptimizer(definition, convention.CreateOptimizer());

        var feature = definition.Features.Get<ProjectionFeature>();

        if (feature is null)
        {
            definition.Features.Set(new ProjectionFeature(HasProjectionMiddleware: true));
        }
        else if (!feature.HasProjectionMiddleware)
        {
            definition.Features.Set(feature with { HasProjectionMiddleware = true });
        }

        var factory = s_factoryTemplate.MakeGenericMethod(type);
        var middleware = CreateDataMiddleware((IQueryBuilder)factory.Invoke(null, [convention])!);

        var index = definition.MiddlewareConfigurations.IndexOf(placeholder);
        definition.MiddlewareConfigurations[index] = new(middleware, key: WellKnownMiddleware.Projection);
    }

    private static IQueryBuilder CreateMiddleware<TEntity>(IProjectionConvention convention)
        => new ProjectionQueryBuilder(convention.CreateBuilder<TEntity>());

    private static Selection UnwrapMutationPayloadSelection(ISelectionSet selectionSet, ObjectField field)
    {
        ref var selection = ref Unsafe.As<SelectionSet>(selectionSet).GetSelectionsReference();
        ref var end = ref Unsafe.Add(ref selection, selectionSet.Selections.Count);

        while (Unsafe.IsAddressLessThan(ref selection, ref end))
        {
            if (ReferenceEquals(selection.Field, field))
            {
                return selection;
            }

            selection = ref Unsafe.Add(ref selection, 1)!;
        }

        throw new InvalidOperationException(
            ProjectionObjectFieldDescriptorExtensions_UnwrapMutationPayloadSelect_Failed);
    }

    private sealed class ProjectionQueryBuilder(IQueryBuilder innerBuilder) : IQueryBuilder
    {
        private const string MockContext = "HotChocolate.Data.Projections.ProxyContext";

        public void Prepare(IMiddlewareContext context)
        {
            // in case we are being called from the node/nodes field we need to enrich
            // the projections context with the type that shall be resolved.
            if (context.LocalContextData.TryGetValue(InternalType, out var value) &&
                value is ObjectType objectType &&
                objectType.RuntimeType != typeof(object))
            {
                var fieldProxy = new ObjectField(context.Selection.Field, objectType);
                var selection = CreateProxySelection(context.Selection, fieldProxy);
                context = new MiddlewareContextProxy(context, selection, objectType);
            }

            //for use case when projection is used with Mutation Conventions
            else if (context.Operation.Type is OperationType.Mutation &&
                context.Selection.Type.NamedType() is ObjectType mutationPayloadType &&
                mutationPayloadType.Features.TryGet(out MutationPayloadInfo? mutationInfo))
            {
                var dataField = mutationPayloadType.Fields[mutationInfo.DataField];
                var payloadSelectionSet = context.Operation.GetSelectionSet(context.Selection, mutationPayloadType);
                var selection = UnwrapMutationPayloadSelection(payloadSelectionSet, dataField);
                context = new MiddlewareContextProxy(context, selection, dataField.DeclaringType);
            }

            context.SetLocalState(MockContext, context);
            innerBuilder.Prepare(context);
        }

        public void Apply(IMiddlewareContext context)
        {
            context = context.GetLocalStateOrDefault<MiddlewareContextProxy>(MockContext) ?? context;
            innerBuilder.Apply(context);
        }
    }

    private sealed class MiddlewareContextProxy(
        IMiddlewareContext context,
        ISelection selection,
        ObjectType objectType)
        : IMiddlewareContext
    {
        private readonly IMiddlewareContext _context = context;

        public IDictionary<string, object?> ContextData => _context.ContextData;

        public Schema Schema => _context.Schema;

        public ObjectType ObjectType { get; } = objectType;

        public IOperation Operation => _context.Operation;

        public IOperationResultBuilder OperationResult => _context.OperationResult;

        public ISelection Selection { get; } = selection;

        public IVariableValueCollection Variables => _context.Variables;

        public Path Path => _context.Path;

        public IServiceProvider RequestServices => _context.RequestServices;

        public string ResponseName => _context.ResponseName;

        public bool HasErrors => _context.HasErrors;

        public IServiceProvider Services
        {
            get => _context.Services;
            set => _context.Services = value;
        }

        public IImmutableDictionary<string, object?> ScopedContextData
        {
            get => _context.ScopedContextData;
            set => _context.ScopedContextData = value;
        }

        public IImmutableDictionary<string, object?> LocalContextData
        {
            get => _context.LocalContextData;
            set => _context.LocalContextData = value;
        }

        public IType? ValueType { get => _context.ValueType; set => _context.ValueType = value; }

        public object? Result { get => _context.Result; set => _context.Result = value; }

        public bool IsResultModified => _context.IsResultModified;

        public CancellationToken RequestAborted => _context.RequestAborted;

        public IFeatureCollection Features => throw new NotImplementedException();

        public T Parent<T>() => _context.Parent<T>();

        public T ArgumentValue<T>(string name) => _context.ArgumentValue<T>(name);

        public TValueNode ArgumentLiteral<TValueNode>(string name) where TValueNode : IValueNode
            => _context.ArgumentLiteral<TValueNode>(name);

        public Optional<T> ArgumentOptional<T>(string name) => _context.ArgumentOptional<T>(name);

        public ValueKind ArgumentKind(string name) => _context.ArgumentKind(name);

        public T Service<T>() where T : notnull => _context.Service<T>();

        public T Service<T>(object key) where T : notnull => _context.Service<T>(key);

        public T Resolver<T>() => _context.Resolver<T>();

        public object Service(Type service) => _context.Service(service);

        public void ReportError(string errorMessage) => _context.ReportError(errorMessage);

        public void ReportError(IError error) => _context.ReportError(error);

        public void ReportError(Exception exception, Action<ErrorBuilder>? configure = null)
            => _context.ReportError(exception, configure);

        public IReadOnlyList<ISelection> GetSelections(
            ObjectType typeContext,
            ISelection? selection = null,
            bool allowInternals = false)
            => _context.GetSelections(typeContext, selection, allowInternals);

        public ISelectionCollection Select()
            => _context.Select();

        public ISelectionCollection Select(string fieldName)
            => _context.Select(fieldName);

        public T GetQueryRoot<T>() => _context.GetQueryRoot<T>();

        public IMiddlewareContext Clone() => _context.Clone();

        public ValueTask<T> ResolveAsync<T>() => _context.ResolveAsync<T>();

        public void RegisterForCleanup(
            Func<ValueTask> action,
            CleanAfter cleanAfter = CleanAfter.Resolver)
            => _context.RegisterForCleanup(action, cleanAfter);

        public IReadOnlyDictionary<string, ArgumentValue> ReplaceArguments(
            IReadOnlyDictionary<string, ArgumentValue> newArgumentValues)
            => _context.ReplaceArguments(newArgumentValues);

        public IReadOnlyDictionary<string, ArgumentValue> ReplaceArguments(
            ReplaceArguments replace)
            => _context.ReplaceArguments(replace);

        public ArgumentValue ReplaceArgument(string argumentName, ArgumentValue newArgumentValue)
            => _context.ReplaceArgument(argumentName, newArgumentValue);

        IResolverContext IResolverContext.Clone() => _context.Clone();
    }

    private static Selection.Sealed CreateProxySelection(ISelection selection, ObjectField field)
    {
        var includeConditionsSource = ((Selection)selection).IncludeConditions;
        var includeConditions = new long[includeConditionsSource.Length];
        includeConditionsSource.CopyTo(includeConditions);

        var proxy = new Selection.Sealed(selection.Id,
            selection.DeclaringType,
            field,
            field.Type,
            selection.SyntaxNode,
            selection.ResponseName,
            selection.Arguments,
            includeConditions,
            selection.IsInternal,
            selection.Strategy != SelectionExecutionStrategy.Serial,
            selection.ResolverPipeline,
            selection.PureResolver);
        proxy.SetSelectionSetId(((Selection)selection).SelectionSetId);
        proxy.Seal(selection.DeclaringOperation, selection.DeclaringSelectionSet);
        return proxy;
    }
}
