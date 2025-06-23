using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Helpers;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a field of an <see cref="ObjectType"/>.
/// </summary>
public sealed class ObjectField : OutputField
{
    private static readonly FieldDelegate s_empty = _ => throw new InvalidOperationException();

    internal ObjectField(ObjectFieldConfiguration configuration, int index)
        : base(configuration, index)
    {
        Member = configuration.Member;
        ResolverMember = configuration.ResolverMember ?? configuration.Member;
        Middleware = s_empty;
        Resolver = configuration.Resolver!;
        ResolverExpression = configuration.Expression;
        SubscribeResolver = configuration.SubscribeResolver;
    }

    internal ObjectField(ObjectField original, IType type)
        : base(original, type)
    {
        Member = original.Member;
        ResolverMember = original.ResolverMember ?? original.Member;
        Middleware = original.Middleware;
        Resolver = original.Resolver!;
        ResolverExpression = original.ResolverExpression;
        SubscribeResolver = original.SubscribeResolver;
        ResultPostProcessor = original.ResultPostProcessor;
        PureResolver = original.PureResolver;
        DependencyInjectionScope = original.DependencyInjectionScope;
        Middleware = original.Middleware;
        Flags = original.Flags;
    }

    /// <summary>
    /// Gets the type that declares this field.
    /// </summary>
    public new ObjectType DeclaringType => Unsafe.As<ObjectType>(base.DeclaringType);

    /// <summary>
    /// Defines if this field can be executed in parallel with other fields.
    /// </summary>
    public bool IsParallelExecutable
    {
        get => (Flags & CoreFieldFlags.ParallelExecutable) == CoreFieldFlags.ParallelExecutable;
        private set
        {
            if (value)
            {
                Flags |= CoreFieldFlags.ParallelExecutable;
            }
            else
            {
                Flags &= ~CoreFieldFlags.ParallelExecutable;
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
    /// variant if it is available, and there are no executable directives that add middleware
    /// to this field.
    /// </summary>
    public PureFieldDelegate? PureResolver { get; private set; }

    /// <summary>
    /// Gets the subscription resolver.
    /// </summary>
    public SubscribeResolverDelegate? SubscribeResolver { get; private set; }

    /// <summary>
    /// Gets the result post-processor.
    /// </summary>
    public IResolverResultPostProcessor? ResultPostProcessor { get; private set; }

    /// <summary>
    /// Gets the associated member of the runtime type for this field.
    /// This property can be <c>null</c> if this field is not associated with
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
    public Expression? ResolverExpression { get; private set; }

    protected override void OnMakeExecutable(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        OutputFieldConfiguration definition)
    {
        var objectFieldDef = (ObjectFieldConfiguration)definition;
        base.OnMakeExecutable(context, declaringMember, definition);
        CompleteResolver(context, objectFieldDef);
        ResolverExpression = objectFieldDef.Expression;
        SubscribeResolver = objectFieldDef.SubscribeResolver;
    }

    private void CompleteResolver(
        ITypeCompletionContext context,
        ObjectFieldConfiguration definition)
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
            List<FieldMiddlewareConfiguration>? middlewareDefinitions = null;

            for (var i = Directives.Count - 1; i >= 0; i--)
            {
                var directive = Directives[i];

                if (directive.Type.Middleware is { } m)
                {
                    (middlewareDefinitions ??= fieldMiddlewareDefinitions.ToList()).Insert(
                        0,
                        new FieldMiddlewareConfiguration(next => m(next, directive)));
                }
            }

            if (middlewareDefinitions is not null)
            {
                fieldMiddlewareDefinitions = middlewareDefinitions;
            }
        }

        var skipMiddleware =
            options.FieldMiddleware is not FieldMiddlewareApplication.AllFields
                && isIntrospectionField;

        var resolvers = definition.Resolvers;
        Resolver = resolvers.Resolver;

        if (resolvers.PureResolver is not null && IsPureContext())
        {
            PureResolver = FieldMiddlewareCompiler.Compile(
                definition.GetResultConverters(),
                resolvers.PureResolver,
                skipMiddleware);
        }

        // by definition, fields with pure resolvers are parallel executable.
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

        // if the source generator has configured this field, we will not try to infer a post-processor with
        // reflection.
        if ((Flags & CoreFieldFlags.SourceGenerator) != CoreFieldFlags.SourceGenerator
            && ResultPostProcessor is null
            && PureResolver is null
            && ((Flags & CoreFieldFlags.Stream) == CoreFieldFlags.Stream
                || (Flags & CoreFieldFlags.Connection) == CoreFieldFlags.Connection
                || (Flags & CoreFieldFlags.CollectionSegment) == CoreFieldFlags.CollectionSegment
                || Type.IsListType()))
        {
            ResultPostProcessor =
                ResolverHelpers.CreateListPostProcessor(
                    context.TypeInspector,
                    GetResultType(definition, RuntimeType));
        }

        bool IsPureContext()
            => skipMiddleware
                || (context.GlobalComponents.Count == 0
                    && fieldMiddlewareDefinitions.Count == 0);

        static Type GetResultType(ObjectFieldConfiguration definition, Type runtimeType)
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
    private static readonly ConcurrentDictionary<Type, IResolverResultPostProcessor> s_methodCache = new();

    private static readonly MethodInfo s_createListPostProcessor =
        typeof(ResolverHelpers).GetMethod(
            nameof(CreateListPostProcessor),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    public static IResolverResultPostProcessor? CreateListPostProcessor(ITypeInspector inspector, Type type)
    {
        var extendedType = inspector.GetType(type);

        if(type == typeof(object))
        {
            return ListPostProcessor<object>.Default;
        }

        if (extendedType.IsArrayOrList)
        {
            var elementType = extendedType.ElementType!.Type;
            return GetFactoryMethod(elementType);
        }

        return null;
    }

    private static IResolverResultPostProcessor GetFactoryMethod(Type elementType)
        => s_methodCache.GetOrAdd(
            elementType,
            static t => (IResolverResultPostProcessor)s_createListPostProcessor.MakeGenericMethod(t).Invoke(null, [])!);

    private static IResolverResultPostProcessor CreateListPostProcessor<T>()
        => ListPostProcessor<T>.Default;
}
