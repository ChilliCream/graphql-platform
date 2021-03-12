using System;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types.Factories
{
    internal sealed class SchemaSyntaxVisitor
        : SyntaxVisitor<SchemaSyntaxVisitorContext>
    {
        private static readonly ObjectTypeFactory _objectTypeFactory = new();
        private static readonly ObjectTypeExtensionFactory _objectTypeExtensionFactory = new();
        private static readonly InterfaceTypeFactory _interfaceTypeFactory = new();
        private static readonly InterfaceTypeExtensionFactory
            _interfaceTypeExtensionFactory = new();
        private static readonly UnionTypeFactory _unionTypeFactory = new();
        private static readonly UnionTypeExtensionFactory _unionTypeExtensionFactory = new();
        private static readonly InputObjectTypeFactory _inputObjectTypeFactory = new();
        private static readonly InputObjectTypeExtensionFactory _inputObjectTypeExtensionFactory =
            new();
        private static readonly EnumTypeFactory _enumTypeFactory = new();
        private static readonly EnumTypeExtensionFactory _enumTypeExtensionFactory = new();
        private static readonly DirectiveTypeFactory _directiveTypeFactory = new();

        protected override ISyntaxVisitorAction DefaultAction => Continue;

        protected override ISyntaxVisitorAction VisitChildren(
            ObjectTypeDefinitionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _objectTypeFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            ObjectTypeExtensionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _objectTypeExtensionFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            InterfaceTypeDefinitionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _interfaceTypeFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            InterfaceTypeExtensionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _interfaceTypeExtensionFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            UnionTypeDefinitionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _unionTypeFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            UnionTypeExtensionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _unionTypeExtensionFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            InputObjectTypeDefinitionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _inputObjectTypeFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            InputObjectTypeExtensionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _inputObjectTypeExtensionFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            EnumTypeDefinitionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _enumTypeFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            EnumTypeExtensionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _enumTypeExtensionFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            DirectiveDefinitionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Types.Add(
                TypeReference.Create(
                    _directiveTypeFactory.Create(
                        context.BindingLookup,
                        context.SchemaOptions,
                        node)));

            return base.VisitChildren(node, context);
        }

        protected override ISyntaxVisitorAction VisitChildren(
            SchemaDefinitionNode node,
            SchemaSyntaxVisitorContext context)
        {
            context.Description = node.Description?.Value;
            context.Directives = node.Directives;

            foreach (OperationTypeDefinitionNode operationType in
                node.OperationTypes)
            {
                switch (operationType.Operation)
                {
                    case OperationType.Query:
                        context.QueryTypeName = operationType.Type.Name.Value;
                        break;

                    case OperationType.Mutation:
                        context.MutationTypeName = operationType.Type.Name.Value;
                        break;

                    case OperationType.Subscription:
                        context.SubscriptionTypeName = operationType.Type.Name.Value;
                        break;

                    default:
                        throw new InvalidOperationException(
                            TypeResources.SchemaSyntaxVisitor_UnknownOperationType);
                }
            }

            return base.VisitChildren(node, context);
        }
    }
}
