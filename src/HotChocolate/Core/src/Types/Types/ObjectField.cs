using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a field of an <see cref="ObjectType"/>.
/// </summary>
public sealed class ObjectField : OutputFieldBase, IObjectField
{
    private static readonly FieldDelegate _empty = _ => throw new InvalidOperationException();

    internal ObjectField(ObjectFieldDefinition definition, int index)
        : base(definition, index)
    {
        Member = definition.Member;
        ResolverMember = definition.ResolverMember ?? definition.Member;
        Middleware = _empty;
        Resolver = definition.Resolver!;
        ResolverExpression = definition.Expression;
        SubscribeResolver = definition.SubscribeResolver;
    }

    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    public new ObjectType DeclaringType => (ObjectType)base.DeclaringType;

    IObjectType IObjectField.DeclaringType => DeclaringType;

    /// <summary>
    /// Defines if this field can be executed in parallel with other fields.
    /// </summary>
    public bool IsParallelExecutable
    {
        get => (Flags & FieldFlags.ParallelExecutable) == FieldFlags.ParallelExecutable;
        private set
        {
            if (value)
            {
                Flags |= FieldFlags.ParallelExecutable;
            }
            else
            {
                Flags &= ~FieldFlags.ParallelExecutable;
            }
        }
    }

    /// <summary>
    /// Defines in which DI scope this field is executed.
    /// </summary>
    public DependencyInjectionScope DependencyInjectionScope { get; private set; }

    /// <summary>
    /// Gets the field resolver middleware.
    /// </summary>
    public FieldDelegate Middleware { get; private set; }

    /// <summary>
    /// Gets the field resolver.
    /// </summary>
    public FieldResolverDelegate? Resolver { get; private set; }

    /// <summary>
    /// Gets the pure field resolver. The pure field resolver is only available if this field
    /// can be resolved without side effects. The execution engine will prefer this resolver
    /// variant if it is available and there are no executable directives that add a middleware
    /// to this field.
    /// </summary>
    public PureFieldDelegate? PureResolver { get; private set; }

    /// <summary>
    /// Gets the subscription resolver.
    /// </summary>
    public SubscribeResolverDelegate? SubscribeResolver { get; }

    /// <summary>
    /// Gets the result post processor.
    /// </summary>
    public IResolverResultPostProcessor? ResultPostProcessor { get; private set; }

    /// <summary>
    /// Gets the associated member of the runtime type for this field.
    /// This property can be <c>null</c> if this field is not associated to
    /// a concrete member on the runtime type.
    /// </summary>
    public MemberInfo? Member { get; }

    /// <summary>
    /// Gets the resolver member of this filed.
    /// If this field has no explicit resolver member
    /// this property will return <see cref="Member"/>.
    /// </summary>
    public MemberInfo? ResolverMember { get; }

    /// <summary>
    /// Gets the associated resolver expression.
    /// This expression can be <c>null</c>.
    /// </summary>
    public Expression? ResolverExpression { get; }

    protected override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        OutputFieldDefinitionBase definition)
    {
        base.OnCompleteField(context, declaringMember, definition);
        CompleteResolver(context, (ObjectFieldDefinition)definition);
    }

    private void CompleteResolver(
        ITypeCompletionContext context,
        ObjectFieldDefinition definition)
    {
        var isIntrospectionField = IsIntrospectionField || DeclaringType.IsIntrospectionType();
        var fieldMiddlewareDefinitions = definition.GetMiddlewareDefinitions();
        var options = context.DescriptorContext.Options;
        var isMutation = ((RegisteredType)context).IsMutationType ?? false;

        if (definition.DependencyInjectionScope.HasValue)
        {
            DependencyInjectionScope = definition.DependencyInjectionScope.Value;
        }
        else
        {
            DependencyInjectionScope = isMutation
                ? options.DefaultMutationDependencyInjectionScope
                : options.DefaultQueryDependencyInjectionScope;
        }

        if (Directives.Count > 0)
        {
            List<FieldMiddlewareDefinition>? middlewareDefinitions = null;

            for (var i = Directives.Count - 1; i >= 0; i--)
            {
                var directive = Directives[i];

                if (directive.Type.Middleware is { } m)
                {
                    (middlewareDefinitions ??= fieldMiddlewareDefinitions.ToList()).Insert(
                        0,
                        new FieldMiddlewareDefinition(next => m(next, directive)));
                }
            }

            if (middlewareDefinitions is not null)
            {
                fieldMiddlewareDefinitions = middlewareDefinitions;
            }
        }

        var skipMiddleware =
            options.FieldMiddleware is not FieldMiddlewareApplication.AllFields && isIntrospectionField;

        var resolvers = definition.Resolvers;
        Resolver = resolvers.Resolver;

        if (resolvers.PureResolver is not null && IsPureContext())
        {
            PureResolver = FieldMiddlewareCompiler.Compile(
                definition.GetResultConverters(),
                resolvers.PureResolver,
                skipMiddleware);
        }

        // by definition fields with pure resolvers are parallel executable.
        if (!IsParallelExecutable && PureResolver is not null)
        {
            IsParallelExecutable = true;
        }

        var middleware = FieldMiddlewareCompiler.Compile(
            context.GlobalComponents,
            fieldMiddlewareDefinitions,
            definition.GetResultConverters(),
            Resolver,
            skipMiddleware);

        if (middleware is null)
        {
            context.ReportError(
                ObjectField_HasNoResolver(
                    context.Type.Name,
                    Name,
                    context.Type));
        }
        else
        {
            Middleware = middleware;
        }

        ResultPostProcessor = definition.ResultPostProcessor;

        // if the source generator has configured this field we will not try to infer a post processor with
        // reflection.
        if ((Flags & FieldFlags.SourceGenerator) != FieldFlags.SourceGenerator
            && ResultPostProcessor is null
            && PureResolver is null
            && ((Flags & FieldFlags.Stream) == FieldFlags.Stream
                || (Flags & FieldFlags.Connection) == FieldFlags.Connection
                || (Flags & FieldFlags.CollectionSegment) == FieldFlags.CollectionSegment
                || Type.IsListType()))
        {
            ResultPostProcessor =
                ResolverHelpers.CreateListPostProcessor(
                    context.TypeInspector,
                    GetResultType(definition, RuntimeType));
        }

        return;

        bool IsPureContext()
        {
            return skipMiddleware || (context.GlobalComponents.Count == 0 && fieldMiddlewareDefinitions.Count == 0);
        }

        static Type GetResultType(ObjectFieldDefinition definition, Type runtimeType)
        {
            if (definition.ResultType == null
                || definition.ResultType == typeof(object))
            {
                return runtimeType;
            }

            return definition.ResultType;
        }
    }
}

file static class ResolverHelpers
{
    private static readonly MethodInfo _createListPostProcessor =
        typeof(ResolverHelpers).GetMethod(
            nameof(CreateListPostProcessor),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    public static IResolverResultPostProcessor? CreateListPostProcessor(ITypeInspector inspector, Type type)
    {
        var extendedType = inspector.GetType(type);

        if (extendedType.IsArrayOrList)
        {
            var elementType = extendedType.ElementType!.Type;
            var generic = _createListPostProcessor.MakeGenericMethod(elementType);
            return (IResolverResultPostProcessor?)generic.Invoke(null, []);
        }

        return null;
    }

    private static IResolverResultPostProcessor CreateListPostProcessor<T>()
        => new ListPostProcessor<T>();
}
