using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal sealed class InputObjectTypeUsageAnalyzer : QuerySyntaxWalker<object?>
    {
        private readonly HashSet<INamedInputType> _inputTypes = new();
        private readonly HashSet<IInputType> _visitedTypes = new();
        private readonly ISchema _schema;

        public InputObjectTypeUsageAnalyzer(ISchema schema)
        {
            _schema = schema;
        }

        public ISet<INamedInputType> InputTypes => _inputTypes;

        public void Analyze(DocumentNode document)
        {
            Visit(document, null);
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode node, object? context)
        {
            ObjectType operationType = _schema.GetOperationType(node.Operation);

            VisitMany(
                node.VariableDefinitions,
                context,
                VisitVariableDefinition);
        }

        protected override void VisitVariableDefinition(
            VariableDefinitionNode node,
            object? context)
        {
            if (_schema.TryGetType(node.Type.NamedType().Name.Value, out INamedType type)
                && type is IInputType inputType)
            {
                VisitInputType(inputType);
            }
        }

        private void VisitInputType(IInputType type)
        {
            if (_visitedTypes.Add(type))
            {
                if (type is HotChocolate.Types.ListType listType
                    && listType.ElementType is IInputType elementType)
                {
                    VisitInputType(elementType);
                }
                else if (type is NonNullType nonNullType
                    && nonNullType.Type is IInputType innerType)
                {
                    VisitInputType(innerType);
                }
                else if (type is INamedInputType namedInputType)
                {
                    VisitNamedInputType(namedInputType);
                }
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
}
