using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static System.Linq.Expressions.Expression;

namespace HotChocolate.Resolvers;

/// <summary>
/// Compiles a batch resolver method into a <see cref="BatchFieldDelegate"/>
/// using expression trees. The compiled delegate has no reflection overhead
/// at execution time.
/// </summary>
[UnconditionalSuppressMessage(
    "ReflectionAnalysis",
    "IL2060",
    Justification =
        "The generic methods being specialized have no trimming constraints on their type parameters.")]
[UnconditionalSuppressMessage(
    "AOT",
    "IL3050",
    Justification =
        "This compiler builds expression trees at schema initialization time and is only used in JIT-compatible "
        + "environments.")]
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
        ParameterExpression? selectionContext = null;

        // Loop variable: int i
        var indexVar = Variable(typeof(int), "i");
        variables.Add(indexVar);

        // contexts[i] cast to IMiddlewareContext
        var contextAtIndex = Convert(
            Call(contextsParam, s_contextsItem, indexVar),
            typeof(IMiddlewareContext));

        // Build parameter expressions.
        var parameterVariables = new Expression[parameters.Length];

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

                    parameterVariables[i] = ConvertList(listVar, param.ParameterType);
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

                    parameterVariables[i] = ConvertList(listVar, param.ParameterType);
                    variables.Add(listVar);
                    preLoopStatements.Add(listInit);
                    loopBodyStatements.Add(addExpr);
                    break;
                }

                default:
                    // Singular selection parameters share the partition's include conditions.
                    var isSelected = param.GetCustomAttribute<IsSelectedAttribute>();
                    Expression? bindingContext = null;

                    if (isSelected is not null
                        || kind is ArgumentKind.Selection or ArgumentKind.Custom
                        || param.ParameterType == typeof(IResolverContext))
                    {
                        if (selectionContext is null)
                        {
                            selectionContext = Variable(typeof(IResolverContext), "selectionContext");
                            variables.Add(selectionContext);
                            preLoopStatements.Add(Assign(
                                selectionContext,
                                Call(
                                    typeof(ResolverContextExtensions),
                                    nameof(ResolverContextExtensions.CreateBatchSelectionContext),
                                    Type.EmptyTypes,
                                    contextsParam)));
                        }

                        bindingContext = selectionContext;
                    }

                    var paramVar = Variable(param.ParameterType, $"p{i}_{param.Name}");
                    parameterVariables[i] = paramVar;
                    variables.Add(paramVar);
                    preLoopStatements.Add(
                        Assign(paramVar, isSelected is null
                            ? BuildFirstContextValue(contextsParam, param, builder, bindingContext)
                            : BuildIsSelectedValue(selectionContext!, isSelected)));
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
            ?? throw ThrowHelper.BatchResolver_ArgumentMustBeList(parameter);

        var listType = typeof(List<>).MakeGenericType(elementType);
        var listCtor = listType.GetConstructor([typeof(int)])!;
        var addMethod = listType.GetMethod("Add")!;

        var listVar = Variable(listType, $"list_{parameter.Name}");
        var listInit = Assign(listVar, New(listCtor, Property(contextsParam, s_contextsLength)));
        var addExpr = Call(listVar, addMethod, valueFactory(contextAtIndex));

        return (listVar, listInit, addExpr);
    }

    private static Expression ConvertList(ParameterExpression list, Type type)
    {
        if (type.IsArray)
        {
            return Call(
                list,
                typeof(List<>).MakeGenericType(type.GetElementType()!)
                    .GetMethod(nameof(List<object>.ToArray))!);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ImmutableArray<>))
        {
            return Call(
                typeof(ImmutableArray),
                nameof(ImmutableArray.ToImmutableArray),
                type.GetGenericArguments(),
                list);
        }

        return list;
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
            var count = list.Count;

            if (count != contexts.Length)
            {
                throw ThrowHelper.BatchResolver_ResultCountMismatch(contexts.Length, count);
            }

            for (var i = 0; i < contexts.Length; i++)
            {
                contexts[i].Result = list[i];
            }
        }
        else
        {
            throw ThrowHelper.BatchResolver_ResultMustBeList(result.GetType());
        }
    }

    /// <summary>
    /// Gets a singular parameter value from its binding context.
    /// </summary>
    private static Expression BuildFirstContextValue(
        ParameterExpression contextsParam,
        ParameterInfo parameter,
        IParameterExpressionBuilder builder,
        Expression? bindingContext)
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

        return new ParameterReplacer(contextParam, bindingContext ?? firstContext).Visit(expr);
    }

    private static Expression BuildIsSelectedValue(Expression context, IsSelectedAttribute attribute)
    {
        Func<IResolverContext, bool> evaluate;

        if (attribute.Fields is not null)
        {
            evaluate = ctx =>
            {
                var selected = new IsSelectedContext(ctx.Schema, ctx.Select());
                IsSelectedVisitor.Instance.Visit(attribute.Fields, selected);
                return selected.AllSelected;
            };
        }
        else
        {
            var names = new HashSet<string>(attribute.FieldNames);
            evaluate = ctx => ctx.Select().IsSelected(names);
        }

        return Invoke(Constant(evaluate), context);
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
                || def == typeof(IEnumerable<>)
                || def == typeof(ImmutableArray<>))
            {
                return type.GetGenericArguments()[0];
            }
        }

        return null;
    }

    internal static Type? GetResultElementType(Type type)
    {
        var (unwrapped, _) = UnwrapAsyncType(type);

        if (unwrapped.IsGenericType
            && unwrapped.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return null;
        }

        return GetListElementType(unwrapped);
    }

    private sealed class ParameterReplacer(ParameterExpression from, Expression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == from ? to : base.VisitParameter(node);
    }
}
