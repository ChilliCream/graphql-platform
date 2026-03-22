using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using static System.Linq.Expressions.Expression;

namespace HotChocolate.Resolvers;

/// <summary>
/// Compiles a batch resolver method into a <see cref="BatchFieldDelegate"/>
/// using expression trees. The compiled delegate has no reflection overhead
/// at execution time.
/// </summary>
internal static class BatchResolverCompiler
{
    private static readonly MethodInfo s_parent =
        typeof(IResolverContext).GetMethod(nameof(IResolverContext.Parent))!;

    private static readonly MethodInfo s_argumentValue =
        typeof(IResolverContext).GetMethods()
            .First(m => m.Name == nameof(IResolverContext.ArgumentValue) && m.IsGenericMethod);

    private static readonly MethodInfo s_resolver =
        typeof(IResolverContext).GetMethod(nameof(IResolverContext.Resolver))!;

    private static readonly PropertyInfo s_contextsLength =
        typeof(ImmutableArray<IMiddlewareContext>).GetProperty(nameof(ImmutableArray<IMiddlewareContext>.Length))!;

    private static readonly MethodInfo s_contextsItem =
        typeof(ImmutableArray<IMiddlewareContext>).GetProperty("Item")!.GetMethod!;

    /// <summary>
    /// Compiles a batch resolver method into a <see cref="BatchFieldDelegate"/>.
    /// Produces a delegate shaped like:
    /// <code>
    /// async contexts =>
    /// {
    ///     var parents = new List&lt;User&gt;(contexts.Length);
    ///     var arg1 = new List&lt;string&gt;(contexts.Length);
    ///     var svc = contexts[0].Service&lt;MyService&gt;();
    ///
    ///     for (int i = 0; i &lt; contexts.Length; i++)
    ///     {
    ///         parents.Add(contexts[i].Parent&lt;User&gt;());
    ///         arg1.Add(contexts[i].ArgumentValue&lt;string&gt;("arg1"));
    ///     }
    ///
    ///     var result = resolverMethod(parents, arg1, svc);
    ///     DistributeList(contexts, result);
    /// }
    /// </code>
    /// </summary>
    public static BatchFieldDelegate Compile(
        MethodInfo method,
        Type? sourceType,
        Type? resolverType,
        IReadOnlyDictionary<ParameterInfo, string> argumentNames,
        Func<ParameterInfo, IParameterExpressionBuilder> getBuilder)
    {
        var contextsParam = Parameter(typeof(ImmutableArray<IMiddlewareContext>), "contexts");
        var parameters = method.GetParameters();
        var variables = new List<ParameterExpression>();
        var preLoopStatements = new List<Expression>();
        var loopBodyStatements = new List<Expression>();
        var postLoopStatements = new List<Expression>();

        // Loop variable: int i
        var indexVar = Variable(typeof(int), "i");
        variables.Add(indexVar);

        // contexts[i] cast to IMiddlewareContext
        var contextAtIndex = Convert(
            Call(contextsParam, s_contextsItem, indexVar),
            typeof(IMiddlewareContext));

        // Build parameter expressions.
        var parameterVariables = new ParameterExpression[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
            var builder = getBuilder(param);
            var kind = builder.Kind;

            switch (kind)
            {
                case ArgumentKind.Source:
                {
                    // Batched: collect Parent<T>() from each context.
                    var (listVar, listInit, addExpr) =
                        CreateListCollector(contextsParam, contextAtIndex, param, ctx =>
                            Call(ctx, s_parent.MakeGenericMethod(GetListElementType(param.ParameterType)!)));

                    parameterVariables[i] = listVar;
                    variables.Add(listVar);
                    preLoopStatements.Add(listInit);
                    loopBodyStatements.Add(addExpr);
                    break;
                }

                case ArgumentKind.Argument:
                {
                    // Batched: collect ArgumentValue<T>() from each context.
                    var elementType = GetListElementType(param.ParameterType)!;
                    var argName = argumentNames.TryGetValue(param, out var name) ? name : param.Name!;

                    var (listVar, listInit, addExpr) =
                        CreateListCollector(contextsParam, contextAtIndex, param, ctx =>
                            Call(ctx, s_argumentValue.MakeGenericMethod(elementType), Constant(argName)));

                    parameterVariables[i] = listVar;
                    variables.Add(listVar);
                    preLoopStatements.Add(listInit);
                    loopBodyStatements.Add(addExpr);
                    break;
                }

                default:
                    // Singular: inject from contexts[0].
                    var paramVar = Variable(param.ParameterType, $"p{i}_{param.Name}");
                    parameterVariables[i] = paramVar;
                    variables.Add(paramVar);
                    preLoopStatements.Add(
                        Assign(paramVar, BuildFirstContextValue(contextsParam, param, builder)));
                    break;
            }
        }

        // Build the collection loop (only if there are batched parameters).
        if (loopBodyStatements.Count > 0)
        {
            var breakLabel = Label("break");

            var loop = Block(
                Assign(indexVar, Constant(0)),
                Loop(
                    IfThenElse(
                        LessThan(indexVar, Property(contextsParam, s_contextsLength)),
                        Block(
                            loopBodyStatements.Append(PostIncrementAssign(indexVar))),
                        Break(breakLabel)),
                    breakLabel));

            preLoopStatements.Add(loop);
        }

        // Call the resolver method.
        Expression callExpr;

        if (method.IsStatic)
        {
            callExpr = Call(method, parameterVariables);
        }
        else
        {
            var ownerExpr = BuildResolverOwner(contextsParam, method, sourceType, resolverType);
            callExpr = Call(ownerExpr, method, parameterVariables);
        }

        // Handle async vs sync return types.
        var returnType = method.ReturnType;
        var (unwrappedType, isAsync) = UnwrapAsyncType(returnType);

        if (isAsync)
        {
            return CompileAsync(
                contextsParam, variables, preLoopStatements, callExpr, returnType, unwrappedType);
        }

        // Sync: call method, distribute, return default ValueTask.
        var resultVar = Variable(unwrappedType, "result");
        variables.Add(resultVar);
        preLoopStatements.Add(Assign(resultVar, callExpr));
        preLoopStatements.Add(BuildDistributeResults(contextsParam, resultVar, unwrappedType));
        preLoopStatements.Add(Default(typeof(ValueTask)));

        var body = Block(typeof(ValueTask), variables, preLoopStatements);
        return Lambda<BatchFieldDelegate>(body, contextsParam).Compile();
    }

    private static (ParameterExpression listVar, Expression listInit, Expression addExpr) CreateListCollector(
        ParameterExpression contextsParam,
        Expression contextAtIndex,
        ParameterInfo parameter,
        Func<Expression, Expression> valueFactory)
    {
        var paramType = parameter.ParameterType;
        var elementType = GetListElementType(paramType)
            ?? throw new InvalidOperationException(
                $"Batch resolver parameter '{parameter.Name}' must be a list type "
                + $"(List<T>, IReadOnlyList<T>, T[], or ImmutableArray<T>). Got: {paramType}.");

        var listType = typeof(List<>).MakeGenericType(elementType);
        var listCtor = listType.GetConstructor([typeof(int)])!;
        var addMethod = listType.GetMethod("Add")!;

        var listVar = Variable(listType, $"list_{parameter.Name}");
        var listInit = Assign(listVar, New(listCtor, Property(contextsParam, s_contextsLength)));
        var addExpr = Call(listVar, addMethod, valueFactory(contextAtIndex));

        return (listVar, listInit, addExpr);
    }

    private static BatchFieldDelegate CompileAsync(
        ParameterExpression contextsParam,
        List<ParameterExpression> variables,
        List<Expression> bodyStatements,
        Expression callExpr,
        Type returnType,
        Type unwrappedType)
    {
        // Compile the argument-building + method call into a Func that returns the async result.
        // Then wrap with a thin async delegate that awaits and distributes.
        var resultVar = Variable(returnType, "asyncResult");
        variables.Add(resultVar);
        bodyStatements.Add(Assign(resultVar, callExpr));
        bodyStatements.Add(resultVar);

        var body = Block(returnType, variables, bodyStatements);

        if (returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var funcType = typeof(Func<,>).MakeGenericType(
                typeof(ImmutableArray<IMiddlewareContext>), returnType);
            var invoker = Lambda(funcType, body, contextsParam).Compile();

            var wrapMethod = typeof(BatchResolverCompiler)
                .GetMethod(nameof(WrapAsyncTask), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(unwrappedType);

            return (BatchFieldDelegate)wrapMethod.Invoke(null, [invoker])!;
        }
        else
        {
            var funcType = typeof(Func<,>).MakeGenericType(
                typeof(ImmutableArray<IMiddlewareContext>), returnType);
            var invoker = Lambda(funcType, body, contextsParam).Compile();

            var wrapMethod = typeof(BatchResolverCompiler)
                .GetMethod(nameof(WrapAsyncValueTask), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(unwrappedType);

            return (BatchFieldDelegate)wrapMethod.Invoke(null, [invoker])!;
        }
    }

    private static BatchFieldDelegate WrapAsyncTask<TResult>(
        Func<ImmutableArray<IMiddlewareContext>, Task<TResult>> invoker)
    {
        return async contexts =>
        {
            var result = await invoker(contexts).ConfigureAwait(false);
            DistributeList(contexts, result);
        };
    }

    private static BatchFieldDelegate WrapAsyncValueTask<TResult>(
        Func<ImmutableArray<IMiddlewareContext>, ValueTask<TResult>> invoker)
    {
        return async contexts =>
        {
            var result = await invoker(contexts).ConfigureAwait(false);
            DistributeList(contexts, result);
        };
    }

    private static void DistributeList<T>(ImmutableArray<IMiddlewareContext> contexts, T result)
    {
        if (result is null)
        {
            for (var i = 0; i < contexts.Length; i++)
            {
                contexts[i].Result = null;
            }

            return;
        }

        if (result is System.Collections.IList list)
        {
            for (var i = 0; i < contexts.Length; i++)
            {
                contexts[i].Result = i < list.Count ? list[i] : null;
            }
        }
        else
        {
            throw new InvalidOperationException(
                $"Batch resolver must return a list type. Got: {result.GetType()}.");
        }
    }

    /// <summary>
    /// Gets a value from the first context using the existing expression builder.
    /// </summary>
    private static Expression BuildFirstContextValue(
        ParameterExpression contextsParam,
        ParameterInfo parameter,
        IParameterExpressionBuilder builder)
    {
        var contextParam = Parameter(typeof(IResolverContext), "ctx");
        var buildContext = new ParameterExpressionBuilderContext(
            parameter,
            contextParam,
            new Dictionary<ParameterInfo, string>());
        var expr = builder.Build(buildContext);

        // Replace contextParam with (IResolverContext)contexts.ItemRef(0)
        var firstContext = Convert(
            Call(contextsParam, s_contextsItem, Constant(0)),
            typeof(IResolverContext));

        return new ParameterReplacer(contextParam, firstContext).Visit(expr);
    }

    /// <summary>
    /// Builds the expression to get the resolver owner instance from contexts[0].
    /// </summary>
    private static Expression BuildResolverOwner(
        ParameterExpression contextsParam,
        MethodInfo method,
        Type? sourceType,
        Type? resolverType)
    {
        var firstContext = Convert(
            Call(contextsParam, s_contextsItem, Constant(0)),
            typeof(IResolverContext));

        if (resolverType is not null && resolverType != sourceType)
        {
            return Call(firstContext, s_resolver.MakeGenericMethod(resolverType));
        }

        var parentType = sourceType ?? method.DeclaringType!;
        return Call(firstContext, s_parent.MakeGenericMethod(parentType));
    }

    private static Expression BuildDistributeResults(
        ParameterExpression contextsParam,
        ParameterExpression resultVar,
        Type resultType)
    {
        var distributeMethod = typeof(BatchResolverCompiler)
            .GetMethod(nameof(DistributeList), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resultType);

        return Call(distributeMethod, contextsParam, resultVar);
    }

    private static (Type unwrapped, bool isAsync) UnwrapAsyncType(Type type)
    {
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();

            if (def == typeof(Task<>) || def == typeof(ValueTask<>))
            {
                return (type.GetGenericArguments()[0], true);
            }
        }

        return (type, false);
    }

    internal static Type? GetListElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();

            if (def == typeof(List<>)
                || def == typeof(IReadOnlyList<>)
                || def == typeof(IList<>)
                || def == typeof(ImmutableArray<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    private sealed class ParameterReplacer(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
