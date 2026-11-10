using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Esql.Functions;

namespace HotChocolate.Data.ElasticSearch.Filtering;

/// <summary>
/// Rewrites the predicates that ES|QL answers differently from .NET, or cannot answer from a column.
/// </summary>
internal sealed class ElasticSearchFilterRewriter(JsonNamingPolicy? namingPolicy) : ExpressionVisitor
{
    private static readonly MethodInfo s_kql =
        typeof(EsqlFunctions).GetMethod(nameof(EsqlFunctions.Kql), [typeof(string)])!;

    private static readonly MethodInfo s_qstr =
        typeof(EsqlFunctions).GetMethod(nameof(EsqlFunctions.Qstr), [typeof(string)])!;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (TrySplitNullMembership(node, out var split))
        {
            return Visit(split);
        }

        if (TryRewriteEnumMembership(node, out var membership))
        {
            return membership;
        }

        if (node.Method.DeclaringType != typeof(Enumerable)
            || node.Method.Name is not (nameof(Enumerable.Any) or nameof(Enumerable.All))
            || node.Arguments[0] is not MemberExpression list)
        {
            return base.VisitMethodCall(node);
        }

        if (IsStringList(list.Type))
        {
            return node.Arguments.Count == 2
                ? RewriteStringList(node, list) ?? base.VisitMethodCall(node)
                : base.VisitMethodCall(node);
        }

        return TryGetObjectElement(list.Type, out _)
            ? RewriteObjectList(node, list)
            : base.VisitMethodCall(node);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.NotEqual
            && node.Left is MemberExpression list
            && TryGetObjectElement(list.Type, out _)
            && IsNull(node.Right))
        {
            return Expression.Constant(true);
        }

        if (node.NodeType == ExpressionType.AndAlso)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);

            if (left is ConstantExpression { Value: true })
            {
                return right;
            }

            return right is ConstantExpression { Value: true } ? left : Expression.AndAlso(left, right);
        }

        if (node.NodeType == ExpressionType.Equal && IsNullValue(node.Right))
        {
            return WithOwners(base.VisitBinary(node), node.Left);
        }

        if (node.NodeType == ExpressionType.NotEqual
            && !IsNullValue(node.Right)
            && TryGetNullableField(node.Left, out var field))
        {
            return OrIsNull(field, base.VisitBinary(node));
        }

        return base.VisitBinary(node);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType != ExpressionType.Not)
        {
            return base.VisitUnary(node);
        }

        var test = TryGetTestedNullableField(node.Operand, out var field)
            ? OrIsNull(field, base.VisitUnary(node))
            : base.VisitUnary(node);
        return WithOwners(test, node.Operand);
    }

    // a test that holds for a missing field of an object holds in .NET only when the object exists.
    private Expression WithOwners(Expression test, Expression scope)
        => new OwnerFinder().Find(scope)
            .Select(FieldPath)
            .Distinct()
            .Aggregate(
                test,
                (result, owner) => Expression.AndAlso(QueryString($"_exists_:{QueryStringName(owner)}"), result));

    private static bool TryGetTestedNullableField(Expression test, out MemberExpression field)
    {
        switch (test)
        {
            case BinaryExpression
            {
                NodeType: ExpressionType.Equal or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual
                    or ExpressionType.LessThan or ExpressionType.LessThanOrEqual
            } comparison when !IsNullValue(comparison.Right):
                return TryGetNullableField(comparison.Left, out field);

            case MethodCallExpression { Method.Name: nameof(Enumerable.Contains) } contains
                when TryGetMembership(contains, out var values, out var target) && !HasNull(values):
                return TryGetNullableField(target, out field);

            default:
                field = null!;
                return false;
        }
    }

    private static bool TryGetNullableField(Expression expression, out MemberExpression field)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert } convert)
        {
            expression = convert.Operand;
        }

        field = (expression as MemberExpression)!;

        return field is not null
            && IsDocumentField(field.Expression)
            && (Nullable.GetUnderlyingType(field.Type) is not null || !field.Type.IsValueType);
    }

    private static bool IsDocumentField(Expression? owner)
        => owner is ParameterExpression || (owner is MemberExpression member && IsDocumentField(member.Expression));

    private static bool IsNullValue(Expression expression)
        => IsNull(expression)
            || (expression is MemberExpression { Expression: ConstantExpression } && Evaluate(expression) is null);

    private static bool TryGetMembership(MethodCallExpression node, out Expression values, out Expression target)
    {
        (values, target) = node switch
        {
            { Method.Name: nameof(Enumerable.Contains), Object: null, Arguments.Count: 2 }
                => (node.Arguments[0], node.Arguments[1]),
            { Method.Name: nameof(Enumerable.Contains), Object: { } list, Arguments.Count: 1 }
                when list.Type != typeof(string) => (list, node.Arguments[0]),
            _ => (null!, null!)
        };

        return values is not null;
    }

    // only a list given by the request is read, not a field of the document.
    private static bool HasNull(Expression values)
        => values is ConstantExpression or MemberExpression { Expression: ConstantExpression }
            && ((IEnumerable?)Evaluate(values))?.Cast<object?>().Any(value => value is null) == true;

    // ES|QL never matches null in an IN list, so a null in the list becomes a test for a missing value.
    private static bool TrySplitNullMembership(MethodCallExpression node, out Expression split)
    {
        split = node;

        if (!TryGetMembership(node, out var values, out var target) || !HasNull(values))
        {
            return false;
        }

        var rest = ((IEnumerable)Evaluate(values)!).Cast<object?>().Where(value => value is not null).ToArray();
        var array = Array.CreateInstance(target.Type, rest.Length);
        Array.Copy(rest, array, rest.Length);

        Expression contains = Expression.Call(
            typeof(Enumerable),
            nameof(Enumerable.Contains),
            [target.Type],
            Expression.Constant(array),
            target);

        // a value of a list is never null, since Elasticsearch does not index a null in a list.
        if (target is ParameterExpression)
        {
            split = contains;
            return true;
        }

        Expression isNull = Expression.Equal(target, Expression.Constant(null, target.Type));
        split = rest.Length == 0 ? isNull : Expression.OrElse(isNull, contains);
        return true;
    }

    // ES|QL answers null for a comparison with a null value, where .NET answers true for its negation.
    // A field of an object is null in .NET only when the object exists, which ES|QL tells with _exists_.
    private BinaryExpression OrIsNull(MemberExpression field, Expression test)
    {
        Expression isNull = Expression.Equal(field, Expression.Constant(null, field.Type));

        if (field.Expression is MemberExpression owner)
        {
            isNull = Expression.AndAlso(isNull, QueryString($"_exists_:{QueryStringName(FieldPath(owner))}"));
        }

        return Expression.OrElse(isNull, test);
    }

    // A field stored through a converter is compared by name, which the IN list of ES|QL does not do for an enum.
    private static bool TryRewriteEnumMembership(MethodCallExpression node, out Expression membership)
    {
        membership = node;

        if (node.Method.Name != nameof(Enumerable.Contains))
        {
            return false;
        }

        var (values, target) = node.Object is null && node.Arguments.Count == 2
            ? (node.Arguments[0], node.Arguments[1])
            : node.Object is not null && node.Arguments.Count == 1
                ? (node.Object, node.Arguments[0])
                : (null, null);

        while (target is UnaryExpression { NodeType: ExpressionType.Convert } convert)
        {
            target = convert.Operand;
        }

        if (values is null
            || target is not MemberExpression field
            || (Nullable.GetUnderlyingType(field.Type) ?? field.Type) is not { IsEnum: true })
        {
            return false;
        }

        membership = ((IEnumerable)Evaluate(values)!).Cast<object?>()
            .Select(Expression (value) => Expression.Equal(field, Expression.Constant(value, field.Type)))
            .DefaultIfEmpty(Expression.Constant(false))
            .Aggregate(Expression.OrElse);
        return true;
    }

    private Expression RewriteObjectList(MethodCallExpression node, MemberExpression list)
    {
        var path = FieldPath(list);
        var nested = list.Member.IsDefined(typeof(ElasticSearchNestedAttribute), true);
        var kqlPath = KqlName(path);

        if (node.Arguments.Count == 1)
        {
            // a nested element exists even when all its fields are null; an object mapping keeps no trace of it.
            return nested ? Kql($"{kqlPath}:{{ * }}") : QueryString($"_exists_:{QueryStringName(path)}");
        }

        var lambda = GetLambda(node.Arguments[1]);
        var predicate = new ObjectPredicate(this, lambda.Parameters[0], nested ? null : QueryStringName(path) + ".");
        var text = predicate.Render(lambda.Body);
        var any = node.Method.Name == nameof(Enumerable.Any);

        if (nested)
        {
            return any ? Kql($"{kqlPath}:{{ {text} }}") : Expression.Not(Kql($"{kqlPath}:{{ not ({text}) }}"));
        }

        if (!any || predicate.Fields.Count > 1 || predicate.Negated || predicate.Conjunction)
        {
            throw ThrowHelper.ListFilter_RequiresNested(path);
        }

        return QueryString(text);
    }

    private Expression? RewriteStringList(MethodCallExpression node, MemberExpression list)
    {
        var lambda = GetLambda(node.Arguments[1]);
        var item = lambda.Parameters[0];
        var body = StripGuard(lambda.Body, item);
        var any = node.Method.Name == nameof(Enumerable.Any);
        var negated = body is UnaryExpression { NodeType: ExpressionType.Not };
        var pattern = negated ? ((UnaryExpression)body).Operand : body;

        // the remaining forms are answered on the field by the ES|QL translation.
        if (pattern is not MethodCallExpression call || (any && !negated))
        {
            return null;
        }

        var regex = StringRegex(call, item);
        var field = QueryStringName(FieldPath(list));
        return any
            ? QueryString($"{field}:/~({regex})/")
            : Expression.Not(QueryString(negated ? $"{field}:/{regex}/" : $"{field}:/~({regex})/"));
    }

    private static string StringRegex(MethodCallExpression call, ParameterExpression item)
    {
        if (call.Object == item)
        {
            var text = EscapeRegex((string)Evaluate(call.Arguments[0])!);
            return call.Method.Name switch
            {
                nameof(string.StartsWith) => text + ".*",
                nameof(string.EndsWith) => ".*" + text,
                nameof(string.Contains) => ".*" + text + ".*",
                _ => throw ThrowHelper.ListFilter_NotSupported(call.ToString())
            };
        }

        if (call.Method.Name == nameof(Enumerable.Contains)
            && (call.Object is null ? call.Arguments[1] : call.Arguments[0]) == item)
        {
            // Elasticsearch does not index a null in a list, so a null choice matches no value.
            var values = ((IEnumerable)Evaluate(call.Object ?? call.Arguments[0])!).OfType<string>().ToArray();

            // # is the empty language of Lucene, so an empty list matches no value.
            return values.Length == 0 ? "#" : "(" + string.Join("|", values.Select(EscapeRegex)) + ")";
        }

        throw ThrowHelper.ListFilter_NotSupported(call.ToString());
    }

    private static Expression StripGuard(Expression expression, ParameterExpression item)
        => expression switch
        {
            BinaryExpression
            {
                NodeType: ExpressionType.AndAlso,
                Left: BinaryExpression { NodeType: ExpressionType.NotEqual } guard
            } b when guard.Left == item && IsNull(guard.Right) => StripGuard(b.Right, item),
            BinaryExpression
            {
                NodeType: ExpressionType.OrElse,
                Left: BinaryExpression { NodeType: ExpressionType.Equal } guard
            } b when guard.Left == item && IsNull(guard.Right) => StripGuard(b.Right, item),
            UnaryExpression { NodeType: ExpressionType.Not } u => Expression.Not(StripGuard(u.Operand, item)),
            _ => expression
        };

    private static string EscapeRegex(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var c in value)
        {
            if ("\\.?+*|{}[]()\"#@&<>~^$/-".Contains(c))
            {
                builder.Append('\\');
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    private string FieldName(MemberInfo member)
        => member.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
            ?? namingPolicy?.ConvertName(member.Name)
            ?? member.Name;

    private string FieldPath(MemberExpression member)
        => member.Expression is MemberExpression parent
            ? FieldPath(parent) + "." + FieldName(member.Member)
            : FieldName(member.Member);

    // the objects that hold the fields of the document read by an expression, outside the lambdas of lists.
    private sealed class OwnerFinder : ExpressionVisitor
    {
        private readonly List<MemberExpression> _owners = [];

        public List<MemberExpression> Find(Expression expression)
        {
            Visit(expression);
            return _owners;
        }

        protected override Expression VisitLambda<T>(Expression<T> node) => node;

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is MemberExpression owner && IsDocumentField(owner))
            {
                _owners.Add(owner);
                return node;
            }

            return base.VisitMember(node);
        }
    }

    // a name with a reserved character is quoted in KQL and escaped in a query string.
    private static string KqlName(string name)
        => name.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.')
            ? name
            : "\"" + name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static string QueryStringName(string name)
    {
        var builder = new StringBuilder(name.Length);

        foreach (var c in name)
        {
            if (char.IsWhiteSpace(c) || "+-=&|><!(){}[]^\"~*?:\\/".Contains(c))
            {
                builder.Append('\\');
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    // the fields that tell that an object exists, since KQL applies no wildcard to a quoted name.
    private IEnumerable<string> Leaves(Type type, string path, HashSet<Type> seen)
    {
        foreach (var property in type.GetProperties())
        {
            var name = path + "." + FieldName(property);
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (propertyType == typeof(string)
                || propertyType.IsValueType
                || typeof(IEnumerable).IsAssignableFrom(propertyType))
            {
                yield return name;
            }
            else if (seen.Add(propertyType))
            {
                foreach (var leaf in Leaves(propertyType, name, seen))
                {
                    yield return leaf;
                }
            }
        }
    }

    private static bool IsStringList(Type type)
        => type != typeof(string) && typeof(IEnumerable<string>).IsAssignableFrom(type);

    private static bool TryGetObjectElement(Type type, out Type element)
    {
        element = typeof(void);

        if (type == typeof(string))
        {
            return false;
        }

        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerable is null)
        {
            return false;
        }

        element = enumerable.GetGenericArguments()[0];
        return element.IsClass && element != typeof(string);
    }

    private static LambdaExpression GetLambda(Expression expression)
        => (LambdaExpression)(expression is UnaryExpression { NodeType: ExpressionType.Quote } quote
            ? quote.Operand
            : expression);

    private static bool IsNull(Expression expression)
        => expression is ConstantExpression { Value: null }
            or UnaryExpression { NodeType: ExpressionType.Convert, Operand: ConstantExpression { Value: null } };

    private static object? Evaluate(Expression expression)
        => Expression.Lambda(expression).Compile().DynamicInvoke();

    private static MethodCallExpression Kql(string query)
        => Expression.Call(s_kql, Expression.Constant(query));

    private static MethodCallExpression QueryString(string query)
        => Expression.Call(s_qstr, Expression.Constant(query));

    /// <summary>
    /// Renders a predicate over one element of a list of objects, in KQL for a nested field and
    /// as a query string for an object field, where a <paramref name="prefix"/> qualifies the field.
    /// </summary>
    private sealed class ObjectPredicate(
        ElasticSearchFilterRewriter owner,
        ParameterExpression item,
        string? prefix)
    {
        private bool IsQueryString => prefix is not null;

        private readonly List<string> _rendered = [];

        private readonly Dictionary<string, Type> _ownerTypes = [];

        public HashSet<string> Fields { get; } = [];

        public bool Negated { get; private set; }

        public bool Conjunction { get; private set; }

        public string Render(Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression { NodeType: ExpressionType.AndAlso } b:
                    Conjunction |= !IsNullGuard(b);
                    return Join(b, IsQueryString ? "AND" : "and");

                case BinaryExpression { NodeType: ExpressionType.OrElse } b:
                    return Join(b, IsQueryString ? "OR" : "or");

                case UnaryExpression { NodeType: ExpressionType.Not } u:
                    Negated = true;
                    return WithOwners(_rendered.Count, () => $"not ({Render(u.Operand)})");

                case BinaryExpression { NodeType: ExpressionType.Equal } b when IsNullValue(b.Right):
                    Negated = true;
                    return WithOwners(_rendered.Count, () => $"not {Field(b.Left)}:*");

                case BinaryExpression { NodeType: ExpressionType.NotEqual } b when IsNullValue(b.Right):
                    return $"{Field(b.Left)}:*";

                case BinaryExpression { NodeType: ExpressionType.Equal } b:
                    return $"{Field(b.Left)}:{Literal(b.Left, Evaluate(b.Right))}";

                case BinaryExpression { NodeType: ExpressionType.NotEqual } b:
                    Negated = true;
                    return WithOwners(
                        _rendered.Count,
                        () => $"not {Field(b.Left)}:{Literal(b.Left, Evaluate(b.Right))}");

                case BinaryExpression { NodeType: ExpressionType.GreaterThan } b:
                    return Compare(b, ">");

                case BinaryExpression { NodeType: ExpressionType.GreaterThanOrEqual } b:
                    return Compare(b, ">=");

                case BinaryExpression { NodeType: ExpressionType.LessThan } b:
                    return Compare(b, "<");

                case BinaryExpression { NodeType: ExpressionType.LessThanOrEqual } b:
                    return Compare(b, "<=");

                case MethodCallExpression { Method.Name: nameof(string.StartsWith), Object: { } o } c
                    when o.Type == typeof(string):
                    return StartsWith(Field(o), (string)Evaluate(c.Arguments[0])!);

                case MethodCallExpression { Method.Name: nameof(string.EndsWith), Object: { } o } c
                    when o.Type == typeof(string):
                    return $"{Field(o)}:*{Wildcard((string)Evaluate(c.Arguments[0])!)}";

                case MethodCallExpression { Method.Name: nameof(string.Contains), Object: { } o } c
                    when o.Type == typeof(string):
                    return $"{Field(o)}:*{Wildcard((string)Evaluate(c.Arguments[0])!)}*";

                case MethodCallExpression { Method.Name: nameof(Enumerable.Contains) } c:
                    return In(c);

                default:
                    throw ThrowHelper.ListFilter_NotSupported(expression.ToString());
            }
        }

        private string Join(BinaryExpression b, string op) => $"({Render(b.Left)} {op} {Render(b.Right)})";

        // a negated condition on a field of an object holds in .NET only when the object exists.
        private string WithOwners(int from, Func<string> render)
        {
            var text = render();
            var owners = _rendered.Skip(from)
                .Where(name => name.Contains('.'))
                .Select(name => name[..name.LastIndexOf('.')])
                .Distinct()
                .Select(name => IsQueryString ? $"_exists_:{prefix}{QueryStringName(name)}" : OwnerExists(name))
                .ToList();

            if (owners.Count == 0)
            {
                return text;
            }

            var and = IsQueryString ? " AND " : " and ";
            return $"({string.Join(and, owners)}{and}{text})";
        }

        private string OwnerExists(string name)
        {
            var leaves = owner.Leaves(_ownerTypes[name], name, []).Select(leaf => $"{KqlName(leaf)}:*");
            return "(" + string.Join(" or ", leaves) + ")";
        }

        // the null check that the filter adds before a string operation on the same field.
        private static bool IsNullGuard(BinaryExpression b)
            => b.Left is BinaryExpression { NodeType: ExpressionType.NotEqual } guard
                && IsNull(guard.Right)
                && b.Right is MethodCallExpression { Object: { } target }
                && target.ToString() == guard.Left.ToString();

        private string Compare(BinaryExpression b, string op)
            => IsQueryString
                ? $"{Field(b.Left)}:{op}{Literal(b.Left, Evaluate(b.Right))}"
                : $"{Field(b.Left)} {op} {Literal(b.Left, Evaluate(b.Right))}";

        private string StartsWith(string field, string value)
        {
            var pattern = Wildcard(value);

            if (!IsQueryString && pattern != value)
            {
                throw ThrowHelper.ListFilter_ReservedCharacterInPrefix(value);
            }

            return $"{field}:{pattern}*";
        }

        private string In(MethodCallExpression call)
        {
            var target = call.Object is null ? call.Arguments[1] : call.Arguments[0];
            var from = _rendered.Count;
            var field = Field(target);
            var choices = ((IEnumerable)Evaluate(call.Object ?? call.Arguments[0])!).Cast<object?>().ToList();
            var values = choices.Where(value => value is not null).Select(value => Literal(target, value)).ToList();

            // no value is in an empty list, so the condition holds for no element.
            var text = values.Count == 0
                ? IsQueryString ? $"({field}:* AND NOT {field}:*)" : $"({field}:* and not {field}:*)"
                : IsQueryString
                    ? $"{field}:({string.Join(" OR ", values)})"
                    : $"({string.Join(" or ", values.Select(v => $"{field}:{v}"))})";

            if (!choices.Contains(null))
            {
                return text;
            }

            // a null choice matches an element where the field is missing.
            Negated = true;
            var missing = IsQueryString ? $"NOT {field}:*" : $"not {field}:*";
            var or = IsQueryString ? "OR" : "or";
            return WithOwners(from, () => values.Count == 0 ? missing : $"({missing} {or} {text})");
        }

        private string Field(Expression expression)
        {
            while (expression is UnaryExpression { NodeType: ExpressionType.Convert } convert)
            {
                expression = convert.Operand;
            }

            if (expression is not MemberExpression member || !IsOnItem(member))
            {
                throw ThrowHelper.ListFilter_NotSupported(expression.ToString());
            }

            var parts = new Stack<string>();

            for (Expression? current = member; current is MemberExpression m; current = m.Expression)
            {
                parts.Push(owner.FieldName(m.Member));
            }

            var name = string.Join(".", parts);
            Fields.Add(name);
            _rendered.Add(name);

            if (member.Expression is MemberExpression parent)
            {
                _ownerTypes[name[..name.LastIndexOf('.')]] = parent.Type;
            }

            return IsQueryString ? prefix + QueryStringName(name) : KqlName(name);
        }

        private bool IsOnItem(MemberExpression member)
            => member.Expression == item
                || (member.Expression is MemberExpression parent && IsOnItem(parent));

        private string Wildcard(string value)
        {
            if (!IsQueryString && value.Contains('?'))
            {
                throw ThrowHelper.ListFilter_ReservedCharacterInPrefix(value);
            }

            var builder = new StringBuilder(value.Length);

            foreach (var c in value)
            {
                // KQL reads a tab or a line break in a value only as an escape sequence.
                if (!IsQueryString && c is '\t' or '\n' or '\r')
                {
                    builder.Append(c switch { '\t' => "\\t", '\n' => "\\n", _ => "\\r" });
                    continue;
                }

                if (c is '\\' or '(' or ')' or ':' or '<' or '>' or '"' or '*' or '{' or '}' or ' '
                    || (IsQueryString
                        && (char.IsWhiteSpace(c)
                            || c is '+' or '-' or '=' or '&' or '|' or '!' or '[' or ']' or '^' or '~' or '?' or '/')))
                {
                    builder.Append('\\');
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        // an enum is stored by name through a string converter, and by number otherwise.
        private static string Literal(Expression field, object? value)
            => value is Enum enumValue && !IsStoredByName(field)
                ? Literal(Convert.ChangeType(
                    enumValue,
                    Enum.GetUnderlyingType(enumValue.GetType()),
                    CultureInfo.InvariantCulture))
                : Literal(value);

        private static bool IsStoredByName(Expression field)
        {
            while (field is UnaryExpression { NodeType: ExpressionType.Convert } convert)
            {
                field = convert.Operand;
            }

            var type = Nullable.GetUnderlyingType(field.Type) ?? field.Type;
            return (field is MemberExpression member && IsStringConverter(member.Member))
                || IsStringConverter(type);
        }

        private static bool IsStringConverter(MemberInfo member)
            => member.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType is { } converter
                && (converter == typeof(JsonStringEnumConverter)
                    || (converter.IsGenericType
                        && converter.GetGenericTypeDefinition() == typeof(JsonStringEnumConverter<>)));

        private static string Literal(object? value)
            => value switch
            {
                string s => Quote(s),
                bool b => b ? "true" : "false",
                DateTime d => Quote(d.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
                DateTimeOffset d => Quote(d.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => throw ThrowHelper.ListFilter_NotSupported(value?.ToString() ?? "null")
            };

        private static string Quote(string value)
            => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
