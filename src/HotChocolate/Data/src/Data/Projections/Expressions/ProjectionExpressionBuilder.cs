using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Projections.Expressions;

internal static class ProjectionExpressionBuilder
{
    private static readonly ConstantExpression s_null =
        Expression.Constant(null, typeof(object));
    private static readonly ConcurrentDictionary<Type, KeyMember> s_keyMembers = new();

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
        var result = s_keyMembers.GetOrAdd(type, static type => InferKeyMember(type));
        if (result.Property is { } property)
        {
            keyMember = property;
            return true;
        }

        keyMember = default!;
        return false;
    }

    private static KeyMember InferKeyMember(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var result = FindKeyMember(properties, "Id");

        if (result.Matched)
        {
            return result;
        }

        return FindKeyMember(properties, type.Name + "Id");
    }

    private static KeyMember FindKeyMember(PropertyInfo[] properties, string name)
    {
        PropertyInfo? match = null;
        var count = 0;

        foreach (var property in properties)
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                match = property;
                count++;
            }
        }

        if (count != 1 || !IsNonNullKey(match!))
        {
            return new KeyMember(null, count > 0);
        }

        return new KeyMember(match, true);
    }

    private static bool IsNonNullKey(PropertyInfo property)
    {
        if (property.GetMethod is not { IsPublic: true }
            || property.GetIndexParameters().Length != 0)
        {
            return false;
        }

        if (property.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) is null;
        }

        return new NullabilityInfoContext().Create(property).ReadState is NullabilityState.NotNull;
    }

    private readonly record struct KeyMember(PropertyInfo? Property, bool Matched);
}
