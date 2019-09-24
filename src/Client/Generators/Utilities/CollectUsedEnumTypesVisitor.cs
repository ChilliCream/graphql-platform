using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Utilities
{
    internal class CollectUsedEnumTypesVisitor
        : QuerySyntaxWalker<object?>
    {
        private readonly HashSet<INamedType> _enumTypes = new HashSet<INamedType>();
        private readonly Stack<IType> _typeContext = new Stack<IType>();
        private readonly Stack<IOutputField> _fieldContext = new Stack<IOutputField>();
        private readonly Stack<IInputField> _inputFieldContext = new Stack<IInputField>();
        private ISchema? _schema;

        public static IReadOnlyList<EnumType> Collect(ISchema schema, DocumentNode node)
        {
            var visitor = new CollectUsedEnumTypesVisitor();
            visitor._schema = schema;
            visitor.Visit(node, null);
            return visitor._enumTypes.OfType<EnumType>().ToArray();
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode node, object? context)
        {
            ObjectType operationType = _schema!.GetOperationType(node.Operation);

            _typeContext.Push(operationType);

            base.VisitOperationDefinition(node, context);

            _typeContext.Pop();
        }

        protected override void VisitField(FieldNode node, object? context)
        {
            IType currentType = _typeContext.Peek();

            if (currentType is IComplexOutputType complexType
                && complexType.Fields.TryGetField(
                    node.Name.Value, out IOutputField field))
            {
                INamedType fieldType = field.Type.NamedType();
                if (fieldType.IsEnumType())
                {
                    _enumTypes.Add(fieldType);
                }

                _typeContext.Push(fieldType);
                _fieldContext.Push(field);

                base.VisitField(node, context);

                _fieldContext.Pop();
                _typeContext.Pop();
            }
        }

        protected override void VisitArgument(ArgumentNode node, object? context)
        {
            IOutputField field = _fieldContext.Peek();

            if (field.Arguments.TryGetField(node.Name.Value, out IInputField inputField))
            {
                INamedType fieldType = inputField.Type.NamedType();

                if (fieldType.IsEnumType())
                {
                    _enumTypes.Add(fieldType);
                }

                _typeContext.Push(fieldType);

                base.VisitArgument(node, context);

                _typeContext.Pop();
            }
        }

        protected override void VisitObjectField(ObjectFieldNode node, object? context)
        {
            IType currentType = _typeContext.Peek();

            if (currentType is InputObjectType inputObjectType
                && inputObjectType.Fields.TryGetField(
                    node.Name.Value, out IInputField field))
            {
                INamedType fieldType = field.Type.NamedType();
                if (fieldType.IsEnumType())
                {
                    _enumTypes.Add(fieldType);
                }

                _typeContext.Push(fieldType);
                _inputFieldContext.Push(field);

                base.VisitObjectField(node, context);

                _inputFieldContext.Pop();
                _typeContext.Pop();
            }
        }
    }
}
