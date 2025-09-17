using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion;

// ReSharper disable once ClassNeverInstantiated.Local
internal sealed class MockFieldMiddleware
{
    private const int DefaultListSize = 3;

    public ValueTask InvokeAsync(IMiddlewareContext context)
    {
        var mockingContext = context.GetOrSetGlobalState(
            nameof(AutomaticMockingContext),
            _ => new AutomaticMockingContext());

        var field = context.Selection.Field;
        var fieldName = field.Name;
        var fieldType = field.Type;
        var namedFieldType = fieldType.NamedType();
        int? nullIndex = null;

        var errorDirective = field.Directives["error"].FirstOrDefault()?.ToValue<ErrorDirective>();
        var nullDirective = field.Directives["null"].FirstOrDefault()?.ToValue<NullDirective>();

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
                    var currentListIndex = context.Parent<ObjectTypeInst>()?.Index;
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
                    var currentListIndex = context.Parent<ObjectTypeInst>()?.Index;
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

        if (fieldName.EndsWith("ById") || fieldName is "node" or "nodes")
        {
            if (context.Selection.Arguments.ContainsName("id"))
            {
                var id = context.ArgumentValue<object>("id");
                if (namedFieldType.IsCompositeType())
                {
                    var possibleTypes = context.Schema.GetPossibleTypes(namedFieldType);
                    var type = DetermineTypeForAbstractSelection(possibleTypes, context.Selection, context.Schema);

                    context.ValueType = type;
                    context.Result = CreateObject(id, type);
                    return ValueTask.CompletedTask;
                }
            }

            if (context.Selection.Arguments.ContainsName("ids"))
            {
                var ids = context.ArgumentValue<object[]>("ids");

                IType nullableType = fieldType;
                if (fieldType.IsNonNullType())
                {
                    nullableType = fieldType.InnerType();
                }

                if (nullableType.IsListType())
                {
                    if (namedFieldType.IsCompositeType())
                    {
                        var possibleTypes = context.Schema.GetPossibleTypes(namedFieldType);
                        var type = DetermineTypeForAbstractSelection(possibleTypes, context.Selection, context.Schema);

                        context.ValueType = type;
                        context.Result = CreateListOfObjects(ids, type, nullIndex);
                        return ValueTask.CompletedTask;
                    }
                }
            }
        }
        else if (fieldName.EqualsInvariantIgnoreCase("id") && fieldType.NamedType() is IdType)
        {
            var potentialId = context.Parent<ObjectTypeInst>().Id;

            context.Result = potentialId;
            return ValueTask.CompletedTask;
        }

        if (fieldType.IsCompositeType())
        {
            var id = ++mockingContext.IdCounter;
            var possibleTypes = context.Schema.GetPossibleTypes(namedFieldType);
            var type = DetermineTypeForAbstractSelection(possibleTypes, context.Selection, context.Schema);

            context.ValueType = type;
            context.Result = CreateObject(id, type);
        }
        else if (fieldType.IsListType())
        {
            if (namedFieldType.IsCompositeType())
            {
                var ids = Enumerable.Range(0, DefaultListSize)
                    .Select(_ => (object)++mockingContext.IdCounter)
                    .ToArray();
                var possibleTypes = context.Schema.GetPossibleTypes(namedFieldType);
                var type = DetermineTypeForAbstractSelection(possibleTypes, context.Selection, context.Schema);

                context.ValueType = type;
                context.Result = CreateListOfObjects(ids, type, nullIndex);
            }
            else if (namedFieldType is EnumType enumType)
            {
                context.Result = CreateListOfEnums(enumType, nullIndex);
            }
            else if (namedFieldType is ScalarType scalarType)
            {
                context.Result = CreateListOfScalars(
                    scalarType,
                    context.Parent<ObjectTypeInst?>() ?? new ObjectTypeInst(null, context.Operation.RootType),
                    nullIndex,
                    mockingContext);
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
                context.Parent<ObjectTypeInst?>() ?? new ObjectTypeInst(null, context.Operation.RootType),
                mockingContext);
        }

        return ValueTask.CompletedTask;
    }

    private ITypeDefinition DetermineTypeForAbstractSelection(
        IReadOnlyList<ObjectType> possibleTypes,
        ISelection selection,
        ISchemaDefinition schema)
    {
        var inlineFragmentNode = selection.SelectionSet?.Selections
            .FirstOrDefault(s =>
                s is InlineFragmentNode inlineFragmentNode && inlineFragmentNode.TypeCondition is not null);

        if (inlineFragmentNode is InlineFragmentNode { TypeCondition: {} typeCondition }
            && schema.Types.TryGetType<ITypeDefinition>(typeCondition.Name.Value, out var type))
        {
            return type;
        }

        return possibleTypes.First();
    }

    private object? CreateScalarValue(
        IScalarTypeDefinition scalarType,
        ObjectTypeInst parentObject,
        AutomaticMockingContext mockingContext)
    {
        return scalarType switch
        {
            IdType => parentObject.Id ?? ++mockingContext.IdCounter,
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

    private object? CreateEnumValue(EnumType enumType)
    {
        return enumType.Values.FirstOrDefault()?.Value;
    }

    private object CreateObject(object id, ITypeDefinition type, int? index = null)
    {
        return new ObjectTypeInst(id, type, index);
    }

    private object?[] CreateListOfScalars(
        IScalarTypeDefinition scalarType,
        ObjectTypeInst parentObject,
        int? nullIndex,
        AutomaticMockingContext mockingContext)
    {
        return Enumerable.Range(0, DefaultListSize)
            .Select(index => nullIndex == index ? null : CreateScalarValue(scalarType, parentObject, mockingContext))
            .ToArray();
    }

    private object?[] CreateListOfEnums(EnumType enumType, int? nullIndex)
    {
        return Enumerable.Range(0, DefaultListSize)
            .Select(index => nullIndex == index ? null : CreateEnumValue(enumType))
            .ToArray();
    }

    private object?[] CreateListOfObjects(object[] ids, ITypeDefinition type, int? nullIndex)
    {
        return ids
            .Select((itemId, index) => nullIndex == index ? null : CreateObject(itemId, type, index))
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
            .AddLocation(context.Selection.SyntaxNode)
            .Build();
    }

    private record ObjectTypeInst(object? Id, ITypeDefinition Type, int? Index = null);

    private class AutomaticMockingContext
    {
        public int IdCounter { get; set; }
    }
}
