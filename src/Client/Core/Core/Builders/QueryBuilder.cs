using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Client.Core.Syntax;
using HotChocolate.Client.Core.Utilities;
using HotChocolate.Language;
using Newtonsoft.Json.Linq;

namespace HotChocolate.Client.Core.Builders
{
    public class QueryBuilder : ExpressionVisitor
    {
        const int MaxPageSize = 100;
        const string CannotSelectIQueryableListExceptionMessage = "Cannot directly select \'IQueryableList<>\'. Use ToList() to unwrap the value.";
        const string CannotSelectIQueryableValueExceptionMessage = "Cannot directly select \'IQueryableValue<>\'. Use Single() or SingleOrDefault() to unwrap the value.";

        static readonly ParameterExpression RootDataParameter = Expression.Parameter(typeof(JObject), "data");

        OperationDefinitionNode root;
        Expression rootExpression;
        SyntaxTree syntax;
        Dictionary<ParameterExpression, LambdaParameter> lambdaParameters;
        List<ISubquery> subqueries = new List<ISubquery>();
        Expression<Func<JObject, IEnumerable<JToken>>> parentIds;
        Expression<Func<JObject, JToken>> pageInfo;
        FragmentDefinitionNode currentFragment;
        Dictionary<string, LambdaExpression> fragmentExpressions = new Dictionary<string, LambdaExpression>();

        public ICompiledQuery<TResult> Build<TResult>(IQueryableValue<TResult> query)
        {
            Initialize();

            var rewritten = Visit(query.Expression);
            var lambda = Expression.Lambda<Func<JObject, TResult>>(
                rewritten.AddCast(typeof(TResult)),
                RootDataParameter);
            var master = new SimpleQuery<TResult>(root, lambda);

            if (subqueries.Count == 0)
            {
                return master;
            }
            else
            {
                return new PagedQuery<TResult>(master, subqueries);
            }
        }

        public ICompiledQuery<IEnumerable<TResult>> Build<TResult>(IQueryableList<TResult> query)
        {
            Initialize();

            var returnType = typeof(IEnumerable<TResult>);
            var rewritten = Visit(query.Expression);
            var toList = ToFinalList(rewritten, returnType);
            var lambda = Expression.Lambda<Func<JObject, IEnumerable<TResult>>>(
                toList.AddCast(returnType),
                RootDataParameter);
            var master = new SimpleQuery<IEnumerable<TResult>>(root, lambda);

            if (subqueries.Count == 0)
            {
                return master;
            }
            else
            {
                return new PagedQuery<IEnumerable<TResult>>(master, subqueries);
            }
        }

        private ISubquery BuildSubquery(
            Expression expression,
            Expression<Func<JObject, IEnumerable<JToken>>> parentIds,
            Expression<Func<JObject, IEnumerable<JToken>>> parentPageInfo)
        {
            Initialize();

            var resultType = typeof(IEnumerable<>).MakeGenericType(expression.Type.GenericTypeArguments[0]);
            var rewritten = Visit(expression);
            var toList = ToFinalList(rewritten, resultType);
            var lambda = Expression.Lambda(
                toList.AddCast(resultType),
                RootDataParameter);
            var master = SimpleSubquery<object>.Create(
                resultType,
                root,
                lambda,
                parentIds,
                pageInfo,
                parentPageInfo);

            if (subqueries.Count == 0)
            {
                return master;
            }
            else
            {
                return PagedSubquery<object>.Create(
                    resultType,
                    master,
                    subqueries,
                    parentIds,
                    pageInfo,
                    parentPageInfo);
            }
        }

        private Expression ToFinalList(Expression expression, Type type)
        {
            if (expression is MethodCallExpression m)
            {
                return m.AddCast(type).AddToList().AddCast(type);
            }
            else if (expression is SubqueryExpression s)
            {
                return Expression.Call(
                    Rewritten.List.ToSubqueryListMethod.MakeGenericMethod(type.GenericTypeArguments[0]),
                        s.MethodCall.AddCast(type),
                        CreateGetQueryContextExpression(),
                        Expression.Constant(s.Subquery))
                    .AddCast(type);
            }
            else
            {
                throw new NotSupportedException("Unable to transform final expression.");
            }
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = BookmarkAndVisit(node.Left);
            var right = BookmarkAndVisit(node.Right);
            var leftNull = IsNullConstant(node.Left);
            var rightNull = IsNullConstant(node.Right);

            if ((node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual))
            {
                // When we're comparing with null, we need to transform an equality conditional such as
                // `token["foo"] == null` to `token["foo"].Type == JTokenType.Null`
                if (!leftNull && rightNull)
                {
                    return Expression.MakeBinary(
                        node.NodeType,
                        Expression.Property(left, nameof(JToken.Type)),
                        Expression.Constant(JTokenType.Null));
                }
                else if(leftNull && !rightNull)
                {
                    return Expression.MakeBinary(
                        node.NodeType,
                        Expression.Constant(JTokenType.Null),
                        Expression.Property(right, nameof(JToken.Type)));
                }
            }

            return node.Update(
                left.AddCast(node.Left.Type),
                node.Conversion,
                right.AddCast(node.Right.Type));
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (syntax.Root == null)
            {
                var query = node.Value as IQuery;
                var mutation = node.Value as IMutation;
                var queryEntity = node.Value as IQueryableValue;

                rootExpression = node;

                if (query != null)
                {
                    root = syntax.AddRoot(OperationType.Query, null);
                    return RootDataParameter.AddIndexer("data");
                }
                else if (mutation != null)
                {
                    root = syntax.AddRoot(OperationType.Mutation, null);
                    return RootDataParameter.AddIndexer("data");
                }
                else if (queryEntity != null)
                {
                    return Visit(queryEntity.Expression);
                }
            }

            return node;
        }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            var test = Visit(node.Test);
            var ifTrue = Visit(node.IfTrue);
            var ifFalse = Visit(node.IfFalse);
            var trueNull = IsNullConstant(ifTrue);
            var falseNull = IsNullConstant(ifFalse);

            if (!trueNull)
            {
                ifTrue = ifTrue.AddCast(node.Type);
            }
            else if (!falseNull)
            {
                ifTrue = Expression.Constant(null, ifFalse.Type);
            }

            if (!falseNull)
            {
                ifFalse = ifFalse.AddCast(node.Type);
            }
            else if (!trueNull)
            {
                ifFalse = Expression.Constant(null, ifTrue.Type);
            }

            return Expression.Condition(
                test,
                ifTrue,
                ifFalse,
                !IsNullConstant(ifTrue) ? ifTrue.Type : ifFalse.Type);
        }

        protected override Expression VisitExtension(Expression node)
        {
            if (node is AliasedExpression aliased)
            {
                switch (aliased.Inner)
                {
                    case MethodCallExpression methodCall:
                        return VisitMethodCall(methodCall, aliased.Alias);
                    case MemberExpression member:
                        return VisitMember(member, aliased.Alias);
                    default:
                        return Visit(aliased.Inner);
                }
            }

            return base.VisitExtension(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var parameters = RewriteParameters(node.Parameters);
            var body = Visit(node.Body);
            return Expression.Lambda(body, node.ToString(), parameters);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return VisitMember(node, null);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            var nodeExpression = AliasedExpression.WrapIfNeeded(node.Expression, node.Member);
            var expression = BookmarkAndVisit(nodeExpression).AddCast(node.Expression.Type);
            return Expression.Bind(node.Member, expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            return VisitMethodCall(node, null);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            var newArguments = new List<Expression>();
            var index = 0;

            foreach (var arg in node.Arguments)
            {
                if (arg.Type.IsConstructedGenericType)
                {
                    var genericTypeDefinition = arg.Type.GetGenericTypeDefinition();

                    if (genericTypeDefinition == typeof(IQueryableValue<>))
                    {
                        throw new GraphQLException(CannotSelectIQueryableValueExceptionMessage);
                    }
                    else if (genericTypeDefinition == typeof(IQueryableList<>))
                    {
                        throw new GraphQLException(CannotSelectIQueryableListExceptionMessage);
                    }
                }

                using (syntax.Bookmark())
                {
                    var alias = node.Members?[index];
                    Expression rewritten;

                    if (arg is MemberExpression member)
                    {
                        rewritten = VisitMember(member, alias);
                    }
                    else if (arg is MethodCallExpression call)
                    {
                        rewritten = VisitMethodCall(call, alias);
                    }
                    else if (arg is UnaryExpression unary)
                    {
                        rewritten = VisitUnary(unary, alias);
                    }
                    else
                    {
                        rewritten = Visit(arg);
                    }

                    newArguments.Add(rewritten.AddCast(arg.Type));
                }

                ++index;
            }

            return node.Members != null ?
                Expression.New(node.Constructor, newArguments, node.Members) :
                Expression.New(node.Constructor, newArguments);
        }

        protected override Expression VisitNewArray(NewArrayExpression node)
        {
            var type = node.Type.GetElementType();
            var initializers = node.Expressions.Select(x => Visit(x).AddCast(type));
            return Expression.NewArrayInit(
                type,
                initializers);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (IsQueryableValue(node.Type))
            {
                LambdaParameter result;

                if (lambdaParameters.TryGetValue(node, out result))
                {
                    return result.Rewritten;
                }
                else
                {
                    throw new Exception(
                        "Internal Error: encountered a lambda parameter that hasn't previously been rewritten.");
                }
            }

            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            return VisitUnary(node, null);
        }

        private void Initialize()
        {
            root = null;
            syntax = new SyntaxTree();
            lambdaParameters = new Dictionary<ParameterExpression, LambdaParameter>();
        }

        private Expression VisitMember(MemberExpression node, MemberInfo alias)
        {
            if (IsQueryableValueMember(node.Member))
            {
                var expression = node.Expression;
                bool isSubqueryPager = false;

                if (expression is SubqueryPagerExpression subqueryPager)
                {
                    // This signals that a parent query has designated this method call as the
                    // paging method for a subquery. Mark it as such and get the real method call
                    // expression.
                    isSubqueryPager = true;
                    expression = subqueryPager.MethodCall;
                }

                if (expression is ParameterExpression parameterExpression)
                {
                    var parameter = (ParameterExpression)Visit(parameterExpression);
                    var parentSelection = GetSelectionSet(parameterExpression);
                    var field = syntax.AddField(parentSelection, node.Member, alias);
                    return Visit(parameterExpression).AddIndexer(field);
                }
                else
                {
                    var instance = Visit(AliasedExpression.WrapIfNeeded(expression, alias));

                    if (isSubqueryPager)
                    {
                        // This is the paging method for a subquery: add the required `pageInfo`
                        // field selections.
                        var pageInfo = PageInfoSelection();
                        syntax.Head.Selections.Add(pageInfo);

                        // And store an expression to read the `pageInfo` from the subquery.
                        var selections = syntax.SelectionStack
                            .OfType<FieldSelection>()
                            .Select(x => x.Name)
                            .Concat(new[] { "pageInfo" });
                        this.pageInfo = CreateSelectTokenExpression(selections);
                    }

                    var field = syntax.AddField(node.Member);
                    return instance.AddIndexer(field);
                }
            }
            else
            {
                var instance = Visit(AliasedExpression.WrapIfNeeded(node.Expression, alias));

                if (ExpressionWasRewritten(node.Expression, instance))
                {
                    instance = instance.AddCast(node.Expression.Type);
                }

                return node.Update(instance);
            }
        }

        private Expression VisitUnary(UnaryExpression node, MemberInfo alias)
        {
            if (node.NodeType == ExpressionType.Convert)
            {
                var rewritten = Visit(AliasedExpression.WrapIfNeeded(node.Operand, alias));
                return Expression.Convert(
                    rewritten.AddCast(node.Operand.Type),
                    node.Type);
            }

            return node.Update(Visit(node.Operand));
        }

        private Expression<Func<JObject, IEnumerable<JToken>>> CreatePageInfoExpression()
        {
            return CreateSelectTokensExpression(
                syntax.SelectionStack
                    .OfType<FieldSelection>()
                    .Select(x => x.Alias ?? x.Name).Concat(new[] { "pageInfo" }));
        }

        private static Expression<Func<JObject, JToken>> CreateSelectTokenExpression(IEnumerable<string> selectors)
        {
            var parameter = Expression.Parameter(typeof(JObject), "data");
            var path = "data." + string.Join(".", selectors);
            var expression = Expression.Call(
                parameter,
                JsonMethods.JTokenSelectToken,
                Expression.Constant(path));
            return Expression.Lambda<Func<JObject, JToken>>(expression, parameter);
        }

        private Expression<Func<JObject, IEnumerable<JToken>>> CreateSelectTokensExpression(IEnumerable<string> selectors)
        {
            var parameter = Expression.Parameter(typeof(JObject), "data");
            var path = "$.data";
            var lastWasNodes = false;

            foreach (var field in selectors)
            {
                if (lastWasNodes) path += ".[*]";
                path += '.' + field;
                lastWasNodes = field == "nodes";
            }

            var expression = Expression.Call(
                parameter,
                JsonMethods.JTokenSelectTokens,
                Expression.Constant(path));
            return Expression.Lambda<Func<JObject, IEnumerable<JToken>>>(expression, parameter);
        }

        private IEnumerable<Expression> VisitMethodArguments(MethodInfo method, ReadOnlyCollection<Expression> arguments)
        {
            var parameters = method.GetParameters();
            var index = 0;

            foreach (var arg in arguments)
            {
                var parameter = parameters[index++];
                yield return Visit(arg).AddCast(parameter.ParameterType);
            }
        }

        private Expression VisitMethodCall(MethodCallExpression node, MemberInfo alias)
        {
            if (node.Method.DeclaringType == typeof(QueryableValueExtensions))
            {
                return RewriteValueExtension(node, alias);
            }
            else if (node.Method.DeclaringType == typeof(QueryableListExtensions))
            {
                return RewriteListExtension(node, alias);
            }
            else if (node.Method.DeclaringType == typeof(QueryableInterfaceExtensions))
            {
                return RewriteInterfaceExtension(node, alias);
            }
            else if (node.Method.DeclaringType == typeof(PagingConnectionExtensions))
            {
                return RewritePagingConnectionExtensions(node);
            }
            else if (IsUnionSwitch(node.Method))
            {
                return RewriteUnionSwitch(node);
            }
            else if (IsQueryableValueMember(node.Method))
            {
                return VisitQueryMethod(node, alias);
            }
            else
            {
                try
                {
                    var methodCallExpression = node.Update(Visit(node.Object), VisitMethodArguments(node.Method, node.Arguments));
                    return methodCallExpression;
                }
                catch (NotSupportedException ex)
                {
                    throw new NotSupportedException($"{node.Method.Name}() is not supported", ex);
                }
            }
        }

        private Expression RewriteValueExtension(MethodCallExpression expression, MemberInfo alias)
        {
            if (expression.Method.GetGenericMethodDefinition() == QueryableValueExtensions.SelectMethod)
            {
                var source = expression.Arguments[0];
                var selectExpression = expression.Arguments[1];
                var lambda = selectExpression.GetLambda();
                var instance = Visit(AliasedExpression.WrapIfNeeded(source, alias));
                var select = (LambdaExpression)Visit(lambda);
                var selectMethod = select.ReturnType == typeof(JToken) ?
                    Rewritten.Value.SelectJTokenMethod :
                    Rewritten.Value.SelectMethod.MakeGenericMethod(select.ReturnType);

                return Expression.Call(
                    selectMethod,
                    instance,
                    select);
            }
            else if (expression.Method.GetGenericMethodDefinition() == QueryableValueExtensions.SelectFragmentMethod)
            {
                var source = expression.Arguments[0];

                IFragment fragment = null;

                if (expression.Arguments[1] is ConstantExpression constantExpression1)
                {
                    fragment = (IFragment)constantExpression1.Value;
                }
                else
                {
                    if (expression.Arguments[1] is MemberExpression memberExpression)
                    {
                        var memberExpressionMember = (FieldInfo) memberExpression.Member;
                        fragment = (IFragment) memberExpressionMember.GetValue(((ConstantExpression)memberExpression.Expression).Value);
                    }
                }

                if (fragment == null)
                {
                    throw new InvalidOperationException("Fragment instance cannot be found");
                }

                var instance = Visit(AliasedExpression.WrapIfNeeded(source, alias));
                var select = VisitFragment(fragment);
                syntax.AddFragmentSpread(fragment.Name);

                return Expression.Call(
                    Rewritten.Value.SelectFragmentMethod.MakeGenericMethod(fragment.ReturnType),
                    instance,
                    select);
            }
            else if (expression.Method.GetGenericMethodDefinition() == QueryableValueExtensions.SelectListMethod)
            {
                var source = expression.Arguments[0];
                var selectExpression = expression.Arguments[1];
                var lambda = selectExpression.GetLambda();
                var instance = Visit(source);
                var select = (LambdaExpression)Visit(lambda);
                var itemType = select.ReturnType == typeof(JToken) ?
                    select.ReturnType :
                    GetEnumerableItemType(select.ReturnType);

                return Expression.Call(
                    Rewritten.Value.SelectListMethod.MakeGenericMethod(itemType),
                    instance,
                    select);
            }
            else if (expression.Method.GetGenericMethodDefinition() == QueryableValueExtensions.SingleMethod)
            {
                var source = expression.Arguments[0];
                var instance = Visit(AliasedExpression.WrapIfNeeded(source, alias));

                return Expression.Call(
                    Rewritten.Value.SingleMethod.MakeGenericMethod(instance.Type),
                    instance);
            }
            else if (expression.Method.GetGenericMethodDefinition() == QueryableValueExtensions.SingleOrDefaultMethod)
            {
                var source = expression.Arguments[0];
                var instance = Visit(AliasedExpression.WrapIfNeeded(source, alias));

                return Expression.Call(
                    Rewritten.Value.SingleOrDefaultMethod.MakeGenericMethod(instance.Type),
                    instance);
            }
            else
            {
                throw new NotSupportedException($"{expression.Method.Name}() is not supported");
            }
        }

        private LambdaExpression VisitFragment(IFragment fragment)
        {
            LambdaExpression lambda;
            if (!syntax.Root.FragmentDefinitions.ContainsKey(fragment.Name))
            {
                currentFragment = syntax.AddFragment(fragment);
                using (syntax.Bookmark())
                {
                    var fragmentExpressionLambda = Visit(fragment.Expression).GetLambda();
                    var castedFragmentExpression = fragmentExpressionLambda.Body.AddCast(fragment.ReturnType);
                    lambda = Expression.Lambda(castedFragmentExpression, fragmentExpressionLambda.Parameters);
                }

                currentFragment = null;
                fragmentExpressions.Add(fragment.Name, lambda);
            }
            else
            {
                lambda = fragmentExpressions[fragment.Name];
            }

            return lambda;
        }

        private Expression RewriteListExtension(MethodCallExpression expression, MemberInfo alias)
        {
            if (expression.Method.GetGenericMethodDefinition() == QueryableListExtensions.SelectMethod)
            {
                var source = expression.Arguments[0];
                var selectExpression = expression.Arguments[1];
                var lambda = selectExpression.GetLambda();
                var instance = Visit(AliasedExpression.WrapIfNeeded(source, alias));
                ISubquery subquery = null;

                if (instance is AllPagesExpression allPages)
                {
                    // .AllPages() was called on the instance. The expression is just a marker for
                    // this, the actual instance is in `allPages.Instance`
                    instance = Visit(AliasedExpression.WrapIfNeeded(allPages.Method, alias));

                    // Select the "id" fields for the subquery.
                    var parentSelection = syntax.SelectionStack.Take(syntax.SelectionStack.Count - 1);
                    var idSelection = AddIdSelection(parentSelection.Last());
                    parentIds = CreateSelectTokensExpression(
                        parentSelection.OfType<FieldSelection>().Select(x => x.Name).Concat(new[]
                        {
                            idSelection.Alias ?? idSelection.Name
                        }));

                    var pageSize = allPages.PageSize ?? MaxPageSize;

                    // Add a "first: pageSize" argument to the query field.
                    syntax.AddArgument("first", pageSize);

                    // Add the required "pageInfo" field selections then select "nodes".
                    syntax.Head.Selections.Add(PageInfoSelection());

                    // Create the subquery
                    subquery = AddSubquery(allPages.Method, expression, instance.AddIndexer("pageInfo"), pageSize);

                    // And continue the query as normal after selecting "nodes".
                    syntax.AddField("nodes");
                    instance = instance.AddIndexer("nodes");
                }

                var select = (LambdaExpression)Visit(lambda);
                var rewrittenSelect = Expression.Call(
                    Rewritten.List.SelectMethod.MakeGenericMethod(select.ReturnType),
                    instance,
                    select);

                // If the expression was an .AllPages() call then return a SubqueryExpression with
                // the related SubQuery to the .ToList() or .ToDictionary() method that follows.
                return subquery == null ?
                    (Expression)rewrittenSelect :
                    new SubqueryExpression(subquery, rewrittenSelect);
            }
            else if (expression.Method.GetGenericMethodDefinition() == QueryableListExtensions.SelectFragmentMethod)
            {
                var source = expression.Arguments[0];

                IFragment fragment = null;

                if (expression.Arguments[1] is ConstantExpression constantExpression1)
                {
                    fragment = (IFragment)constantExpression1.Value;
                }
                else
                {
                    if (expression.Arguments[1] is MemberExpression memberExpression)
                    {
                        var memberExpressionMember = (FieldInfo)memberExpression.Member;
                        fragment = (IFragment)memberExpressionMember.GetValue(((ConstantExpression)memberExpression.Expression)
                            .Value);
                    }
                }

                if (fragment == null)
                {
                    throw new InvalidOperationException("Fragment instance cannot be found");
                }

                var instance = Visit(AliasedExpression.WrapIfNeeded(source, alias));

                ISubquery subquery = null;
                if (instance is AllPagesExpression allPages)
                {
                    // .AllPages() was called on the instance. The expression is just a marker for
                    // this, the actual instance is in `allPages.Instance`
                    instance = Visit(AliasedExpression.WrapIfNeeded(allPages.Method, alias));

                    // Select the "id" fields for the subquery.
                    var parentSelection = syntax.SelectionStack.Take(syntax.SelectionStack.Count - 1);
                    var idSelection = AddIdSelection(parentSelection.Last());
                    parentIds = CreateSelectTokensExpression(
                        parentSelection.OfType<FieldSelection>().Select(x => x.Name).Concat(new[]
                        {
                            idSelection.Alias ?? idSelection.Name
                        }));

                    var pageSize = allPages.PageSize ?? MaxPageSize;

                    // Add a "first: pageSize" argument to the query field.
                    syntax.AddArgument("first", pageSize);

                    // Add the required "pageInfo" field selections then select "nodes".
                    syntax.Head.Selections.Add(PageInfoSelection());

                    // Create the subquery
                    subquery = AddSubquery(allPages.Method, expression, instance.AddIndexer("pageInfo"), pageSize);

                    // And continue the query as normal after selecting "nodes".
                    syntax.AddField("nodes");
                    instance = instance.AddIndexer("nodes");
                }

                var @select = VisitFragment(fragment);
                syntax.AddFragmentSpread(fragment.Name);

                var rewrittenSelect = Expression.Call(
                    Rewritten.List.SelectMethod.MakeGenericMethod(@select.ReturnType),
                    instance,
                    @select);

                // If the expression was an .AllPages() call then return a SubqueryExpression with
                // the related SubQuery to the .ToList() or .ToDictionary() method that follows.
                return subquery == null ?
                    (Expression)rewrittenSelect :
                    new SubqueryExpression(subquery, rewrittenSelect);
            }
            else if (expression.Method.GetGenericMethodDefinition() == QueryableListExtensions.ToDictionaryMethod)
            {
                var source = expression.Arguments[0];
                var instance = Visit(AliasedExpression.WrapIfNeeded(source, alias));
                var keySelect = expression.Arguments[1].GetLambda();
                var valueSelect = expression.Arguments[2].GetLambda();
                var inputType = GetEnumerableItemType(instance.Type);

                if (inputType == typeof(JToken))
                {
                    throw new NotImplementedException();
                }

                if (instance is SubqueryExpression subquery)
                {
                    instance = subquery.MethodCall;

                    return Expression.Call(
                        Rewritten.List.ToSubqueryDictionaryMethod.MakeGenericMethod(
                            inputType,
                            keySelect.ReturnType,
                            valueSelect.ReturnType),
                        instance,
                        CreateGetQueryContextExpression(),
                        Expression.Constant(subquery.Subquery),
                        keySelect,
                        valueSelect);
                }
                else
                {
                    return Expression.Call(
                        LinqMethods.ToDictionaryMethod.MakeGenericMethod(
                            inputType,
                            keySelect.ReturnType,
                            valueSelect.ReturnType),
                        instance,
                        keySelect,
                        valueSelect);
                }
            }
            else if (expression.Method.GetGenericMethodDefinition() == QueryableListExtensions.ToListMethod)
            {
                var source = expression.Arguments[0];
                var instance = Visit(AliasedExpression.WrapIfNeeded(source, alias));
                var inputType = GetEnumerableItemType(instance.Type);
                var resultType = GetQueryableListItemType(source.Type);

                if (instance is SubqueryExpression subquery)
                {
                    instance = subquery.MethodCall;

                    if (inputType == typeof(JToken))
                    {
                        instance = Expression.Call(
                            Rewritten.List.ToListMethod.MakeGenericMethod(resultType),
                            instance);
                    }

                    return  Expression.Call(
                        Rewritten.List.ToSubqueryListMethod.MakeGenericMethod(resultType),
                        instance,
                        CreateGetQueryContextExpression(),
                        Expression.Constant(subquery.Subquery));
                }
                else
                {
                    if (inputType == typeof(JToken))
                    {
                        return Expression.Call(
                            Rewritten.List.ToListMethod.MakeGenericMethod(resultType),
                            instance);
                    }
                    else
                    {
                        return Expression.Call(
                            LinqMethods.ToListMethod.MakeGenericMethod(resultType),
                            instance);
                    }
                }
            }
            else if (expression.Method.GetGenericMethodDefinition() == QueryableListExtensions.OfTypeMethod)
            {
                var source = expression.Arguments[0];
                var instance = Visit(source);
                var resultType = GetQueryableListItemType(source.Type);
                var fragment = syntax.AddInlineFragment(expression.Method.GetGenericArguments()[0], true);

                return Expression.Call(
                    Rewritten.List.OfTypeMethod,
                    instance,
                    Expression.Constant(fragment.TypeCondition));
            }
            else
            {
                throw new NotSupportedException($"{expression.Method.Name}() is not supported");
            }
        }

        private Expression RewriteInterfaceExtension(MethodCallExpression expression, MemberInfo alias)
        {
            if (expression.Method.GetGenericMethodDefinition() == QueryableInterfaceExtensions.CastMethod)
            {
                var source = expression.Arguments[0];
                var instance = Visit(source);
                var targetType = expression.Method.GetGenericArguments()[0];
                var fragment = syntax.AddInlineFragment(targetType, true);

                return Expression.Call(
                    Rewritten.Interface.CastMethod,
                    instance,
                    Expression.Constant(fragment.TypeCondition));
            }
            else
            {
                throw new NotSupportedException($"{expression.Method.Name}() is not supported");
            }
        }

        private Expression RewritePagingConnectionExtensions(MethodCallExpression expression)
        {
            if (expression.Method.GetGenericMethodDefinition() == PagingConnectionExtensions.AllPagesMethod)
            {
                // .AllPages() was called on a IPagingConnection. We can't handle this yet -
                // return an AllPagesExpression so we know to handle it when the containing Select
                // is visited.
                return new AllPagesExpression((MethodCallExpression)expression.Arguments[0]);
            }
            else if (expression.Method.GetGenericMethodDefinition() == PagingConnectionExtensions.AllPagesCustomSizeMethod)
            {
                int? allPagesValue = null;
                if(expression.Arguments[1] is ConstantExpression constantExpression)
                {
                    allPagesValue = (int) constantExpression.Value;
                }
                else if (expression.Arguments[1] is MemberExpression memberExpression)
                {
                    if (memberExpression.Expression is ConstantExpression memberConstantExpression)
                    {
                        var memberExpressionMember = (FieldInfo)memberExpression.Member;
                        allPagesValue = (int) memberExpressionMember.GetValue(memberConstantExpression.Value);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }

                return new AllPagesExpression((MethodCallExpression)expression.Arguments[0], allPagesValue);
            }
            else
            {
                throw new NotSupportedException($"{expression.Method.Name}() is not supported");
            }
        }

        private Expression RewriteUnionSwitch(MethodCallExpression expression)
        {
            LambdaExpression CastInitializer(Expression initializer, Type type)
            {
                var lambda = initializer.GetLambda();
                var bodyType = lambda.Body.Type;

                if (bodyType == typeof(JToken))
                {
                    return Expression.Lambda(
                        lambda.Body.AddCast(type),
                        lambda.Parameters);
                }
                else
                {
                    return lambda;
                }
            }

            var source = expression.Object;
            var instance = Visit(source);
            var resultType = expression.Method.GetGenericArguments()[0];

            if (!(expression.Arguments[0].GetLambda().Body is MethodCallExpression casesBody))
            {
                throw new GraphQLException("Expected union switch expression.");
            }

            var cases = new Dictionary<string, Expression>();
            BuildUnionSwitchCases(casesBody, cases);

            var funcType = typeof(Func<,>).MakeGenericType(typeof(JToken), resultType);
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), funcType);
            var dictionaryAdd = dictionaryType.GetTypeInfo().GetDeclaredMethod("Add");
            var initializers = cases.Select(x =>
                Expression.ElementInit(
                    dictionaryAdd,
                    Expression.Constant(x.Key),
                    CastInitializer(x.Value, resultType))).ToList();
            var newDictionary = Expression.ListInit(
                Expression.New(dictionaryType),
                initializers);

            return Expression.Call(
                Rewritten.Value.SwitchMethod.MakeGenericMethod(resultType),
                instance,
                newDictionary);
            throw new NotImplementedException();
        }

        private void BuildUnionSwitchCases(MethodCallExpression body, Dictionary<string, Expression> result)
        {
            if (body.Object is MethodCallExpression previous)
            {
                BuildUnionSwitchCases(previous, result);
            }

            using (syntax.Bookmark())
            {
                var methodParam = body.Method.GetParameters()[0];
                var type = methodParam.ParameterType.GenericTypeArguments[0];
                syntax.AddInlineFragment(type, true);

                var selector = Visit(body.Arguments[0]);
                result.Add(type.Name, selector);
            }
        }

        private Expression VisitQueryMethod(MethodCallExpression node, MemberInfo alias)
        {
            var queryEntity = (node.Object as ConstantExpression)?.Value as IQueryableValue;
            var instance = Visit(queryEntity?.Expression ?? node.Object);
            var field = syntax.AddField(node.Method, alias);

            VisitQueryMethodArguments(node.Method.GetParameters(), node.Arguments);

            return instance.AddIndexer(field);
        }

        private void VisitQueryMethodArguments(ParameterInfo[] parameters, ReadOnlyCollection<Expression> arguments)
        {
            for (var i = 0; i < parameters.Length; ++i)
            {
                var parameter = parameters[i];
                var value = EvaluateValue(arguments[i]);

                if (value is IArg arg)
                {
                    if (arg.VariableName == null)
                    {
                        value = arg.Value;
                    }
                    else
                    {
                        value = syntax.AddVariableDefinition(arg.Type, arg.IsNullableVariable, arg.VariableName);
                    }
                }

                if (value != null)
                {
                    syntax.AddArgument(parameter.Name, value);
                }
            }
        }

        private FieldSelection AddIdSelection(ISelectionSet set)
        {
            var result = set.Selections.OfType<FieldSelection>().FirstOrDefault(x => x.Name == "id");

            if (result == null)
            {
                result = new FieldSelection("id", null);
                set.Selections.Insert(0, result);
            }

            return result;
        }

        private ISubquery AddSubquery(
            MethodCallExpression expression,
            MethodCallExpression selector,
            Expression pageInfoSelector,
            int pageSize)
        {
            // Create a lambda that selects the "pageInfo" fields.
            var parentPageInfo = CreatePageInfoExpression();

            // Create the actual subquery.
            var nodeQuery = CreateNodeQuery(expression, selector, pageSize);
            var subqueryBuilder = new QueryBuilder();
            var subquery = subqueryBuilder.BuildSubquery(nodeQuery, parentIds, parentPageInfo);
            subqueries.Add(subquery);
            return subquery;
        }


        private MethodCallExpression CreateGetQueryContextExpression()
        {
            return Expression.Call(
                RootDataParameter,
                JsonMethods.JTokenAnnotation.MakeGenericMethod(typeof(ISubqueryRunner)));
        }

        private Expression CreateNodeQuery(
            MethodCallExpression expression,
            MethodCallExpression selector,
            int pageSize)
        {
            // Given an expression such as:
            //
            // new Query()
            //   .Repository("foo", "bar")
            //   .Issues()
            //   .AllPages()
            //   .Select(x => x.Name);
            //
            // This method creates a subquery expression that reads:
            //
            // new Query()
            //   .Node(Var("__id"))
            //   .Cast<Repository>
            //   .Issues(first: 100, after: Var("__after"))
            //   .Select(x => x.Name);
            //
            // The passed in `expression` parameter is the part before `AllPages()` and the
            // `selector` parameter is the `x => x.Name` selector.

            // Get the `Repository` type in the example above.
            var nodeType = expression.Object.Type;

            // First create the expression `new Query().Node(Var("__id"))`
            Expression rewritten = Expression.Call(
                rootExpression,
                "Node",
                null,
                Expression.Constant(new Arg<ID>("__id", false)));

            // Add `.Cast<nodeType>`.
            rewritten = Expression.Call(
                QueryableInterfaceExtensions.CastMethod.MakeGenericMethod(nodeType),
                rewritten);

            // Rewrite the method to add the `first: pageSize` and `after: Var("__after")`
            // parameters, and make it be called on `rewritten`.
            var methodCall = RewritePagingMethodCall(expression, rewritten, pageSize);

            // Wrap this in a SubqueryPagerExpression to instruct the child query builder to
            // add the paging infrastructure.
            rewritten = new SubqueryPagerExpression(methodCall);

            // Add "nodes"
            rewritten = Expression.Property(rewritten, "Nodes");

            // And now add in the selector.
            return selector.Update(
                null,
                new[] { rewritten, selector.Arguments[1] });
        }

        MethodCallExpression RewritePagingMethodCall(
            MethodCallExpression methodCall,
            Expression instance,
            int pageSize)
        {
            var arguments = new List<Expression>();
            var i = 0;

            foreach (var parameter in methodCall.Method.GetParameters())
            {
                switch (parameter.Name)
                {
                    case "first":
                        arguments.Add(Expression.Constant(new Arg<int>(pageSize), parameter.ParameterType));
                        break;
                    case "after":
                        arguments.Add(Expression.Constant(new Arg<string>("__after", true), parameter.ParameterType));
                        break;
                    default:
                        arguments.Add(methodCall.Arguments[i]);
                        break;
                }

                ++i;
            }

            return methodCall.Update(
                instance,
                arguments);
        }

        private object EvaluateValue(Expression expression)
        {
            if (expression is ConstantExpression c)
            {
                return c.Value;
            }
            else if (expression is LambdaExpression l)
            {
                var compiled = l.Compile();
                return compiled.DynamicInvoke();
            }
            else
            {
                var lambda = Expression.Lambda(expression);
                var compiled = lambda.Compile();
                return compiled.DynamicInvoke();
            }
        }

        private IEnumerable<ParameterExpression> RewriteParameters(IEnumerable<ParameterExpression> parameters)
        {
            var result = new List<ParameterExpression>();

            foreach (var parameter in parameters)
            {
                if (IsQueryableValue(parameter.Type))
                {
                    var rewritten = Expression.Parameter(typeof(JToken), parameter.Name);
                    var p = new LambdaParameter(
                        parameter,
                        rewritten,
                        currentFragment ?? syntax.Head);
                    lambdaParameters.Add(parameter, p);
                    result.Add(rewritten);
                }
                else
                {
                    result.Add(parameter);
                }
            }

            return result;
        }

        private Expression BookmarkAndVisit(Expression left)
        {
            using (syntax.Bookmark())
            {
                return Visit(left);
            }
        }

        private static Type GetEnumerableItemType(Type type)
        {
            var ti = type.GetTypeInfo();

            if (ti.IsGenericType && ti.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return ti.GenericTypeArguments[0];
            }
            else
            {
                throw new NotSupportedException("Not an IEnumerable<>.");
            }
        }

        private static Expression GetObject(Expression expression)
        {
            if (expression is MemberExpression member)
            {
                return member.Expression;
            }
            else if (expression is MethodCallExpression method)
            {
                return method.Object;
            }
            else
            {
                throw new NotImplementedException(
                    "Don't know how to get the object expression from " + expression);
            }
        }

        private static Type GetQueryableListItemType(Type type)
        {
            var ti = type.GetTypeInfo();

            if (ti.GetGenericTypeDefinition() == typeof(IQueryableList<>))
            {
                return ti.GenericTypeArguments[0];
            }
            else
            {
                throw new NotSupportedException("Not an IQueryableList<>.");
            }
        }

        private ISelectionSet GetSelectionSet(ParameterExpression parameter)
        {
            return lambdaParameters[parameter].SelectionSet;
        }

        private static bool ExpressionWasRewritten(Expression oldExpression, Expression newExpression)
        {
            return newExpression.Type == typeof(JToken) && oldExpression.Type != typeof(JToken);
        }

        private static bool IsNullConstant(Expression expression)
        {
            if (expression is ConstantExpression c)
            {
                return c.Value == null;
            }

            return false;
        }

        private static bool IsQueryableValue(Type type)
        {
            return typeof(IQueryableValue).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }

        private static bool IsQueryableValueMember(Expression expression)
        {
            var memberExpression = expression as MemberExpression;
            return memberExpression != null && IsQueryableValueMember(memberExpression.Member);
        }

        private static bool IsQueryableValueMember(MemberInfo member)
        {
            return IsQueryableValue(member.DeclaringType);
        }

        private static bool IsUnion(Type type)
        {
            return typeof(IUnion).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo());
        }

        private static bool IsUnionSwitch(MemberInfo member)
        {
            return IsUnion(member.DeclaringType) &&
                member is MethodInfo m &&
                m.Name == "Switch";
        }

        private static FieldSelection PageInfoSelection()
        {
            var result = new FieldSelection("pageInfo", null);
            result.Selections.Add(new FieldSelection("hasNextPage", null));
            result.Selections.Add(new FieldSelection("endCursor", null));
            return result;
        }

        private class LambdaParameter
        {
            public LambdaParameter(
                ParameterExpression original,
                ParameterExpression rewritten,
                ISelectionSet selectionSet)
            {
                Original = original;
                Rewritten = rewritten;
                SelectionSet = selectionSet;
            }

            public ParameterExpression Original { get; }
            public ParameterExpression Rewritten { get; }
            public ISelectionSet SelectionSet { get; }
        }
    }
}
