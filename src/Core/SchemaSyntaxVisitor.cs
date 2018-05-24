using System;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Factories;

namespace HotChocolate
{
    internal delegate AsyncFieldResolverDelegate FieldResolverFactory(
        ObjectType objectType, Field field);

    internal class SchemaSyntaxVisitor
        : SyntaxNodeVisitor
    {
        private readonly SchemaContext _context;
        private readonly ObjectTypeFactory _objectTypeFactory = new ObjectTypeFactory();
        private readonly InterfaceTypeFactory _interfaceTypeFactory = new InterfaceTypeFactory();
        private readonly UnionTypeFactory _unionTypeFactory = new UnionTypeFactory();
        private readonly InputObjectTypeFactory _inputObjectTypeFactory = new InputObjectTypeFactory();

        public SchemaSyntaxVisitor(SchemaContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            _context = context;
        }

        protected override void VisitDocument(DocumentNode node)
        {
            VisitMany(node.Definitions);
        }

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node)
        {
            _context.RegisterType(_objectTypeFactory.Create(_context, node));
        }

        protected override void VisitInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node)
        {
            _context.RegisterType(_interfaceTypeFactory.Create(_context, node));
        }

        protected override void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node)
        {
            _context.RegisterType(_unionTypeFactory.Create(_context, node));
        }

        protected override void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node)
        {
            _context.RegisterType(_inputObjectTypeFactory.Create(_context, node));
        }

        protected override void VisitSchemaDefinition(
            SchemaDefinitionNode node)
        {
            foreach (OperationTypeDefinitionNode operationType in node.OperationTypes)
            {
                switch (operationType.Operation)
                {
                    case OperationType.Query:
                        _context.QueryTypeName = operationType.Type.Name.Value;
                        break;
                    case OperationType.Mutation:
                        _context.MutationTypeName = operationType.Type.Name.Value;
                        break;
                    case OperationType.Subscription:
                        _context.SubscriptionTypeName = operationType.Type.Name.Value;
                        break;
                    default:
                        throw new InvalidOperationException("Unknown operation type.");
                }
            }
        }
    }
}
