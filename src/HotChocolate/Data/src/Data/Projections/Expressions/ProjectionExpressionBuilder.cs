using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Projections.Expressions;

internal static class ProjectionExpressionBuilder
{
    private static readonly ConstantExpression s_null =
        Expression.Constant(null, typeof(object));

    public static MemberInitExpression CreateMemberInit(
        Type type,
        IEnumerable<MemberBinding> expressions)
    {
        var ctor = Expression.New(type);
        return Expression.MemberInit(ctor, expressions);
    }

    public static Expression NotNull(Expression expression)
    {
        return Expression.NotEqual(expression, s_null);
    }

    public static Expression NotNullAndAlso(Expression property, Expression condition)
    {
        return Expression.Condition(
            NotNull(property),
            condition,
            Expression.Default(property.Type));
    }

    public static Expression NotNullByKeyAndAlso(Expression property, Expression condition)
    {
        if (TryCreateKeyNotNullCheck(property, out var keyNotNullCheck))
        {
            return Expression.Condition(
                keyNotNullCheck,
                condition,
                Expression.Default(property.Type));
        }

        return NotNullAndAlso(property, condition);
    }

    private static bool TryCreateKeyNotNullCheck(
        Expression property,
        out Expression keyNotNullCheck)
    {
        if (property.Type.IsValueType)
        {
            keyNotNullCheck = default!;
            return false;
        }

        if (!TryGetKeyMember(property.Type, out var keyMember))
        {
            keyNotNullCheck = default!;
            return false;
        }

        var key = Expression.MakeMemberAccess(property, keyMember);
        var keyType = key.Type;
        var nullableType = Nullable.GetUnderlyingType(keyType);

        if (nullableType is not null || !keyType.IsValueType)
        {
            keyNotNullCheck = Expression.NotEqual(key, Expression.Constant(null, keyType));
            return true;
        }

        var liftedType = typeof(Nullable<>).MakeGenericType(keyType);
        var liftedKey = Expression.Convert(key, liftedType);
        keyNotNullCheck = Expression.NotEqual(liftedKey, Expression.Constant(null, liftedType));
        return true;
    }

    private static bool TryGetKeyMember(Type type, out MemberInfo keyMember)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

        var idProperty = type.GetProperty("Id", flags);
        if (idProperty is not null && IsKeyLike(idProperty))
        {
            keyMember = idProperty;
            return true;
        }

        var typeNameIdProperty = type.GetProperty(type.Name + "Id", flags);
        if (typeNameIdProperty is not null && IsKeyLike(typeNameIdProperty))
        {
            keyMember = typeNameIdProperty;
            return true;
        }

        var keyProperty = type.GetProperties(flags)
            .FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && IsKeyLike(p));

        if (keyProperty is not null)
        {
            keyMember = keyProperty;
            return true;
        }

        keyMember = default!;
        return false;
    }

    private static bool IsKeyLike(PropertyInfo property)
        => property.CanRead
            && (property.PropertyType.IsValueType || property.PropertyType == typeof(string));
}
