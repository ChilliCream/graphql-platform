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
        private readonly ObjectTypeExtensionFactory _objectTypeExtensionFactory =
            new ObjectTypeExtensionFactory();
        private readonly InterfaceTypeFactory _interfaceTypeFactory =
            new InterfaceTypeFactory();
        private readonly InterfaceTypeExtensionFactory _interfaceTypeExtensionFactory =
            new InterfaceTypeExtensionFactory();
        private readonly UnionTypeFactory _unionTypeFactory =
            new UnionTypeFactory();
        private readonly UnionTypeExtensionFactory _unionTypeExtensionFactory =
            new UnionTypeExtensionFactory();
        private readonly InputObjectTypeFactory _inputObjectTypeFactory =
            new InputObjectTypeFactory();
        private readonly InputObjectTypeExtensionFactory _inputObjectTypeExtensionFactory =
            new InputObjectTypeExtensionFactory();
        private readonly EnumTypeFactory _enumTypeFactory =
            new EnumTypeFactory();
        private readonly EnumTypeExtensionFactory _enumTypeExtensionFactory =
            new EnumTypeExtensionFactory();
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

        public IReadOnlyCollection<DirectiveNode> Directives { get; private set; }

        public IReadOnlyList<ITypeReference> Types => _types;

        protected override void VisitObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
                _objectTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitObjectTypeExtension(
            ObjectTypeExtensionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
                _objectTypeExtensionFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitInterfaceTypeDefinition(
            InterfaceTypeDefinitionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
                _interfaceTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitInterfaceTypeExtension(
            InterfaceTypeExtensionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
                _interfaceTypeExtensionFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
                _unionTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitUnionTypeExtension(
            UnionTypeExtensionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
                _unionTypeExtensionFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitInputObjectTypeDefinition(
            InputObjectTypeDefinitionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
                _inputObjectTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitInputObjectTypeExtension(
            InputObjectTypeExtensionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
                _inputObjectTypeExtensionFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitEnumTypeDefinition(
            EnumTypeDefinitionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
                _enumTypeFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitEnumTypeExtension(
            EnumTypeExtensionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
                _enumTypeExtensionFactory.Create(_bindingLookup, node)));
        }

        protected override void VisitDirectiveDefinition(
            DirectiveDefinitionNode node,
            object context)
        {
            _types.Add(TypeReference.Create(
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
