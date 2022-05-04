using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Pipeline;

public class ApplyExtensionsMiddleware
{
    private readonly MergeSchema _next;

    public ApplyExtensionsMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        var definitions = new Dictionary<string, ITypeDefinitionNode>();
        var extensions = new List<ITypeExtensionNode>();

        foreach (ServiceConfiguration configuration in context.Configurations)
        {
            foreach (DocumentNode document in configuration.Documents)
            {
                CollectTypeDefinitions(definitions, extensions, document);
                CollectTypeExtensions(extensions, document);
                DocumentNode subgraph = ApplyExtensions(definitions, extensions);
                context.Documents = context.Documents.Add(subgraph);
            }
        }

        await _next(context);
    }

    private void CollectTypeDefinitions(
        Dictionary<string, ITypeDefinitionNode> definitions,
        List<ITypeExtensionNode> extensions,
        DocumentNode document)
    {
        foreach (IDefinitionNode definition in document.Definitions)
        {
            if (definition is ITypeDefinitionNode typeDef)
            {
                if (definitions.ContainsKey(typeDef.Name.Value))
                {
                    extensions.Add(ConvertToExtension(typeDef));
                }
                else
                {
                    definitions.Add(typeDef.Name.Value, typeDef);
                }
            }
        }
    }

    private void CollectTypeExtensions(
        List<ITypeExtensionNode> extensions,
        DocumentNode document)
    {
        foreach (IDefinitionNode definition in document.Definitions)
        {
            if (definition is ITypeExtensionNode typeExt)
            {
                extensions.Add(typeExt);
            }
        }
    }

    private DocumentNode ApplyExtensions(
        Dictionary<string, ITypeDefinitionNode> definitions,
        List<ITypeExtensionNode> extensions)
    {

    }

    private static ITypeExtensionNode ConvertToExtension(ITypeDefinitionNode typeDef)
        => typeDef switch
        {
            ObjectTypeDefinitionNode objectTypeDef => ConvertToExtension(objectTypeDef),
            InterfaceTypeDefinitionNode objectTypeDef => ConvertToExtension(objectTypeDef),
            UnionTypeDefinitionNode objectTypeDef => ConvertToExtension(objectTypeDef),
            InputObjectTypeDefinitionNode objectTypeDef => ConvertToExtension(objectTypeDef),
            EnumTypeDefinitionNode objectTypeDef => ConvertToExtension(objectTypeDef),
            ScalarTypeDefinitionNode objectTypeDef => ConvertToExtension(objectTypeDef),
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
            typeDef.Directives;
}
