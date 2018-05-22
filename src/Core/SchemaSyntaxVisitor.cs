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
    }
}
