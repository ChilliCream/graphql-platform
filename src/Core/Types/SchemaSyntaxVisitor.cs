using System;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Factories;

namespace HotChocolate
{
    internal class SchemaSyntaxVisitor
        : SchemaSyntaxWalker<object>
    {
        private readonly ITypeRegistry _typeRegistry;
        private readonly ObjectTypeFactory _objectTypeFactory = new ObjectTypeFactory();
        private readonly InterfaceTypeFactory _interfaceTypeFactory = new InterfaceTypeFactory();
        private readonly UnionTypeFactory _unionTypeFactory = new UnionTypeFactory();
        private readonly InputObjectTypeFactory _inputObjectTypeFactory = new InputObjectTypeFactory();
        private readonly EnumTypeFactory _enumTypeFactory = new EnumTypeFactory();

        public string QueryTypeName { get; private set; }
        public string MutationTypeName { get; private set; }
        public string SubscriptionTypeName { get; private set; }

        public SchemaSyntaxVisitor(ITypeRegistry typeRegistry)
        {
            if (typeRegistry == null)
            {
                throw new ArgumentNullException(nameof(typeRegistry));
            }

            _typeRegistry = typeRegistry;
        }

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            object context)
        {
            _typeRegistry.RegisterType(_objectTypeFactory.Create(node));
        }

        protected override void VisitInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node,
            object context)
        {
            _typeRegistry.RegisterType(_interfaceTypeFactory.Create(node));
        }

        protected override void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            object context)
        {
            _typeRegistry.RegisterType(_unionTypeFactory.Create(node));
        }

        protected override void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node,
            object context)
        {
            _typeRegistry.RegisterType(_inputObjectTypeFactory.Create(node));
        }

        protected override void VisitEnumTypeDefinition(
            EnumTypeDefinitionNode node,
            object context)
        {
            _typeRegistry.RegisterType(_enumTypeFactory.Create(node));
        }

        protected override void VisitSchemaDefinition(
            SchemaDefinitionNode node,
            object context)
        {
            foreach (OperationTypeDefinitionNode operationType in node.OperationTypes)
            {
                switch (operationType.Operation)
                {
                    case OperationType.Query:
                        QueryTypeName = operationType.Type.Name.Value;
                        break;
                    case OperationType.Mutation:
                        MutationTypeName = operationType.Type.Name.Value;
                        break;
                    case OperationType.Subscription:
                        SubscriptionTypeName = operationType.Type.Name.Value;
                        break;
                    default:
                        throw new InvalidOperationException("Unknown operation type.");
                }
            }
        }
    }
}
