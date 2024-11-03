using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Data;
using HotChocolate.Data.Projections;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Data.DataResources;
using static HotChocolate.Data.Projections.ProjectionProvider;
using static HotChocolate.Execution.Processing.OperationCompilerOptimizerHelper;
using static HotChocolate.Types.UnwrapFieldMiddlewareHelper;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Types;

public static class ProjectionObjectFieldDescriptorExtensions
{
    private static readonly MethodInfo _factoryTemplate =
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
        descriptor
            .Extend()
            .OnBeforeCreate(x => x.ContextData[ProjectionConvention.IsProjectedKey] = isProjected);

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
        if (descriptor is null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        FieldMiddlewareDefinition placeholder =
            new(_ => _ => default, key: WellKnownMiddleware.Projection);

        var extension = descriptor.Extend();

        extension.Definition.MiddlewareDefinitions.Add(placeholder);
        extension.Definition.Flags |= FieldFlags.UsesProjections;

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

                    definition.Configurations.Add(
                        new CompleteConfiguration<ObjectFieldDefinition>(
                            (c, d) => CompileMiddleware(selectionType, d, placeholder, c, scope),
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
        var convention = context.DescriptorContext.GetProjectionConvention(scope);
        RegisterOptimizer(definition.ContextData, convention.CreateOptimizer());

        definition.ContextData[ProjectionContextIdentifier] = true;

        var factory = _factoryTemplate.MakeGenericMethod(type);
        var middleware = CreateDataMiddleware((IQueryBuilder)factory.Invoke(null, [convention,])!);

        var index = definition.MiddlewareDefinitions.IndexOf(placeholder);
        definition.MiddlewareDefinitions[index] = new(middleware, key: WellKnownMiddleware.Projection);
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
        private const string _mockContext = "HotChocolate.Data.Projections.ProxyContext";

        public void Prepare(IMiddlewareContext context)
        {
            // in case we are being called from the node/nodes field we need to enrich
            // the projections context with the type that shall be resolved.
            if (context.LocalContextData.TryGetValue(InternalType, out var value) &&
                value is ObjectType objectType &&
                objectType.RuntimeType != typeof(object))
            {
                var fieldProxy = new NodeFieldProxy(context.Selection.Field, objectType);
                var selection = CreateProxySelection(context.Selection, fieldProxy);
                context = new MiddlewareContextProxy(context, selection, objectType);
            }

            //for use case when projection is used with Mutation Conventions
            else if (context.Operation.Type is OperationType.Mutation &&
                context.Selection.Type.NamedType() is ObjectType mutationPayloadType &&
                mutationPayloadType.ContextData.GetValueOrDefault(MutationConventionDataField, null)
                    is string dataFieldName)
            {
                var dataField = mutationPayloadType.Fields[dataFieldName];
                var payloadSelectionSet = context.Operation.GetSelectionSet(context.Selection, mutationPayloadType);
                var selection = UnwrapMutationPayloadSelection(payloadSelectionSet, dataField);
                context = new MiddlewareContextProxy(context, selection, dataField.DeclaringType);
            }

            context.SetLocalState(_mockContext, context);
            innerBuilder.Prepare(context);
        }

        public void Apply(IMiddlewareContext context)
        {
            context = context.GetLocalStateOrDefault<MiddlewareContextProxy>(_mockContext) ?? context;
            innerBuilder.Apply(context);
        }
    }

    private sealed class MiddlewareContextProxy : IMiddlewareContext
    {
        private readonly IMiddlewareContext _context;

        public MiddlewareContextProxy(
            IMiddlewareContext context,
            ISelection selection,
            IObjectType objectType)
        {
            _context = context;
            Selection = selection;
            ObjectType = objectType;
        }

        public IDictionary<string, object?> ContextData => _context.ContextData;

        public ISchema Schema => _context.Schema;

        public IObjectType ObjectType { get; }

        public IOperation Operation => _context.Operation;

        public IOperationResultBuilder OperationResult => _context.OperationResult;

        public ISelection Selection { get; }

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

        public void ReportError(Exception exception, Action<IErrorBuilder>? configure = null)
            => _context.ReportError(exception, configure);

        public IReadOnlyList<ISelection> GetSelections(
            IObjectType typeContext,
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

    private static Selection CreateProxySelection(ISelection selection, NodeFieldProxy field)
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

    private sealed class NodeFieldProxy : IObjectField
    {
        private readonly IObjectField _nodeField;
        private readonly ObjectType _type;
        private readonly Type _runtimeType;

        public NodeFieldProxy(IObjectField nodeField, ObjectType type)
        {
            _nodeField = nodeField;
            _type = type;
            _runtimeType = type.RuntimeType;
        }

        public IObjectType DeclaringType => _nodeField.DeclaringType;

        public bool IsParallelExecutable => _nodeField.IsParallelExecutable;

        public DependencyInjectionScope DependencyInjectionScope => _nodeField.DependencyInjectionScope;

        public FieldDelegate Middleware => _nodeField.Middleware;

        public FieldResolverDelegate? Resolver => _nodeField.Resolver;

        public PureFieldDelegate? PureResolver => _nodeField.PureResolver;

        public SubscribeResolverDelegate? SubscribeResolver => _nodeField.SubscribeResolver;

        public IResolverResultPostProcessor? ResultPostProcessor => _nodeField.ResultPostProcessor;

        public MemberInfo? Member => _nodeField.Member;

        public MemberInfo? ResolverMember => _nodeField.ResolverMember;

        public bool IsIntrospectionField => _nodeField.IsIntrospectionField;

        public bool IsDeprecated => _nodeField.IsDeprecated;

        public string? DeprecationReason => _nodeField.DeprecationReason;

        public int Index => _nodeField.Index;

        public string? Description => _nodeField.Description;

        public IDirectiveCollection Directives => _nodeField.Directives;

        public IReadOnlyDictionary<string, object?> ContextData => _nodeField.ContextData;

        public IOutputType Type => _type;

        public IFieldCollection<IInputField> Arguments => _nodeField.Arguments;

        public string Name => _nodeField.Name;

        public SchemaCoordinate Coordinate => _nodeField.Coordinate;

        public Type RuntimeType => _runtimeType;

        IComplexOutputType IOutputField.DeclaringType => _nodeField.DeclaringType;

        ITypeSystemObject IField.DeclaringType => ((IField)_nodeField).DeclaringType;
    }
}
