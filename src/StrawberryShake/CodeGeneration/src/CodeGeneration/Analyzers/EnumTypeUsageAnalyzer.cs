using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    internal sealed class EnumTypeUsageAnalyzer : QuerySyntaxWalker<object?>
    {
        private readonly HashSet<EnumType> _enumTypes = new();
        private readonly HashSet<IInputType> _visitedTypes = new();
        private readonly Stack<IType> _typeContext = new();
        private readonly Stack<IOutputField> _fieldContext = new();
        private readonly ISchema _schema;

        public EnumTypeUsageAnalyzer(ISchema schema)
        {
            _schema = schema;
        }

        public ISet<EnumType> EnumTypes => _enumTypes;

        public void Analyze(DocumentNode document)
        {
            Visit(document, null);
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode node,
            object? context)
        {
            ObjectType operationType = _schema.GetOperationType(node.Operation);

            _typeContext.Push(operationType);

            base.VisitOperationDefinition(node, context);

            _typeContext.Pop();
        }

        protected override void VisitVariableDefinition(
            VariableDefinitionNode node,
            object? context)
        {
            if (_schema.TryGetType(node.Type.NamedType().Name.Value, out INamedType type) &&
                type is IInputType inputType)
            {
                VisitInputType(inputType);
            }
        }

        protected override void VisitField(FieldNode node, object? context)
        {
            IType currentType = _typeContext.Peek();

            if (currentType is IComplexOutputType complexType &&
                complexType.Fields.TryGetField(node.Name.Value, out IOutputField? field))
            {
                INamedType fieldType = field.Type.NamedType();
                if (fieldType is IInputType inputType)
                {
                    VisitInputType(inputType);
                }

                _typeContext.Push(fieldType);
                _fieldContext.Push(field);

                base.VisitField(node, context);

                _fieldContext.Pop();
                _typeContext.Pop();
            }
        }

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node,
            object? context)
        {
            INamedType type = _schema!.GetType<INamedType>(node.TypeCondition.Name.Value);

            _typeContext.Push(type);

            base.VisitFragmentDefinition(node, context);

            _typeContext.Pop();
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
                else if (type is InputObjectType inputObjectType)
                {
                    VisitInputObjectType(inputObjectType);
                }
                else if (type is EnumType enumType)
                {
                    _enumTypes.Add(enumType);
                }
            }
        }

        private void VisitInputObjectType(InputObjectType type)
        {
            foreach (IInputField field in type.Fields)
            {
                VisitInputType(field.Type);
            }
        }

        protected override void VisitInlineFragment(
            InlineFragmentNode node,
            object? context)
        {
            if (node.TypeCondition != null)
            {
                INamedType type = _schema!.GetType<INamedType>(node.TypeCondition.Name.Value);
                _typeContext.Push(type);
            }

            base.VisitInlineFragment(node, context);

            if (node.TypeCondition != null)
            {
                _typeContext.Pop();
            }
        }
    }
}
