using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors.Configurations;
using static System.Linq.Expressions.Expression;
using static System.Reflection.BindingFlags;

namespace HotChocolate.ApolloFederation.Resolvers;

/// <summary>
/// This class contains helpers to generate representation-driven entity fillers. The representation is
/// authoritative wherever it carries a value, while resolver values survive where it is silent.
/// </summary>
/// <remarks>
/// Non-null concrete object fields are filled per-property instead of replaced wholesale. Abstract
/// object fields are still replaced as a whole.
/// </remarks>
internal static class ExternalSetterExpressionHelper
{
    private static readonly MethodInfo s_createSetValueExpression =
        typeof(ExternalSetterExpressionHelper)
            .GetMethod(nameof(CreateSetValueExpression), Static | NonPublic)!;
    private static readonly MethodInfo s_trySetExternal =
        typeof(ReferenceResolverHelper)
            .GetMethod(nameof(ReferenceResolverHelper.TrySetExternal), Static | Public)!;

    private static readonly ParameterExpression s_schema = Parameter(typeof(Schema), "schema");
    private static readonly ParameterExpression s_type = Parameter(typeof(ObjectType), "type");
    private static readonly ParameterExpression s_data = Parameter(typeof(IValueNode), "data");
    private static readonly ParameterExpression s_entity = Parameter(typeof(object), "entity");

    public static void TryAddExternalSetter(ObjectType type, ObjectTypeConfiguration typeDef)
    {
        var visited = new HashSet<ITypeDefinition> { type };
        var block = BuildFiller(type, type.RuntimeType, s_entity, [], visited);

        if (block is not null)
        {
            // A reference resolver may return an instance that is not the type's runtime type.
            // The generated setters cast the entity to that runtime type, so guard the fill.
            var guarded = IfThen(TypeIs(s_entity, type.RuntimeType), block);

            typeDef.Features.Set(new ExternalSetter(
                Lambda<Action<Schema, ObjectType, IValueNode, object>>(
                        guarded, s_schema, s_type, s_data, s_entity)
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
                var expression = CreateTrySetValue(type.RuntimeType, property, [field.Name], s_entity);
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

    private static BlockExpression? BuildFiller(
        ObjectType type,
        Type runtimeType,
        Expression instance,
        string[] pathPrefix,
        HashSet<ITypeDefinition> visited)
    {
        var statements = new List<Expression>();
        var variables = new List<ParameterExpression>();

        foreach (var field in type.Fields)
        {
            if (field.Member is not PropertyInfo property)
            {
                continue;
            }

            var fieldType = field.Type is NonNullType nonNull ? nonNull.NullableType : field.Type;
            var path = Append(pathPrefix, field.Name);

            if (fieldType is ListType)
            {
                if (property.SetMethod is not null)
                {
                    statements.Add(CreateTrySetValue(runtimeType, property, path, instance));
                }

                continue;
            }

            if (fieldType.NamedType() is ObjectType objectType
                && property.PropertyType.IsClass
                && property.GetMethod is not null)
            {
                var childType = property.PropertyType;
                var childVar = Parameter(typeof(object), field.Name);

                BlockExpression? recurse = null;
                // The visited set tracks the current ancestor chain. A repeated type is a cycle,
                // so non-null children are preserved and null children can still be reconstructed.
                if (visited.Add(objectType))
                {
                    recurse = BuildFiller(objectType, childType, childVar, path, visited);
                    visited.Remove(objectType);
                }

                var reconstruct = property.SetMethod is not null
                    ? CreateTrySetValue(runtimeType, property, path, instance)
                    : null;

                if (recurse is null && reconstruct is null)
                {
                    continue;
                }

                variables.Add(childVar);
                statements.Add(
                    Assign(
                        childVar,
                        Convert(Property(Convert(instance, runtimeType), property), typeof(object))));
                statements.Add(
                    IfThenElse(
                        ReferenceEqual(childVar, Constant(null, typeof(object))),
                        reconstruct ?? (Expression)Empty(),
                        recurse ?? (Expression)Empty()));
                continue;
            }

            if (property.SetMethod is not null)
            {
                statements.Add(CreateTrySetValue(runtimeType, property, path, instance));
            }
        }

        return statements.Count == 0 ? null : Block(variables, statements);
    }

    private static MethodCallExpression CreateTrySetValue(
        Type runtimeType,
        PropertyInfo property,
        string[] path,
        Expression instance)
    {
        var trySetValue = s_trySetExternal.MakeGenericMethod(property.PropertyType);
        var setter = CreateSetValue(runtimeType, property);
        return Call(trySetValue, s_schema, s_type, s_data, Convert(instance, typeof(object)), Constant(path), setter);
    }

    private static string[] Append(string[] prefix, string name)
    {
        var path = new string[prefix.Length + 1];
        Array.Copy(prefix, path, prefix.Length);
        path[prefix.Length] = name;
        return path;
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
