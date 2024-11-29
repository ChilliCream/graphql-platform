using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Shared;

// ReSharper disable once ClassNeverInstantiated.Local
internal sealed class MockFieldMiddleware
{
    private const int DefaultListSize = 3;

    public ValueTask InvokeAsync(IMiddlewareContext context)
    {
        var mockingContext = context.GetGlobalStateOrDefault<AutomaticMockingContext>(nameof(AutomaticMockingContext));
        if (mockingContext is null)
        {
            mockingContext = new AutomaticMockingContext();
            context.SetGlobalState(nameof(AutomaticMockingContext), mockingContext);
        }

        var field = context.Selection.Field;
        var fieldName = field.Name;
        var fieldType = field.Type;
        var namedFieldType = fieldType.NamedType();
        int? nullIndex = null;

        var errorDirective = field.Directives["error"].FirstOrDefault()?.AsValue<ErrorDirective>();
        var nullDirective = field.Directives["null"].FirstOrDefault()?.AsValue<NullDirective>();

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

                    context.ValueType = possibleTypes.First();
                    context.Result = CreateObject(id);
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

                        context.ValueType = possibleTypes.First();
                        context.Result = CreateListOfObjects(ids, nullIndex);
                        return ValueTask.CompletedTask;
                    }
                }
            }
        }
        else if (fieldName.EqualsInvariantIgnoreCase("id") && fieldType.NamedType() is IdType)
        {
            var potentialId = context.Parent<ObjectTypeInst>().Id;

            if (potentialId is not null)
            {
                context.Result = potentialId;
                return ValueTask.CompletedTask;
            }
        }

        var hasIdFieldSelection = context.Select().IsSelected("id");

        if (fieldType.IsCompositeType())
        {
            int? id = hasIdFieldSelection ? ++mockingContext.IdCounter : null;
            var possibleTypes = context.Schema.GetPossibleTypes(namedFieldType);

            context.ValueType = possibleTypes.First();
            context.Result = CreateObject(id);
        }
        else if (fieldType.IsListType())
        {
            if (namedFieldType.IsCompositeType())
            {
                var ids = Enumerable.Range(0, DefaultListSize)
                    .Select(_ => (object?)(hasIdFieldSelection ? ++mockingContext.IdCounter : null)).ToArray();
                var possibleTypes = context.Schema.GetPossibleTypes(namedFieldType);

                context.ValueType = possibleTypes.First();
                context.Result = CreateListOfObjects(ids, nullIndex);
            }
            else if (namedFieldType is EnumType enumType)
            {
                context.Result = CreateListOfEnums(enumType, nullIndex);
            }
            else
            {
                context.Result = CreateListOfScalars(namedFieldType, nullIndex, mockingContext);
            }
        }
        else if (namedFieldType is EnumType enumType)
        {
            context.Result = CreateEnumValue(enumType);
        }
        else
        {
            context.Result = CreateScalarValue(namedFieldType, mockingContext);
        }

        return ValueTask.CompletedTask;
    }

    private object? CreateScalarValue(INamedType scalarType, AutomaticMockingContext mockingContext)
    {
        return scalarType switch
        {
            IdType => ++mockingContext.IdCounter,
            StringType => "string",
            IntType => 123,
            FloatType => 123.456,
            BooleanType => true,
            _ => null,
        };
    }

    private object? CreateEnumValue(EnumType enumType)
    {
        return enumType.Values.FirstOrDefault()?.Value;
    }

    private object CreateObject(object? id, int? index = null)
    {
        return new ObjectTypeInst(id, index);
    }

    private object?[] CreateListOfScalars(INamedType scalarType, int? nullIndex, AutomaticMockingContext mockingContext)
    {
        return Enumerable.Range(0, DefaultListSize)
            .Select(index => nullIndex == index ? null : CreateScalarValue(scalarType, mockingContext))
            .ToArray();
    }

    private object?[] CreateListOfEnums(EnumType enumType, int? nullIndex)
    {
        return Enumerable.Range(0, DefaultListSize)
            .Select(index => nullIndex == index ? null : CreateEnumValue(enumType))
            .ToArray();
    }

    private object?[] CreateListOfObjects(object?[] ids, int? nullIndex)
    {
        return ids
            .Select((itemId, index) => nullIndex == index ? null : CreateObject(itemId, index))
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
            .SetLocations([context.Selection.SyntaxNode])
            .Build();
    }

    private record ObjectTypeInst(object? Id = null, int? Index = null);

    private class AutomaticMockingContext
    {
        public int IdCounter { get; set; }
    }
}
