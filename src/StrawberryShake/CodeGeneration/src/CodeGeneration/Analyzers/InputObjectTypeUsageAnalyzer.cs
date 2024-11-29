using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers;

internal sealed class InputObjectTypeUsageAnalyzer(ISchema schema) : SyntaxWalker<object?>
{
    private readonly HashSet<INamedInputType> _inputTypes = [];
    private readonly HashSet<IInputType> _visitedTypes = [];

    public ISet<INamedInputType> InputTypes => _inputTypes;

    public void Analyze(DocumentNode document)
    {
        Visit(document, null);
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

                    case INamedInputType namedInputType:
                        VisitNamedInputType(namedInputType);
                        break;
                }
            }

            break;
        }
    }

    private void VisitNamedInputType(INamedInputType type)
    {
        if (_inputTypes.Add(type))
        {
            if (type is InputObjectType inputObjectType)
            {
                foreach (IInputField field in inputObjectType.Fields)
                {
                    VisitInputType(field.Type);
                }
            }
        }
    }
}
