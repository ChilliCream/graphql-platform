using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal sealed class EnumTypeUsageAnalyzer(ISchema schema) : SyntaxWalker<object?>
{
    private readonly HashSet<EnumType> _enumTypes = [];
    private readonly HashSet<IInputType> _visitedTypes = [];
    private readonly Stack<IType> _typeContext = new();

    public ISet<EnumType> EnumTypes => _enumTypes;

    public void Analyze(DocumentNode document)
    {
        Visit(document, null);
    }

    protected override ISyntaxVisitorAction Enter(OperationDefinitionNode node, object? context)
    {
        var operationType = schema.GetOperationType(node.Operation)!;

        _typeContext.Push(operationType);

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(OperationDefinitionNode node, object? context)
    {
        _typeContext.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(VariableDefinitionNode node, object? context)
    {
        if (schema.TryGetType<INamedType>(node.Type.NamedType().Name.Value, out var type) &&
            type is IInputType inputType)
        {
            VisitInputType(inputType);
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(FieldNode node, object? context)
    {
        var currentType = _typeContext.Peek();

        if (currentType is IComplexOutputType complexType &&
            complexType.Fields.TryGetField(node.Name.Value, out var field))
        {
            var fieldType = field.Type.NamedType();
            if (fieldType is IInputType inputType)
            {
                VisitInputType(inputType);
            }

            _typeContext.Push(fieldType);

            return Continue;
        }

        return Skip;
    }

    protected override ISyntaxVisitorAction Leave(FieldNode node, object? context)
    {
        _typeContext.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(FragmentDefinitionNode node, object? context)
    {
        var type = schema.GetType<INamedType>(node.TypeCondition.Name.Value);

        _typeContext.Push(type);

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(FragmentDefinitionNode node, object? context)
    {
        _typeContext.Pop();

        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(InlineFragmentNode node, object? context)
    {
        if (node.TypeCondition != null)
        {
            var type = schema.GetType<INamedType>(node.TypeCondition.Name.Value);
            _typeContext.Push(type);
        }

        return Continue;
    }

    protected override ISyntaxVisitorAction Leave(InlineFragmentNode node, object? context)
    {
        if (node.TypeCondition != null)
        {
            _typeContext.Pop();
        }

        return Continue;
    }

    private void VisitInputType(IInputType type)
    {
        while (true)
        {
            if (_visitedTypes.Add(type))
            {
                switch (type)
                {
                    case ListType { ElementType: IInputType elementType }:
                        type = elementType;
                        continue;

                    case NonNullType { Type: IInputType innerType }:
                        type = innerType;
                        continue;

                    case InputObjectType inputObjectType:
                        VisitInputObjectType(inputObjectType);
                        break;

                    case EnumType enumType:
                        _enumTypes.Add(enumType);
                        break;
                }
            }

            break;
        }
    }

    private void VisitInputObjectType(InputObjectType type)
    {
        foreach (IInputField field in type.Fields)
        {
            VisitInputType(field.Type);
        }
    }
}
