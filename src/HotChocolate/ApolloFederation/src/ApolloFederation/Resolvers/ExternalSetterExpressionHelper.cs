using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Definitions;
using static System.Linq.Expressions.Expression;
using static System.Reflection.BindingFlags;

namespace HotChocolate.ApolloFederation.Resolvers;

/// <summary>
/// This class contains helpers to generate external field setters.
/// </summary>
internal static class ExternalSetterExpressionHelper
{
    private static readonly MethodInfo _createSetValueExpression =
        typeof(ExternalSetterExpressionHelper)
            .GetMethod(nameof(CreateSetValueExpression), Static | NonPublic)!;
    private static readonly MethodInfo _trySetExternal =
        typeof(ReferenceResolverHelper)
            .GetMethod(nameof(ReferenceResolverHelper.TrySetExternal), Static | Public)!;

    private static readonly ParameterExpression _type = Parameter(typeof(ObjectType), "type");
    private static readonly ParameterExpression _data = Parameter(typeof(IValueNode), "data");
    private static readonly ParameterExpression _entity = Parameter(typeof(object), "entity");

    public static void TryAddExternalSetter(ObjectType type, ObjectTypeDefinition typeDef)
    {
        List<Expression>? block = null;

        foreach (var field in type.Fields)
        {
            if (field.Directives.ContainsDirective<ExternalDirective>() &&
                field.Member is PropertyInfo { SetMethod: { }, } property)
            {
                var expression = CreateTrySetValue(type.RuntimeType, property, field.Name);
                (block ??= []).Add(expression);
            }
        }

        if (block is not null)
        {
            typeDef.ContextData[FederationContextData.ExternalSetter] =
                Lambda<Action<ObjectType, IValueNode, object>>(
                    Block(block), _type, _data, _entity)
                        .Compile();
        }
    }

    private static Expression CreateTrySetValue(
        Type runtimeType,
        PropertyInfo property,
        string fieldName)
    {
        var trySetValue = _trySetExternal.MakeGenericMethod(property.PropertyType);
        var path = Constant(new[] { fieldName, });
        var setter = CreateSetValue(runtimeType, property);
        return Call(trySetValue, _type, _data, _entity, path, setter);
    }

    private static Expression CreateSetValue(
        Type runtimeType,
        PropertyInfo property)
        => (Expression)_createSetValueExpression
            .MakeGenericMethod(property.PropertyType)
            .Invoke(null, [runtimeType, property,])!;

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
