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

        protected override void VisitVariableDefinition(
            VariableDefinitionNode node,
            object? context)
        {
            if (_schema!.TryGetType(
                node.Type.NamedType().Name.Value,
                out INamedType type)
                && type.IsEnumType())
            {
                _enumTypes.Add(type);
            }
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

        protected override void VisitFragmentDefinition(
            FragmentDefinitionNode node,
            object? context)
        {
            INamedType type = _schema!.GetType<INamedType>(
                node.TypeCondition.Name.Value);

            _typeContext.Push(type);

            base.VisitFragmentDefinition(node, context);

            _typeContext.Pop();
        }
    }
}
