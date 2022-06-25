using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;

public sealed class ApplyExtensionsMiddleware
{
    private const string _schema = "$schema";

    private readonly IApplyExtension[] _applyExtensions =
    {
        new ApplyObjectTypeExtension(),
        new ApplyInterfaceTypeExtension(),
    };
    private readonly MergeSchema _next;

    public ApplyExtensionsMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        var definitions = new Dictionary<string, ITypeSystemDefinitionNode>();
        var extensions = new List<ITypeSystemExtensionNode>();

        for (var i = 0; i < context.Documents.Count; i++)
        {
            Document document = context.Documents[i];
            CollectTypeDefinitions(definitions, extensions, document.SyntaxTree);
            CollectTypeExtensions(extensions, document.SyntaxTree);

            DocumentNode rewritten = ApplyExtensions(definitions, extensions);
            document = new Document(document.Name, rewritten);
            context.Documents = context.Documents.SetItem(i, document);

            definitions.Clear();
            extensions.Clear();
        }

        await _next(context);
    }

    private static void CollectTypeDefinitions(
        Dictionary<string, ITypeSystemDefinitionNode> definitions,
        List<ITypeSystemExtensionNode> extensions,
        DocumentNode document)
    {
        foreach (IDefinitionNode definition in document.Definitions)
        {
            if (definition is ITypeSystemDefinitionNode typeDef)
            {
                var name = GetName(typeDef);
                if (definitions.ContainsKey(name))
                {
                    // Directive definitions have no extensions syntax,
                    // so if find a second directive with the same name we will just drop it.
                    if (definition.Kind is not SyntaxKind.DirectiveDefinition)
                    {
                        extensions.Add(ConvertToExtension(typeDef));
                    }
                }
                else
                {
                    definitions.Add(name, typeDef);
                }
            }
        }
    }

    private static void CollectTypeExtensions(
        List<ITypeSystemExtensionNode> extensions,
        DocumentNode document)
    {
        foreach (IDefinitionNode definition in document.Definitions)
        {
            if (definition is ITypeSystemExtensionNode typeExt)
            {
                extensions.Add(typeExt);
            }
        }
    }

    private DocumentNode ApplyExtensions(
        Dictionary<string, ITypeSystemDefinitionNode> definitions,
        List<ITypeSystemExtensionNode> extensions)
    {
        var preserved = new List<ITypeSystemExtensionNode>();

        foreach (ITypeSystemExtensionNode extension in extensions)
        {
            if (extension is SchemaExtensionNode schemaExt)
            {
                ApplyExtensions(definitions, schemaExt);
            }
            else if (extension is ITypeExtensionNode typeExt)
            {
                // We try to apply the type extension. If we do not find a definition that we
                // can apply it to we will preserve the extension for latter processing steps.
                if (!TryApplyExtensions(definitions, typeExt))
                {
                    preserved.Add(typeExt);
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(extensions), extension, null);
            }
        }

        // We create a single document per service where the extensions are at the top.
        var definitionList = new List<IDefinitionNode>();
        definitionList.AddRange(preserved.OrderBy(t => ((IHasName)t).Name.Value));
        definitionList.AddRange(definitions.OrderBy(t => t.Key).Select(t => t.Value));
        return new DocumentNode(definitionList);
    }

    private static void ApplyExtensions(
        Dictionary<string, ITypeSystemDefinitionNode> definitions,
        SchemaExtensionNode extension)
    {
        if (definitions.TryGetValue(_schema, out ITypeSystemDefinitionNode? def) &&
            def is SchemaDefinitionNode schemaDef)
        {
            var directives = schemaDef.Directives.ToList();
            directives.AddRange(extension.Directives);
            definitions[_schema] = schemaDef.WithDirectives(directives);
        }
        else
        {
            definitions[_schema] = new SchemaDefinitionNode(
                null,
                null,
                extension.Directives,
                extension.OperationTypes);
        }
    }

    private bool TryApplyExtensions(
        Dictionary<string, ITypeSystemDefinitionNode> definitions,
        ITypeExtensionNode extension)
    {
        if (definitions.TryGetValue(extension.Name.Value, out ITypeSystemDefinitionNode? node) &&
            node is ITypeDefinitionNode typeDef)
        {
            for (var i = 0; i < _applyExtensions.Length; i++)
            {
                ITypeDefinitionNode? merged = _applyExtensions[i].TryApply(typeDef, extension);

                if (merged is not null)
                {
                    definitions[extension.Name.Value] = merged;
                    return true;
                }
            }
        }

        return false;
    }

    private static ITypeSystemExtensionNode ConvertToExtension(IDefinitionNode typeDef)
        => typeDef switch
        {
            ObjectTypeDefinitionNode objectTypeDef => ConvertToExtension(objectTypeDef),
            InterfaceTypeDefinitionNode interfaceTypeDef => ConvertToExtension(interfaceTypeDef),
            UnionTypeDefinitionNode unionTypeDef => ConvertToExtension(unionTypeDef),
            InputObjectTypeDefinitionNode inputTypeDef => ConvertToExtension(inputTypeDef),
            EnumTypeDefinitionNode enumTypeDef => ConvertToExtension(enumTypeDef),
            ScalarTypeDefinitionNode scalarTypeDef => ConvertToExtension(scalarTypeDef),
            SchemaDefinitionNode schemaDef => ConvertToExtension(schemaDef),
            _ => throw new ArgumentOutOfRangeException(nameof(typeDef), typeDef, null)
        };

    private static ObjectTypeExtensionNode ConvertToExtension(
        ObjectTypeDefinitionNode typeDef)
        => new ObjectTypeExtensionNode(
            typeDef.Location,
            typeDef.Name,
            typeDef.Directives,
            typeDef.Interfaces,
            typeDef.Fields);

    private static InterfaceTypeExtensionNode ConvertToExtension(
        InterfaceTypeDefinitionNode typeDef)
        => new InterfaceTypeExtensionNode(
            typeDef.Location,
            typeDef.Name,
            typeDef.Directives,
            typeDef.Interfaces,
            typeDef.Fields);

    private static UnionTypeExtensionNode ConvertToExtension(
        UnionTypeDefinitionNode typeDef)
        => new UnionTypeExtensionNode(
            typeDef.Location,
            typeDef.Name,
            typeDef.Directives,
            typeDef.Types);

    private static InputObjectTypeExtensionNode ConvertToExtension(
        InputObjectTypeDefinitionNode typeDef)
        => new InputObjectTypeExtensionNode(
            typeDef.Location,
            typeDef.Name,
            typeDef.Directives,
            typeDef.Fields);

    private static EnumTypeExtensionNode ConvertToExtension(
        EnumTypeDefinitionNode typeDef)
        => new EnumTypeExtensionNode(
            typeDef.Location,
            typeDef.Name,
            typeDef.Directives,
            typeDef.Values);

    private static ScalarTypeExtensionNode ConvertToExtension(
        ScalarTypeDefinitionNode typeDef)
        => new ScalarTypeExtensionNode(
            typeDef.Location,
            typeDef.Name,
            typeDef.Directives);

    private static SchemaExtensionNode ConvertToExtension(
        SchemaDefinitionNode schemaDef)
        => new SchemaExtensionNode(
            schemaDef.Location,
            schemaDef.Directives,
            schemaDef.OperationTypes);

    private static string GetName(IDefinitionNode definition)
        => definition switch
        {
            IHasName typeDef => typeDef.Name.Value,
            SchemaDefinitionNode => _schema,
            SchemaExtensionNode => _schema,
            _ => throw new ArgumentOutOfRangeException(nameof(definition), definition, null)
        };
}
