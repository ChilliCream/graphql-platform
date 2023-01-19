using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;
using static HotChocolate.Utilities.ErrorHelper;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Represents a field of an <see cref="ObjectType"/>.
/// </summary>
public sealed class ObjectField
    : OutputFieldBase<ObjectFieldDefinition>
    , IObjectField
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
        get
        {
            return (Flags & FieldFlags.ParallelExecutable) == FieldFlags.ParallelExecutable;
        }
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
    /// Defines that the resolver pipeline returns an
    /// <see cref="IAsyncEnumerable{T}"/> as its result.
    /// </summary>
    public bool HasStreamResult
        => (Flags & FieldFlags.Stream) == FieldFlags.Stream;

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
    /// can be resolved without side-effects. The execution engine will prefer this resolver
    /// variant if it is available and there are no executable directives that add a middleware
    /// to this field.
    /// </summary>
    public PureFieldDelegate? PureResolver { get; private set; }

    /// <summary>
    /// Gets the subscription resolver.
    /// </summary>
    public SubscribeResolverDelegate? SubscribeResolver { get; }

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
    [Obsolete("Use resolver expression.")]
    public Expression? Expression => ResolverExpression;

    /// <summary>
    /// Gets the associated resolver expression.
    /// This expression can be <c>null</c>.
    /// </summary>
    public Expression? ResolverExpression { get; }

    protected override void OnCompleteField(
        ITypeCompletionContext context,
        ITypeSystemMember declaringMember,
        ObjectFieldDefinition definition)
    {
        base.OnCompleteField(context, declaringMember, definition);

        CompleteResolver(context, definition);
    }

    private void CompleteResolver(
        ITypeCompletionContext context,
        ObjectFieldDefinition definition)
    {
        var isIntrospectionField = IsIntrospectionField || DeclaringType.IsIntrospectionType();
        var fieldMiddlewareDefinitions = definition.GetMiddlewareDefinitions();
        var options = context.DescriptorContext.Options;

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
            options.FieldMiddleware is not FieldMiddlewareApplication.AllFields &&
            isIntrospectionField;

        var resolvers = CompileResolver(context, definition);

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
                        context.Type,
                        SyntaxNode));
        }
        else
        {
            Middleware = middleware;
        }

        bool IsPureContext()
        {
            return skipMiddleware ||
               (context.GlobalComponents.Count == 0 &&
               fieldMiddlewareDefinitions.Count == 0);
        }
    }

    private static FieldResolverDelegates CompileResolver(
        ITypeCompletionContext context,
        ObjectFieldDefinition definition)
    {
        var resolvers = definition.Resolvers;

        if (!resolvers.HasResolvers)
        {
            if (definition.Expression is LambdaExpression lambdaExpression)
            {
                resolvers = context.DescriptorContext.ResolverCompiler.CompileResolve(
                    lambdaExpression,
                    definition.SourceType ??
                    definition.Member?.ReflectedType ??
                    definition.Member?.DeclaringType ??
                    typeof(object),
                    definition.ResolverType);
            }
            else if (definition.ResolverMember is not null)
            {
                resolvers = context.DescriptorContext.ResolverCompiler.CompileResolve(
                    definition.ResolverMember,
                    definition.SourceType ??
                    definition.Member?.ReflectedType ??
                    definition.Member?.DeclaringType ??
                    typeof(object),
                    definition.ResolverType,
                    definition.GetParameterExpressionBuilders());
            }
            else if (definition.Member is not null)
            {
                resolvers = context.DescriptorContext.ResolverCompiler.CompileResolve(
                    definition.Member,
                    definition.SourceType ??
                    definition.Member.ReflectedType ??
                    definition.Member.DeclaringType,
                    definition.ResolverType,
                    definition.GetParameterExpressionBuilders());
            }
        }

        return resolvers;
    }
}
