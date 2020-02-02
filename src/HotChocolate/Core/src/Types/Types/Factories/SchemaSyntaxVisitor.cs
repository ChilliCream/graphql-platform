using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Factories
{
    internal class SchemaSyntaxVisitor
        : SchemaSyntaxWalker<object>
    {
        private readonly ObjectTypeFactory _objectTypeFactory =
            new ObjectTypeFactory();
        private readonly InterfaceTypeFactory _interfaceTypeFactory =
            new InterfaceTypeFactory();
        private readonly UnionTypeFactory _unionTypeFactory =
            new UnionTypeFactory();
        private readonly InputObjectTypeFactory _inputObjectTypeFactory =
            new InputObjectTypeFactory();
        private readonly EnumTypeFactory _enumTypeFactory =
            new EnumTypeFactory();
        private readonly DirectiveTypeFactory _directiveTypeFactory =
            new DirectiveTypeFactory();

        private readonly List<ITypeReference> _types =
            new List<ITypeReference>();

        private readonly IBindingLookup _bindingLookup;

        public SchemaSyntaxVisitor(IBindingLookup bindingLookup)
        {
            _bindingLookup = bindingLookup
                ?? throw new ArgumentNullException(nameof(bindingLookup));
        }

        public string QueryTypeName { get; private set; }

        public string MutationTypeName { get; private set; }

        public string SubscriptionTypeName { get; private set; }

        public string Description { get; private set; }

        public IReadOnlyCollection<DirectiveNode> Directives
        { get; private set; }

        public IReadOnlyList<ITypeReference> Types => _types;

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            object context)
        {
            _types.Add(SchemaTypeReference.Create(
                _objectTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node,
            object context)
        {
            _types.Add(SchemaTypeReference.Create(
                _interfaceTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            object context)
        {
            _types.Add(SchemaTypeReference.Create(
                _unionTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node,
            object context)
        {
            _types.Add(SchemaTypeReference.Create(
                _inputObjectTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitEnumTypeDefinition(
            EnumTypeDefinitionNode node,
            object context)
        {
            _types.Add(SchemaTypeReference.Create(
                _enumTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitDirectiveDefinition(
            DirectiveDefinitionNode node,
            object context)
        {
            _types.Add(SchemaTypeReference.Create(
                _directiveTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitSchemaDefinition(
            SchemaDefinitionNode node,
            object context)
        {
            Description = node.Description?.Value;
            Directives = node.Directives;

            foreach (OperationTypeDefinitionNode operationType in
                node.OperationTypes)
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
                        throw new InvalidOperationException(
                            TypeResources.SchemaSyntaxVisitor_UnknownOperationType);
                }
            }
        }
    }
}
