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
    private static readonly MethodInfo s_createGetValueExpression =
        typeof(ExternalSetterExpressionHelper)
            .GetMethod(nameof(CreateGetValueExpression), Static | NonPublic)!;
    private static readonly MethodInfo s_createSetLeafValueExpression =
        typeof(ExternalSetterExpressionHelper)
            .GetMethod(nameof(CreateSetLeafValueExpression), Static | NonPublic)!;
    private static readonly MethodInfo s_trySetExternal =
        typeof(ReferenceResolverHelper)
            .GetMethod(nameof(ReferenceResolverHelper.TrySetExternal), Static | Public)!;
    private static readonly MethodInfo s_trySetNestedExternal =
        typeof(ReferenceResolverHelper)
            .GetMethod(nameof(ReferenceResolverHelper.TrySetNestedExternal), Static | Public)!;

    private static readonly ParameterExpression s_schema = Parameter(typeof(Schema), "schema");
    private static readonly ParameterExpression s_type = Parameter(typeof(ObjectType), "type");
    private static readonly ParameterExpression s_data = Parameter(typeof(IValueNode), "data");
    private static readonly ParameterExpression s_entity = Parameter(typeof(object), "entity");

    public static void TryAddExternalSetter(ObjectType type, ObjectTypeConfiguration typeDef)
    {
        List<Expression>? block = null;

        foreach (var field in type.Fields)
        {
            if (field.Directives.ContainsDirective<ExternalDirective>()
                && field.Member is PropertyInfo { SetMethod: not null } property)
            {
                var expression = CreateTrySetValue(type.RuntimeType, property, field.Name);
                (block ??= []).Add(expression);
            }
        }

        AddNestedExternalSetters(type, ref block);

        if (block is not null)
        {
            typeDef.Features.Set(new ExternalSetter(
                Lambda<Action<Schema, ObjectType, IValueNode, object>>(
                    Block(block), s_schema, s_type, s_data, s_entity)
                        .Compile()));
        }
    }

    public static void TryAddExternalSetter(InterfaceType type, InterfaceTypeConfiguration typeDef)
    {
        List<Expression>? block = null;

        foreach (var field in type.Fields)
        {
            if (field.Directives.ContainsDirective<ExternalDirective>()
                && typeDef.Fields.FirstOrDefault(f => f.Name == field.Name) is
                { Member: PropertyInfo { SetMethod: not null } property })
            {
                var expression = CreateTrySetValue(type.RuntimeType, property, field.Name);
                (block ??= []).Add(expression);
            }
        }

        if (block is not null)
        {
            typeDef.Features.Set(new ExternalSetter(
                Lambda<Action<Schema, ObjectType, IValueNode, object>>(
                        Block(block), s_schema, s_type, s_data, s_entity)
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
        return Call(trySetValue, s_schema, s_type, s_data, s_entity, path, setter);
    }

    private static void AddNestedExternalSetters(ObjectType type, ref List<Expression>? block)
    {
        foreach (var field in type.Fields)
        {
            if (field.Directives.FirstOrDefault<RequiresDirective>() is not { } directive)
            {
                continue;
            }

            var requires = directive.ToValue<RequiresDirective>();

            foreach (var path in EnumerateLeafPaths(requires.Fields))
            {
                // v1 scope: two-segment object paths only (intermediate object + external leaf).
                if (path.Length != 2)
                {
                    continue;
                }

                if (!type.Fields.TryGetField(path[0], out var intermediateField)
                    || intermediateField.Directives.ContainsDirective<ExternalDirective>()
                    || intermediateField.Member is not PropertyInfo { GetMethod: not null } intermediateProperty
                    || !intermediateProperty.PropertyType.IsClass)
                {
                    continue;
                }

                var intermediateFieldType = intermediateField.Type is NonNullType nonNull
                    ? nonNull.NullableType
                    : intermediateField.Type;

                // v1 scope: object intermediates only. Lists and abstract types are out of scope.
                if (intermediateFieldType is not ObjectType intermediateObjectType)
                {
                    continue;
                }

                if (!intermediateObjectType.Fields.TryGetField(path[1], out var leafField)
                    || leafField.Member is not PropertyInfo { SetMethod: not null } leafProperty)
                {
                    continue;
                }

                var expression = CreateTrySetNestedValue(
                    type.RuntimeType,
                    intermediateProperty,
                    leafProperty,
                    [path[0]],
                    path);
                (block ??= []).Add(expression);
            }
        }
    }

    private static IEnumerable<string[]> EnumerateLeafPaths(SelectionSetNode selectionSet)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode fieldNode)
            {
                continue;
            }

            if (fieldNode.SelectionSet is null)
            {
                yield return [fieldNode.Name.Value];
            }
            else
            {
                foreach (var childPath in EnumerateLeafPaths(fieldNode.SelectionSet))
                {
                    var path = new string[childPath.Length + 1];
                    path[0] = fieldNode.Name.Value;
                    Array.Copy(childPath, 0, path, 1, childPath.Length);
                    yield return path;
                }
            }
        }
    }

    private static MethodCallExpression CreateTrySetNestedValue(
        Type runtimeType,
        PropertyInfo intermediateProperty,
        PropertyInfo leafProperty,
        string[] intermediatePath,
        string[] leafPath)
    {
        var intermediateType = intermediateProperty.PropertyType;
        var leafType = leafProperty.PropertyType;
        var trySetValue = s_trySetNestedExternal.MakeGenericMethod(intermediateType, leafType);

        var getIntermediate = CreateGetValue(runtimeType, intermediateProperty);
        var setLeaf = CreateSetLeafValue(intermediateType, leafProperty);
        var setIntermediate = intermediateProperty.SetMethod is not null
            ? CreateSetValue(runtimeType, intermediateProperty)
            : NullSetIntermediate(intermediateType);

        return Call(
            trySetValue,
            s_schema,
            s_type,
            s_data,
            s_entity,
            Constant(intermediatePath),
            Constant(leafPath),
            getIntermediate,
            setLeaf,
            setIntermediate);
    }

    private static ConstantExpression NullSetIntermediate(Type intermediateType)
        => Constant(null, typeof(Action<,>).MakeGenericType(typeof(object), intermediateType));

    private static ConstantExpression CreateGetValue(Type runtimeType, PropertyInfo property)
        => (ConstantExpression)s_createGetValueExpression
            .MakeGenericMethod(property.PropertyType)
            .Invoke(null, [runtimeType, property])!;

    private static ConstantExpression CreateGetValueExpression<TValue>(
        Type runtimeType,
        PropertyInfo property)
        where TValue : class
    {
        var entity = Parameter(typeof(object), "entity");
        var castedEntity = Convert(entity, runtimeType);
        var getValue = Property(castedEntity, property);
        return Constant(Lambda<Func<object, TValue?>>(getValue, entity).Compile());
    }

    private static Expression CreateSetLeafValue(Type intermediateType, PropertyInfo property)
        => (Expression)s_createSetLeafValueExpression
            .MakeGenericMethod(intermediateType, property.PropertyType)
            .Invoke(null, [intermediateType, property])!;

    private static ConstantExpression CreateSetLeafValueExpression<TIntermediate, TValue>(
        Type intermediateType,
        PropertyInfo property)
        where TIntermediate : class
    {
        var intermediate = Parameter(intermediateType, "intermediate");
        var value = Parameter(property.PropertyType, "value");
        var setValue = Call(intermediate, property.SetMethod!, value);
        return Constant(Lambda<Action<TIntermediate, TValue?>>(setValue, intermediate, value).Compile());
    }

    private static Expression CreateSetValue(
        Type runtimeType,
        PropertyInfo property)
        => (Expression)s_createSetValueExpression
            .MakeGenericMethod(property.PropertyType)
            .Invoke(null, [runtimeType, property])!;

    private static ConstantExpression CreateSetValueExpression<TValue>(
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
