using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    internal sealed class TypeReferenceRewriter
        : SchemaSyntaxRewriter<TypeReferenceRewriter.TypeReferenceContext>
    {
        public DocumentNode RewriteSchema(
            DocumentNode document,
            NameString schemaName)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            var names = new Dictionary<NameString, NameString>();

            foreach (ITypeDefinitionNode type in document.Definitions
                .OfType<ITypeDefinitionNode>())
            {
                NameString originalName = type.GetOriginalName(schemaName);
                if (!originalName.Equals(type.Name.Value))
                {
                    names[originalName] = type.Name.Value;
                }
            }

            var context = new TypeReferenceContext(schemaName, names);
            return RewriteDocument(document, context);
        }

        protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            TypeReferenceContext context)
        {
            if (IsRelevant(node, context))
            {
                return base.RewriteObjectTypeDefinition(node, context);
            }

            return node;
        }


        protected override InterfaceTypeDefinitionNode
            RewriteInterfaceTypeDefinition(
                InterfaceTypeDefinitionNode node,
                TypeReferenceContext context)
        {
            if (IsRelevant(node, context))
            {
                return base.RewriteInterfaceTypeDefinition(node, context);
            }

            return node;
        }

        protected override UnionTypeDefinitionNode RewriteUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            TypeReferenceContext context)
        {
            if (IsRelevant(node, context))
            {
                return base.RewriteUnionTypeDefinition(node, context);
            }

            return node;
        }

        protected override InputObjectTypeDefinitionNode
            RewriteInputObjectTypeDefinition(
                InputObjectTypeDefinitionNode node,
                TypeReferenceContext context)
        {
            if (IsRelevant(node, context))
            {
                return base.RewriteInputObjectTypeDefinition(node, context);
            }

            return node;
        }

        protected override NamedTypeNode RewriteNamedType(
            NamedTypeNode node,
            TypeReferenceContext context)
        {
            if (context.Names.TryGetValue(node.Name.Value, out NameString newName))
            {
                return node.WithName(node.Name.WithValue(newName));
            }
            return node;
        }

        private static bool IsRelevant(
            ITypeDefinitionNode typeDefinition,
            TypeReferenceContext context)
        {
            return !context.TargetSchema.HasValue
                || typeDefinition.IsFromSchema(context.TargetSchema.Value);
        }

        public sealed class TypeReferenceContext
        {
            public TypeReferenceContext(
                NameString? targetSchema,
                IReadOnlyDictionary<NameString, NameString> names)
            {
                TargetSchema = targetSchema
                    ?? throw new ArgumentNullException(nameof(targetSchema));
                Names = names
                    ?? throw new ArgumentNullException(nameof(names));
            }

            public NameString? TargetSchema { get; }

            public IReadOnlyDictionary<NameString, NameString> Names { get; }
        }
    }
}
