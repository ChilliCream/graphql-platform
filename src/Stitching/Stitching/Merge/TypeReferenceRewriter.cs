using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Resolvers;

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

            IReadOnlyDictionary<NameString, NameString> renamedTypes =
                GetRenamedTypes(document, schemaName);

            var context = new TypeReferenceContext(schemaName, renamedTypes);
            return RewriteDocument(document, context);
        }

        private Dictionary<NameString, NameString> GetRenamedTypes(
            DocumentNode document,
            NameString schemaName)
        {
            var names = new Dictionary<NameString, NameString>();

            foreach (NamedSyntaxNode type in document.Definitions
                .OfType<NamedSyntaxNode>())
            {
                NameString originalName = type.GetOriginalName(schemaName);
                if (!originalName.Equals(type.Name.Value))
                {
                    names[originalName] = type.Name.Value;
                }
            }

            return names;
        }

        private Dictionary<FieldReference, NameString> GetRenamedFields(
            DocumentNode document,
            NameString schemaName)
        {
            DocumentNode current = document;
            var others = new List<IDefinitionNode>(current.Definitions
                .Where(t => !(t is ComplexTypeDefinitionNodeBase)));
            var complexTypes = document.Definitions
                .OfType<ComplexTypeDefinitionNodeBase>()
                .ToDictionary(t => t.Name.Value);
            var queue = new Queue<string>(complexTypes.Keys);

            while (queue.Count > 0)
            {
                string name = queue.Dequeue();

                switch (complexTypes[name])
                {
                    case ObjectTypeDefinitionNode objectType:
                        break;
                    case InterfaceTypeDefinitionNode interfaceType:
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            throw new NotImplementedException();
        }

        private static void RenameInterfaceField(
            InterfaceTypeDefinitionNode interfaceType,
            IDictionary<string, ComplexTypeDefinitionNodeBase> types,
            IDictionary<FieldDefinitionNode, NameString> renamedFields,
            NameString schemaName)
        {
            foreach (FieldDefinitionNode fieldDefinition in
                interfaceType.Fields)
            {
                NameString originalName =
                    fieldDefinition.GetOriginalName(schemaName);
                if (!originalName.Equals(fieldDefinition.Name.Value))
                {

                }
            }
        }


        private static void RenameInterfaceField(
            InterfaceTypeDefinitionNode interfaceType,
            IDictionary<string, ComplexTypeDefinitionNodeBase> types,
            IDictionary<FieldDefinitionNode, NameString> renamedFields,
            NameString schemaName,
            NameString originalFieldName,
            NameString newFieldName)
        {
            List<ObjectTypeDefinitionNode> objectTypes = types.Values
                .OfType<ObjectTypeDefinitionNode>()
                .Where(t => t.Interfaces.Select(i => i.Name.Value)
                    .Any(n => string.Equals(n,
                        interfaceType.Name.Value,
                        StringComparison.Ordinal)))
                .ToList();

            AddNewFieldName(interfaceType, renamedFields,
                schemaName, originalFieldName, newFieldName);

            foreach (ObjectTypeDefinitionNode objectType in objectTypes)
            {
                AddNewFieldName(objectType, renamedFields,
                    schemaName, originalFieldName, newFieldName);
            }
        }

        private static void AddNewFieldName(
            ComplexTypeDefinitionNodeBase type,
            IDictionary<FieldDefinitionNode, NameString> renamedFields,
            NameString schemaName,
            NameString originalFieldName,
            NameString newFieldName)
        {
            FieldDefinitionNode fieldDefinition = type.Fields.FirstOrDefault(
                t => originalFieldName.Equals(t.GetOriginalName(schemaName)));
            if (fieldDefinition != null)
            {
                renamedFields[fieldDefinition] = newFieldName;
            }
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
            NamedSyntaxNode typeDefinition,
            TypeReferenceContext context)
        {
            return !context.SourceSchema.HasValue
                || typeDefinition.IsFromSchema(context.SourceSchema.Value);
        }

        public sealed class TypeReferenceContext
        {
            public TypeReferenceContext(
                NameString? sourceSchema,
                IReadOnlyDictionary<NameString, NameString> names)
            {
                SourceSchema = sourceSchema
                    ?? throw new ArgumentNullException(nameof(sourceSchema));
                Names = names
                    ?? throw new ArgumentNullException(nameof(names));
            }

            public NameString? SourceSchema { get; }

            public IReadOnlyDictionary<NameString, NameString> Names { get; }
        }


    }
}
