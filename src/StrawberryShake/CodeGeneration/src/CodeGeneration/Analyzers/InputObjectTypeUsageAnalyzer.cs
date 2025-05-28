using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal sealed class InputObjectTypeUsageAnalyzer(Schema schema) : SyntaxWalker<object?>
{
    private readonly HashSet<IInputTypeDefinition> _inputTypes = [];
    private readonly HashSet<IType> _visitedTypes = [];

    public ISet<IInputTypeDefinition> InputTypes => _inputTypes;

    public void Analyze(DocumentNode document)
    {
        Visit(document, null);
    }

    protected override ISyntaxVisitorAction Enter(VariableDefinitionNode node, object? context)
    {
        if (schema.Types.TryGetType(node.Type.NamedType().Name.Value, out var type)
            && type is IInputType inputType)
        {
            VisitInputType(inputType);
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

                    case NonNullType { NullableType: IInputType innerType }:
                        type = innerType;
                        continue;

                    case IInputTypeDefinition namedInputType:
                        VisitNamedInputType(namedInputType);
                        break;
                }
            }

            break;
        }
    }

    private void VisitNamedInputType(IInputTypeDefinition type)
    {
        if (_inputTypes.Add(type) && type is InputObjectType inputObjectType)
        {
            foreach (IInputValueDefinition field in inputObjectType.Fields)
            {
                VisitInputType(field.Type);
            }
        }
    }
}
