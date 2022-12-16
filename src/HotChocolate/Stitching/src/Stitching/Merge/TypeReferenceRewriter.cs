using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge;

internal sealed class TypeReferenceRewriter : SyntaxRewriter<TypeReferenceRewriter.Context>
{
    public DocumentNode? RewriteSchema(
        DocumentNode document,
        string schemaName)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        IReadOnlyDictionary<string, string> renamedTypes =
            GetRenamedTypes(document, schemaName);

        var fieldsToRename =
            GetFieldsToRename(document, schemaName);

        var context = new Context(
            schemaName, renamedTypes, fieldsToRename);

        return RewriteDocument(document, context);
    }

    private static Dictionary<string, string> GetRenamedTypes(
        DocumentNode document,
        string schemaName)
    {
        var names = new Dictionary<string, string>();

        foreach (var type in document.Definitions
            .OfType<NamedSyntaxNode>())
        {
            var originalName = type.GetOriginalName(schemaName);
            if (!originalName.Equals(type.Name.Value))
            {
                names[originalName] = type.Name.Value;
            }
        }

        return names;
    }

    private static Dictionary<FieldDefinitionNode, string> GetFieldsToRename(
        DocumentNode document,
        string schemaName)
    {
        var fieldsToRename =
            new Dictionary<FieldDefinitionNode, string>();

        var types = document.Definitions
            .OfType<ComplexTypeDefinitionNodeBase>()
            .Where(t => t.IsFromSchema(schemaName))
            .ToDictionary(t => t.GetOriginalName(schemaName));

        var queue = new Queue<string>(types.Keys);

        var context = new RenameFieldsContext(
            types, fieldsToRename, schemaName);

        while (queue.Count > 0)
        {
            var name = queue.Dequeue();

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
        var interfaceTypes =
            GetInterfaceTypes(objectType, renameContext.Types);

        foreach (var fieldDefinition in
            objectType.Fields)
        {
            var originalName =
                fieldDefinition.GetOriginalName(renameContext.SchemaName);
            if (!originalName.Equals(fieldDefinition.Name.Value))
            {
                foreach (var interfaceType in
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

    private static IReadOnlyCollection<InterfaceTypeDefinitionNode> GetInterfaceTypes(
        ObjectTypeDefinitionNode objectType,
        IDictionary<string, ComplexTypeDefinitionNodeBase> types)
    {
        var interfaceTypes = new List<InterfaceTypeDefinitionNode>();

        foreach (var namedType in objectType.Interfaces)
        {
            if (types.TryGetValue(namedType.Name.Value,
                    out var ct)
                && ct is InterfaceTypeDefinitionNode it)
            {
                interfaceTypes.Add(it);
            }
        }

        return interfaceTypes;
    }

    private static IReadOnlyCollection<InterfaceTypeDefinitionNode>
        GetInterfacesThatProvideFieldDefinition(
            string originalFieldName,
            IEnumerable<InterfaceTypeDefinitionNode> interfaceTypes)
    {
        var items = new List<InterfaceTypeDefinitionNode>();

        foreach (var interfaceType in
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
        foreach (var fieldDefinition in
            interfaceType.Fields)
        {
            var originalName = fieldDefinition.GetOriginalName(renameContext.SchemaName);
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
        string originalFieldName,
        string newFieldName)
    {
        var objectTypes =
            renameContext.Types.Values
                .OfType<ObjectTypeDefinitionNode>()
                .Where(t => t.Interfaces.Select(i => i.Name.Value)
                    .Any(n => string.Equals(n,
                        interfaceType.Name.Value,
                        StringComparison.Ordinal)))
                .ToList();

        AddNewFieldName(interfaceType, renameContext,
            originalFieldName, newFieldName);

        foreach (var objectType in objectTypes)
        {
            AddNewFieldName(objectType, renameContext,
                originalFieldName, newFieldName);
        }
    }

    private static void AddNewFieldName(
        ComplexTypeDefinitionNodeBase type,
        RenameFieldsContext renameContext,
        string originalFieldName,
        string newFieldName)
    {
        var fieldDefinition = type.Fields.FirstOrDefault(
            t => originalFieldName.Equals(t.GetOriginalName(
                renameContext.SchemaName)));
        if (fieldDefinition != null)
        {
            renameContext.RenamedFields[fieldDefinition] = newFieldName;
        }
    }

    protected override ObjectTypeDefinitionNode? RewriteObjectTypeDefinition(
        ObjectTypeDefinitionNode node,
        Context context)
    {
        if (IsRelevant(node, context))
        {
            return base.RewriteObjectTypeDefinition(node, context);
        }

        return node;
    }


    protected override InterfaceTypeDefinitionNode?
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
            out var newName))
        {
            return node.WithName(node.Name.WithValue(newName));
        }
        return node;
    }

    protected override FieldDefinitionNode? RewriteFieldDefinition(
        FieldDefinitionNode node,
        Context context)
    {
        var current = node;

        if (context.FieldNames.TryGetValue(current, out var newName))
        {
            current = current.Rename(newName, context.SourceSchema);
        }

        return base.RewriteFieldDefinition(current, context);
    }

    private static bool IsRelevant(
        NamedSyntaxNode typeDefinition,
        Context context)
    {
        return string.IsNullOrEmpty(context.SourceSchema)
            || typeDefinition.IsFromSchema(context.SourceSchema);
    }

    public sealed class Context : ISyntaxVisitorContext
    {
        public Context(
            string? sourceSchema,
            IReadOnlyDictionary<string, string> names,
            IReadOnlyDictionary<FieldDefinitionNode, string> fieldNames)
        {
            SourceSchema = sourceSchema
                ?? throw new ArgumentNullException(nameof(sourceSchema));
            Names = names
                ?? throw new ArgumentNullException(nameof(names));
            FieldNames = fieldNames
                ?? throw new ArgumentNullException(nameof(fieldNames));
        }

        public string? SourceSchema { get; }

        public IReadOnlyDictionary<string, string> Names { get; }

        public IReadOnlyDictionary<FieldDefinitionNode, string>
            FieldNames
        { get; }
    }

    private class RenameFieldsContext
    {
        public RenameFieldsContext(
            IDictionary<string, ComplexTypeDefinitionNodeBase> types,
            IDictionary<FieldDefinitionNode, string> renamedFields,
            string schemaName)
        {
            Types = types;
            RenamedFields = renamedFields;
            SchemaName = schemaName;
        }

        public IDictionary<string, ComplexTypeDefinitionNodeBase> Types { get; }

        public IDictionary<FieldDefinitionNode, string> RenamedFields { get; }

        public string SchemaName { get; }
    }
}
