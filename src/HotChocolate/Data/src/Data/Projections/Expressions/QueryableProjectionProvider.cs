using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.WellKnownContextData;

namespace HotChocolate.Data.Projections.Expressions;

public delegate object? ApplyProjection(IResolverContext context, object? input);

public class QueryableProjectionProvider : ProjectionProvider
{
    public static readonly string ContextApplyProjectionKey = nameof(ApplyProjection);
    public const string SkipProjectionKey = "SkipProjection";

    public QueryableProjectionProvider()
    {
    }

    public QueryableProjectionProvider(Action<IProjectionProviderDescriptor> configure)
        : base(configure)
    {
    }

    public override FieldMiddleware CreateExecutor<TEntityType>()
    {
        var applyProjection = CreateApplicator<TEntityType>();

        return next => context => ExecuteAsync(next, context);

        async ValueTask ExecuteAsync(
            FieldDelegate next,
            IMiddlewareContext context)
        {
            context.LocalContextData =
                context.LocalContextData.SetItem(ContextApplyProjectionKey, applyProjection);

            // first we let the pipeline run and produce a result.
            await next(context).ConfigureAwait(false);

            context.Result = applyProjection(context, context.Result);
        }
    }

    private static ApplyProjection CreateApplicator<TEntityType>()
        => (context, input) =>
        {
            if (input is null)
            {
                return input;
            }

            // if projections are already applied we can skip
            var skipProjection =
            context.LocalContextData.TryGetValue(SkipProjectionKey, out var skip) &&
            skip is true;

            // ensure sorting is only applied once
            context.LocalContextData =
            context.LocalContextData.SetItem(SkipProjectionKey, true);

            if (skipProjection)
            {
                return input;
            }

            // in case we are being called from the node/nodes field we need to enrich
            // the projections context with the type that shall be resolved.
            Type? selectionRuntimeType = null;
            ISelection? selection = null;

            if (context.LocalContextData.TryGetValue(InternalType, out var value) &&
                value is ObjectType objectType &&
                objectType.RuntimeType != typeof(object))
            {
                selectionRuntimeType = objectType.RuntimeType;
                var fieldProxy = new NodeFieldProxy(context.Selection.Field, objectType);
                selection = CreateProxySelection(context.Selection, fieldProxy);
            }

            var visitorContext =
                new QueryableProjectionContext(
                    context,
                    context.ObjectType,
                    selectionRuntimeType ?? context.Selection.Type.UnwrapRuntimeType());
            var visitor = new QueryableProjectionVisitor();

            // if we do not have a node selection proxy than this is a standard field and we
            // just traverse
            if (selection is null)
            {
                visitor.Visit(visitorContext);
            }

            // but if we have a node selection proxy we will push that into the visitor to use
            // it instead of the selection on the context.
            else
            {
                visitor.Visit(visitorContext, selection);
            }

            var projection = visitorContext.Project<TEntityType>();

            return input switch
            {
                IQueryable<TEntityType> q => q.Select(projection),
                IEnumerable<TEntityType> e => e.AsQueryable().Select(projection),
                QueryableExecutable<TEntityType> ex => ex.WithSource(ex.Source.Select(projection)),
                _ => input
            };
        };

    private static Selection CreateProxySelection(ISelection selection, NodeFieldProxy field)
    {
        var includeConditionsSource = ((Selection)selection).IncludeConditions;
        var includeConditions = new long[includeConditionsSource.Length];
        includeConditionsSource.CopyTo(includeConditions);

        var proxy = new Selection(selection.Id, selection.DeclaringType, field, field.Type, selection.SyntaxNode, selection.ResponseName, selection.Arguments, includeConditions, selection.IsInternal, selection.Strategy != SelectionExecutionStrategy.Serial, selection.ResolverPipeline, selection.PureResolver);
        proxy.SetSelectionSetId(((Selection)selection).SelectionSetId);
        proxy.Seal(selection.DeclaringSelectionSet);
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

        public bool HasStreamResult => _nodeField.HasStreamResult;

        public FieldDelegate Middleware => _nodeField.Middleware;

        public FieldResolverDelegate? Resolver => _nodeField.Resolver;

        public PureFieldDelegate? PureResolver => _nodeField.PureResolver;

        public SubscribeResolverDelegate? SubscribeResolver => _nodeField.SubscribeResolver;

        public IReadOnlyList<IDirective> ExecutableDirectives => _nodeField.ExecutableDirectives;

        public MemberInfo? Member => _nodeField.Member;

        public MemberInfo? ResolverMember => _nodeField.ResolverMember;

        public bool IsIntrospectionField => _nodeField.IsIntrospectionField;

        public bool IsDeprecated => _nodeField.IsDeprecated;

        public string? DeprecationReason => _nodeField.DeprecationReason;

        public int Index => _nodeField.Index;

        public string? Description => _nodeField.Description;

        public IDirectiveCollection Directives => _nodeField.Directives;

        public ISyntaxNode? SyntaxNode => _nodeField.SyntaxNode;

        public IReadOnlyDictionary<string, object?> ContextData => _nodeField.ContextData;

        public IOutputType Type => _type;

        public IFieldCollection<IInputField> Arguments => _nodeField.Arguments;

        public string Name => _nodeField.Name;

        public FieldCoordinate Coordinate => _nodeField.Coordinate;

        public Type RuntimeType => _runtimeType;

        IComplexOutputType IOutputField.DeclaringType => _nodeField.DeclaringType;

        ITypeSystemObject IField.DeclaringType => ((IField)_nodeField).DeclaringType;
    }
}
