using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;
using static System.Linq.Expressions.Expression;
using static System.Reflection.BindingFlags;

namespace HotChocolate.ApolloFederation.Resolvers;

/// <summary>
/// This class contains helpers to generate external field setters.
/// </summary>
internal static class ExternalSetterExpressionHelper
{
    private static readonly MethodInfo s_createSetValueExpression =
        typeof(ExternalSetterExpressionHelper)
            .GetMethod(nameof(CreateSetValueExpression), Static | NonPublic)!;
    private static readonly MethodInfo s_trySetExternal =
        typeof(ReferenceResolverHelper)
            .GetMethod(nameof(ReferenceResolverHelper.TrySetExternal), Static | Public)!;

    private static readonly ParameterExpression s_type = Parameter(typeof(ObjectType), "type");
    private static readonly ParameterExpression s_data = Parameter(typeof(IValueNode), "data");
    private static readonly ParameterExpression s_entity = Parameter(typeof(object), "entity");

    public static void TryAddExternalSetter(ObjectType type, ObjectTypeConfiguration typeDef)
    {
        List<Expression>? block = null;

        foreach (var field in type.Fields)
        {
            if (field.Directives.ContainsDirective<ExternalDirective>() &&
                field.Member is PropertyInfo { SetMethod: not null } property)
            {
                var expression = CreateTrySetValue(type.RuntimeType, property, field.Name);
                (block ??= []).Add(expression);
            }
        }

        if (block is not null)
        {
            typeDef.Features.Set(new ExternalSetter(
                Lambda<Action<ObjectType, IValueNode, object>>(
                    Block(block), s_type, s_data, s_entity)
                        .Compile()));
        }
    }

    public static void TryAddExternalSetter(InterfaceType type, InterfaceTypeConfiguration typeDef)
    {
        List<Expression>? block = null;

        foreach (var field in type.Fields)
        {
            if (field.Directives.ContainsDirective<ExternalDirective>() &&
                typeDef.Fields.FirstOrDefault(f => f.Name == field.Name) is
                    { Member: PropertyInfo { SetMethod: not null } property })
            {
                var expression = CreateTrySetValue(type.RuntimeType, property, field.Name);
                (block ??= []).Add(expression);
            }
        }

        if (block is not null)
        {
            typeDef.Features.Set(new ExternalSetter(
                Lambda<Action<ObjectType, IValueNode, object>>(
                        Block(block), s_type, s_data, s_entity)
                    .Compile()));
        }
    }

    private static Expression CreateTrySetValue(
        Type runtimeType,
        PropertyInfo property,
        string fieldName)
    {
        var trySetValue = s_trySetExternal.MakeGenericMethod(property.PropertyType);
        var path = Constant(new[] { fieldName });
        var setter = CreateSetValue(runtimeType, property);
        return Call(trySetValue, s_type, s_data, s_entity, path, setter);
    }

    private static Expression CreateSetValue(
        Type runtimeType,
        PropertyInfo property)
        => (Expression)s_createSetValueExpression
            .MakeGenericMethod(property.PropertyType)
            .Invoke(null, [runtimeType, property])!;

    private static Expression CreateSetValueExpression<TValue>(
        Type runtimeType,
        PropertyInfo property)
    {
        var entity = Parameter(typeof(object), "entity");
        var castedEntity = Convert(entity, runtimeType);
        var value = Parameter(property.PropertyType, "value");
        var setValue = Call(castedEntity, property.SetMethod!, value);
        return Constant(Lambda<Action<object, TValue?>>(setValue, entity, value).Compile());
    }
}
