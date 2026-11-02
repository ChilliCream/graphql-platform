using System.IO.Hashing;
using System.Text;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion;

// ReSharper disable once ClassNeverInstantiated.Local
// TODO: The atIndex needs to support variable batch requests
internal sealed class MockFieldMiddleware
{
    private const int DefaultListSize = 3;
    private static readonly INodeIdParser s_nodeIdParser = new DefaultNodeIdParser();

    public ValueTask InvokeAsync(IMiddlewareContext context)
    {
        var field = context.Selection.Field;
        var fieldName = field.Name;
        var fieldType = field.Type;
        var namedFieldType = fieldType.NamedType();
        int? nullIndex = null;

        var errorDirective = field.Directives["error"].FirstOrDefault()?.ToValue<ErrorDirective>();
        var nullDirective = field.Directives["null"].FirstOrDefault()?.ToValue<NullDirective>();
        var returnsDirective = field.Directives["returns"].FirstOrDefault()?.ToValue<ReturnsDirective>();
        var requestedTypes = returnsDirective?.Types
                .Select(v => (ObjectType)context.Schema.Types[v].ExpectObjectType())
                .ToArray() ??
            [];

        if (errorDirective is not null)
        {
            if (errorDirective.AtIndex.HasValue)
            {
                nullIndex = errorDirective.AtIndex;

                if (fieldType.IsListType())
                {
                    context.ReportError(CreateError(context, nullIndex));
                }
                else if (namedFieldType.IsScalarType() || namedFieldType.IsEnumType())
                {
                    var currentListIndex = context.Parent<MockObject>()?.Index;
                    if (currentListIndex.HasValue && currentListIndex == nullIndex)
                    {
                        throw new GraphQLException(CreateError(context));
                    }
                }
            }
            else
            {
                throw new GraphQLException(CreateError(context));
            }
        }
        else if (nullDirective is not null)
        {
            if (nullDirective.AtIndex.HasValue)
            {
                nullIndex = nullDirective.AtIndex;

                if (namedFieldType.IsScalarType() || namedFieldType.IsEnumType())
                {
                    var currentListIndex = context.Parent<MockObject>()?.Index;
                    if (currentListIndex.HasValue && currentListIndex == nullIndex)
                    {
                        context.Result = null;
                        return ValueTask.CompletedTask;
                    }
                }
            }
            else
            {
                context.Result = null;
                return ValueTask.CompletedTask;
            }
        }

        if ((fieldName.EndsWith("ById") || fieldName is "node" or "nodes")
            && field.Arguments.Count == 1
            && field.Arguments[0].Type.NamedType().Name == "ID")
        {
            var argument = field.Arguments[0];

            if (argument.Type.IsListType())
            {
                var ids = context.ArgumentValue<object[]>(argument.Name);

                IType nullableType = fieldType;
                if (fieldType.IsNonNullType())
                {
                    nullableType = fieldType.InnerType();
                }

                if (nullableType.IsListType())
                {
                    if (namedFieldType.IsCompositeType())
                    {
                        context.Result = CreateListOfObjects(
                            ids,
                            requestedTypes,
                            context,
                            nullIndex);
                        return ValueTask.CompletedTask;
                    }
                }
            }
            else
            {
                var id = context.ArgumentValue<object>(argument.Name);
                if (namedFieldType.IsCompositeType())
                {
                    var type = GetConcreteTypes(requestedTypes, context, [id], 1).First();

                    context.Result = CreateObject(id, type);
                    return ValueTask.CompletedTask;
                }
            }
        }
        else if (fieldName.EqualsInvariantIgnoreCase("id") && fieldType.NamedType() is IdType)
        {
            context.Result = context.Parent<MockObject>().Id;
            return ValueTask.CompletedTask;
        }

        if (fieldType.IsCompositeType())
        {
            var type = GetConcreteTypes(requestedTypes, context, null, 1).First();
            var id = CreateId(type, context, null);

            context.Result = CreateObject(id, type);
        }
        else if (fieldType.IsListType())
        {
            if (namedFieldType.IsCompositeType())
            {
                context.Result = CreateListOfObjects(
                    null,
                    requestedTypes,
                    context,
                    nullIndex);
            }
            else if (namedFieldType is EnumType enumType)
            {
                context.Result = CreateListOfEnums(enumType, nullIndex);
            }
            else if (namedFieldType is ScalarType scalarType)
            {
                context.Result = CreateListOfScalars(
                    scalarType,
                    context.Parent<MockObject?>() ?? new MockObject(null, context.Operation.RootType, null),
                    nullIndex,
                    context);
            }
        }
        else if (namedFieldType is EnumType enumType)
        {
            context.Result = CreateEnumValue(enumType);
        }
        else if (namedFieldType is ScalarType scalarType)
        {
            context.Result = CreateScalarValue(
                scalarType,
                context.Parent<MockObject?>() ?? new MockObject(null, context.Operation.RootType, null),
                context,
                null);
        }

        return ValueTask.CompletedTask;
    }

    private static object? CreateScalarValue(
        IScalarTypeDefinition scalarType,
        MockObject parentObject,
        IResolverContext context,
        int? index)
    {
        return scalarType switch
        {
            IdType => parentObject.Id ?? CreateId(parentObject.Type, context, index),
            StringType => parentObject.Type.Name
                + (parentObject.Id is not null && parentObject.Type.Name != "Viewer"
                    ? ": " + parentObject.Id
                    : string.Empty),
            IntType => 123,
            FloatType => 123.456,
            BooleanType => true,
            _ => null
        };
    }

    /// <summary>
    /// Creates an ID that depends only on the parent object's ID, the field's response name
    /// and argument values, and the list index.
    /// </summary>
    private static string CreateId(ITypeDefinition type, IResolverContext context, int? index)
    {
        var key = new StringBuilder();
        key.Append(context.Parent<MockObject?>()?.Id);
        key.Append('/');
        key.Append(context.ResponseName);
        key.Append('(');

        foreach (var argument in context.Selection.Field.Arguments)
        {
            key.Append(argument.Name);
            key.Append(':');
            key.Append(context.ArgumentLiteral<IValueNode>(argument.Name));
            key.Append(',');
        }

        key.Append(')');

        if (index.HasValue)
        {
            key.Append('[');
            key.Append(index.Value);
            key.Append(']');
        }

        var number = XxHash32.HashToUInt32(Encoding.UTF8.GetBytes(key.ToString()));
        var idString = type.Name + ":" + number;

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(idString));
    }

    private static IEnumerable<ObjectType> GetConcreteTypes(
        ObjectType[] requestedTypes,
        IMiddlewareContext context,
        object[]? ids,
        int listSize)
    {
        if (context.Selection.Field.Type.NamedType() is ObjectType objectType)
        {
            for (var i = 0; i < listSize; i++)
            {
                yield return objectType;
            }

            yield break;
        }

        var possibleTypes = context.Schema
            .GetPossibleTypes(context.Selection.Field.Type.AsTypeDefinition());

        for (var i = 0; i < listSize; i++)
        {
            var id = ids?.ElementAtOrDefault(i);

            if (id is string idString
                && s_nodeIdParser.TryParseTypeName(idString, out var typeName)
                && possibleTypes.FirstOrDefault(t => t.Name == typeName) is { } requestedType)
            {
                yield return requestedType;
                continue;
            }

            if (i < requestedTypes.Length)
            {
                yield return requestedTypes[i];
            }
            else
            {
                var typeIndex = (i - requestedTypes.Length) % possibleTypes.Count;
                yield return possibleTypes[typeIndex];
            }
        }
    }

    private static object? CreateEnumValue(EnumType enumType)
    {
        return enumType.Values.FirstOrDefault()?.Value;
    }

    private static object CreateObject(
        object id,
        ObjectType type,
        int? index = null)
    {
        return new MockObject(id, type, index);
    }

    private static object?[] CreateListOfScalars(
        IScalarTypeDefinition scalarType,
        MockObject parentObject,
        int? nullIndex,
        IResolverContext context)
    {
        return Enumerable.Range(0, DefaultListSize)
            .Select(index => nullIndex == index
                ? null
                : CreateScalarValue(scalarType, parentObject, context, index))
            .ToArray();
    }

    private static object?[] CreateListOfEnums(EnumType enumType, int? nullIndex)
    {
        return Enumerable.Range(0, DefaultListSize)
            .Select(index => nullIndex == index ? null : CreateEnumValue(enumType))
            .ToArray();
    }

    private static object?[] CreateListOfObjects(
        object[]? ids,
        ObjectType[] requestedTypes,
        IMiddlewareContext context,
        int? nullIndex)
    {
        var listLength = ids?.Length ?? DefaultListSize;
        var concreteTypes = GetConcreteTypes(requestedTypes, context, ids, listLength).ToArray();

        return Enumerable.Range(0, listLength)
            .Select(index =>
            {
                if (index == nullIndex)
                {
                    return null;
                }

                var concreteType = concreteTypes[index];
                var id = ids?.ElementAtOrDefault(index) ?? CreateId(concreteType, context, index);

                return CreateObject(id, concreteType, index);
            })
            .ToArray();
    }

    private static IError CreateError(IResolverContext context, int? index = null)
    {
        var path = context.Path;

        if (index.HasValue)
        {
            path = path.Append(index.Value);
        }

        return ErrorBuilder.New()
            .SetMessage("Unexpected Execution Error")
            .SetPath(path)
            .AddLocations(context.Selection)
            .Build();
    }
}
