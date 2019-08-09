using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
{
    internal sealed class TypeReferenceRewriter
        : SchemaSyntaxRewriter<TypeReferenceRewriter.Context>
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

            Dictionary<FieldDefinitionNode, NameString> fieldsToRename =
                GetFieldsToRename(document, schemaName);

            var context = new Context(
                schemaName, renamedTypes, fieldsToRename);

            return RewriteDocument(document, context);
        }

        private static Dictionary<NameString, NameString> GetRenamedTypes(
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

        private static Dictionary<FieldDefinitionNode, NameString>
            GetFieldsToRename(
                DocumentNode document,
                NameString schemaName)
        {
            var fieldsToRename =
                new Dictionary<FieldDefinitionNode, NameString>();

            var types = document.Definitions
                .OfType<ComplexTypeDefinitionNodeBase>()
                .Where(t => t.IsFromSchema(schemaName))
                .ToDictionary(t => t.GetOriginalName(schemaName));

            var queue = new Queue<NameString>(types.Keys);

            var context = new RenameFieldsContext(
                types, fieldsToRename, schemaName);

            while (queue.Count > 0)
            {
                string name = queue.Dequeue();

                switch (types[name])
                {
                    case ObjectTypeDefinitionNode objectType:
                        RenameObjectField(objectType, context);
                        break;
                    case InterfaceTypeDefinitionNode interfaceType:
                        RenameInterfaceField(interfaceType, context);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            return fieldsToRename;
        }

        private static void RenameObjectField(
            ObjectTypeDefinitionNode objectType,
            RenameFieldsContext renameContext)
        {
            IReadOnlyCollection<InterfaceTypeDefinitionNode> interfaceTypes =
                GetInterfaceTypes(objectType, renameContext.Types);

            foreach (FieldDefinitionNode fieldDefinition in
                objectType.Fields)
            {
                NameString originalName =
                    fieldDefinition.GetOriginalName(renameContext.SchemaName);
                if (!originalName.Equals(fieldDefinition.Name.Value))
                {
                    foreach (InterfaceTypeDefinitionNode interfaceType in
                        GetInterfacesThatProvideFieldDefinition(
                            originalName, interfaceTypes))
                    {
                        RenameInterfaceField(interfaceType,
                            renameContext, originalName,
                            fieldDefinition.Name.Value);
                    }
                }
            }
        }

        private static IReadOnlyCollection<InterfaceTypeDefinitionNode>
            GetInterfaceTypes(
                ObjectTypeDefinitionNode objectType,
                IDictionary<NameString, ComplexTypeDefinitionNodeBase> types)
        {
            var interfaceTypes = new List<InterfaceTypeDefinitionNode>();

            foreach (NamedTypeNode namedType in objectType.Interfaces)
            {
                if (types.TryGetValue(namedType.Name.Value,
                    out ComplexTypeDefinitionNodeBase ct)
                    && ct is InterfaceTypeDefinitionNode it)
                {
                    interfaceTypes.Add(it);
                }
            }

            return interfaceTypes;
        }

        private static IReadOnlyCollection<InterfaceTypeDefinitionNode>
            GetInterfacesThatProvideFieldDefinition(
                NameString originalFieldName,
                IEnumerable<InterfaceTypeDefinitionNode> interfaceTypes)
        {
            var items = new List<InterfaceTypeDefinitionNode>();

            foreach (InterfaceTypeDefinitionNode interfaceType in
                interfaceTypes)
            {
                if (interfaceType.Fields.Any(t =>
                    originalFieldName.Equals(t.Name.Value)))
                {
                    items.Add(interfaceType);
                }
            }

            return items;
        }

        private static void RenameInterfaceField(
            InterfaceTypeDefinitionNode interfaceType,
            RenameFieldsContext renameContext)
        {
            foreach (FieldDefinitionNode fieldDefinition in
                interfaceType.Fields)
            {
                NameString originalName =
                    fieldDefinition.GetOriginalName(renameContext.SchemaName);
                if (!originalName.Equals(fieldDefinition.Name.Value))
                {
                    RenameInterfaceField(
                        interfaceType, renameContext,
                        originalName, fieldDefinition.Name.Value);
                }
            }
        }

        private static void RenameInterfaceField(
            InterfaceTypeDefinitionNode interfaceType,
            RenameFieldsContext renameContext,
            NameString originalFieldName,
            NameString newFieldName)
        {
            List<ObjectTypeDefinitionNode> objectTypes =
                renameContext.Types.Values
                    .OfType<ObjectTypeDefinitionNode>()
                    .Where(t => t.Interfaces.Select(i => i.Name.Value)
                        .Any(n => string.Equals(n,
                            interfaceType.Name.Value,
                            StringComparison.Ordinal)))
                    .ToList();

            AddNewFieldName(interfaceType, renameContext,
                originalFieldName, newFieldName);

            foreach (ObjectTypeDefinitionNode objectType in objectTypes)
            {
                AddNewFieldName(objectType, renameContext,
                    originalFieldName, newFieldName);
            }
        }

        private static void AddNewFieldName(
            ComplexTypeDefinitionNodeBase type,
            RenameFieldsContext renameContext,
            NameString originalFieldName,
            NameString newFieldName)
        {
            FieldDefinitionNode fieldDefinition = type.Fields.FirstOrDefault(
                t => originalFieldName.Equals(t.GetOriginalName(
                    renameContext.SchemaName)));
            if (fieldDefinition != null)
            {
                renameContext.RenamedFields[fieldDefinition] = newFieldName;
            }
        }

        protected override ObjectTypeDefinitionNode RewriteObjectTypeDefinition(
            ObjectTypeDefinitionNode node,
            Context context)
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
                Context context)
        {
            if (IsRelevant(node, context))
            {
                return base.RewriteInterfaceTypeDefinition(node, context);
            }

            return node;
        }

        protected override UnionTypeDefinitionNode RewriteUnionTypeDefinition(
            UnionTypeDefinitionNode node,
            Context context)
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
                Context context)
        {
            if (IsRelevant(node, context))
            {
                return base.RewriteInputObjectTypeDefinition(node, context);
            }

            return node;
        }

        protected override NamedTypeNode RewriteNamedType(
            NamedTypeNode node,
            Context context)
        {
            if (context.Names.TryGetValue(node.Name.Value,
                out NameString newName))
            {
                return node.WithName(node.Name.WithValue(newName));
            }
            return node;
        }

        protected override FieldDefinitionNode RewriteFieldDefinition(
            FieldDefinitionNode node,
            Context context)
        {
            FieldDefinitionNode current = node;

            if (context.FieldNames.TryGetValue(current, out NameString newName))
            {
                current = current.Rename(newName, context.SourceSchema.Value);
            }

            return base.RewriteFieldDefinition(current, context);
        }

        private static bool IsRelevant(
            NamedSyntaxNode typeDefinition,
            Context context)
        {
            return !context.SourceSchema.HasValue
                || typeDefinition.IsFromSchema(context.SourceSchema.Value);
        }

        public sealed class Context
        {
            public Context(
                NameString? sourceSchema,
                IReadOnlyDictionary<NameString, NameString> names,
                IReadOnlyDictionary<FieldDefinitionNode, NameString> fieldNames)
            {
                SourceSchema = sourceSchema
                    ?? throw new ArgumentNullException(nameof(sourceSchema));
                Names = names
                    ?? throw new ArgumentNullException(nameof(names));
                FieldNames = fieldNames
                    ?? throw new ArgumentNullException(nameof(fieldNames));
            }

            public NameString? SourceSchema { get; }

            public IReadOnlyDictionary<NameString, NameString> Names { get; }

            public IReadOnlyDictionary<FieldDefinitionNode, NameString>
            FieldNames
            { get; }
        }

        private class RenameFieldsContext
        {
            public RenameFieldsContext(
                IDictionary<NameString, ComplexTypeDefinitionNodeBase> types,
                IDictionary<FieldDefinitionNode, NameString> renamedFields,
                NameString schemaName)
            {
                Types = types;
                RenamedFields = renamedFields;
                SchemaName = schemaName;
            }

            public IDictionary<NameString, ComplexTypeDefinitionNodeBase> Types
            { get; }

            public IDictionary<FieldDefinitionNode, NameString> RenamedFields
            { get; }

            public NameString SchemaName
            { get; }
        }
    }
}
