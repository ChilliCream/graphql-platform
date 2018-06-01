using System;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Factories;

namespace HotChocolate
{
    //internal delegate AsyncFieldResolverDelegate FieldResolverFactory(
    //    ObjectType objectType, Field field);

    internal class SchemaSyntaxVisitor
        : SyntaxNodeVisitor
    {
        private readonly ITypeRegistry _typeRegistry;
        private readonly ObjectTypeFactory _objectTypeFactory = new ObjectTypeFactory();
        private readonly InterfaceTypeFactory _interfaceTypeFactory = new InterfaceTypeFactory();
        private readonly UnionTypeFactory _unionTypeFactory = new UnionTypeFactory();
        private readonly InputObjectTypeFactory _inputObjectTypeFactory = new InputObjectTypeFactory();

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

        protected override void VisitDocument(DocumentNode node)
        {
            VisitMany(node.Definitions);
        }

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node)
        {
            _typeRegistry.RegisterType(_objectTypeFactory.Create(node));
        }

        protected override void VisitInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node)
        {
            _typeRegistry.RegisterType(_interfaceTypeFactory.Create(node));
        }

        protected override void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node)
        {
            _typeRegistry.RegisterType(_unionTypeFactory.Create(node));
        }

        protected override void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node)
        {
            _typeRegistry.RegisterType(_inputObjectTypeFactory.Create(node));
        }

        protected override void VisitSchemaDefinition(
            SchemaDefinitionNode node)
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
