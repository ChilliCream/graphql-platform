using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Language;
using static HotChocolate.Language.SyntaxComparison;
using static HotChocolate.Stitching.Types.ExceptionHelper;

namespace HotChocolate.Stitching.Types.Pipeline;

public sealed class ApplyExtensionsMiddleware
{
    private const string _schema = "$schema";
    private readonly MergeSchema _next;

    public ApplyExtensionsMiddleware(MergeSchema next)
    {
        _next = next;
    }

    public async ValueTask InvokeAsync(ISchemaMergeContext context)
    {
        var definitions = new Dictionary<string, ITypeSystemDefinitionNode>();
        var extensions = new List<ITypeSystemExtensionNode>();

        foreach (ServiceConfiguration configuration in context.Configurations)
        {
            foreach (DocumentNode document in configuration.Documents)
            {
                CollectTypeDefinitions(definitions, extensions, document);
                CollectTypeExtensions(extensions, document);
            }

            DocumentNode subgraph = ApplyExtensions(definitions, extensions);
            context.Documents = context.Documents.Add(subgraph);
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

    private static DocumentNode ApplyExtensions(
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

    private static bool TryApplyExtensions(
        Dictionary<string, ITypeSystemDefinitionNode> definitions,
        ITypeExtensionNode extension)
    {
        if (definitions.TryGetValue(extension.Name.Value, out ITypeSystemDefinitionNode? typeDef))
        {
            ITypeSystemDefinitionNode definition = extension switch
            {
                ObjectTypeExtensionNode typeExt => ApplyExtensions(
                    (ObjectTypeDefinitionNode)typeDef,
                    typeExt),
                InterfaceTypeExtensionNode typeExt => ApplyExtensions(
                    (InterfaceTypeDefinitionNode)typeDef,
                    typeExt),
                _ => throw new ArgumentOutOfRangeException(nameof(extension), extension, null)
            };

            definitions[extension.Name.Value] = definition;
            return true;
        }

        return false;
    }

    private static ObjectTypeDefinitionNode ApplyExtensions(
        ObjectTypeDefinitionNode definition,
        ObjectTypeExtensionNode extension)
    {
        IReadOnlyList<DirectiveNode> directives = definition.Directives;
        IReadOnlyList<NamedTypeNode> interfaces = definition.Interfaces;
        IReadOnlyList<FieldDefinitionNode> fields = definition.Fields;

        if (extension.Directives.Count > 0)
        {
            var temp = definition.Directives.ToList();
            temp.AddRange(extension.Directives);
            directives = temp;
        }

        if (extension.Interfaces.Count > 0)
        {
            var names = definition.Interfaces.Select(t => t.Name.Value).ToHashSet();
            List<NamedTypeNode>? temp = null;

            foreach (NamedTypeNode type in extension.Interfaces)
            {
                if (names.Add(type.Name.Value))
                {
                    (temp ?? definition.Interfaces.ToList()).Add(type);
                }
            }

            if (temp is not null)
            {
                interfaces = temp;
            }
        }

        if (extension.Fields.Count > 0)
        {
            var map = new OrderedDictionary<string, FieldDefinitionNode>(
                definition.Fields.Select(t => new KeyValuePair<string, FieldDefinitionNode>(
                    t.Name.Value,
                    t)));
            bool touched = false;

            foreach (FieldDefinitionNode extensionField in extension.Fields)
            {
                // By default extensions would only allow to insert new fields.
                // We also use extensions as a vehicle to annotate fields.
                // So if we see that there is already a field we will try to merge it.
                if (map.TryGetValue(extensionField.Name.Value, out FieldDefinitionNode? field))
                {
                    FieldDefinitionNode temp =
                        ApplyExtensions(definition.Name.Value, field, extensionField);

                    if (!touched)
                    {
                        touched = !ReferenceEquals(temp, field);
                    }

                    map[extensionField.Name.Value] = temp;
                }
                else
                {
                    map.Add(extensionField.Name.Value, extensionField);
                    touched = true;
                }
            }

            fields = map.Values.ToList();
        }

        if (!ReferenceEquals(definition.Directives, directives) ||
            !ReferenceEquals(definition.Interfaces, interfaces) ||
            !ReferenceEquals(definition.Fields, fields))
        {
            return new ObjectTypeDefinitionNode(
                null,
                definition.Name,
                definition.Description,
                directives,
                interfaces,
                fields);
        }

        return definition;
    }

    private static FieldDefinitionNode ApplyExtensions(
        string typeName,
        FieldDefinitionNode definition,
        FieldDefinitionNode extension)
    {
        // we first need to validate that the field structure can be merged.
        if (definition.Arguments.Count != extension.Arguments.Count)
        {
            throw ApplyExtensionsMiddleware_ArgumentCountMismatch(typeName, definition, extension);
        }

        IReadOnlyList<InputValueDefinitionNode> arguments = definition.Arguments;
        IReadOnlyList<DirectiveNode> directives = definition.Directives;

        if (definition.Arguments.Count > 0)
        {
            List<InputValueDefinitionNode>? temp = null;

            for (var i = 0; i < definition.Arguments.Count; i++)
            {
                InputValueDefinitionNode arg = definition.Arguments[i];
                InputValueDefinitionNode argExt = definition.Arguments[i];

                if (!arg.Name.Equals(argExt.Name, Syntax))
                {
                    throw ApplyExtensionsMiddleware_UnexpectedArgumentName(
                        arg.Name.Value,
                        argExt.Name.Value,
                        i,
                        typeName,
                        definition.Name.Value);
                }

                if (!arg.Type.Equals(argExt.Type, Syntax))
                {
                    throw ApplyExtensionsMiddleware_ArgumentTypeMismatch(
                        arg,
                        argExt,
                        i,
                        typeName,
                        definition.Name.Value);
                }

                if (argExt.Directives.Count > 0)
                {
                    if (temp is null)
                    {
                        temp = new();

                        if (i > 0)
                        {
                            for (var j = 0; j < i; j++)
                            {
                                temp.Add(arguments[j]);
                            }
                        }
                    }

                    var argDirectives = arg.Directives.ToList();
                    argDirectives.AddRange(argExt.Directives);
                    arg = arg.WithDirectives(argDirectives);
                    temp.Add(arg);
                }
                else if(temp is not null)
                {
                    temp.Add(arg);
                }
            }

            if (temp is not null)
            {
                arguments = temp;
            }
        }

        if (extension.Directives.Count > 0)
        {
            var temp = definition.Directives.ToList();
            temp.AddRange(extension.Directives);
            directives = temp;
        }

        if (!ReferenceEquals(arguments, definition.Arguments) ||
            !ReferenceEquals(directives, definition.Directives))
        {
            return new FieldDefinitionNode(
                null,
                definition.Name,
                definition.Description,
                arguments,
                definition.Type,
                directives);
        }

        return definition;
    }

    private static InterfaceTypeDefinitionNode ApplyExtensions(
        InterfaceTypeDefinitionNode definition,
        InterfaceTypeExtensionNode extension)
    {
        IReadOnlyList<DirectiveNode> directives = definition.Directives;
        IReadOnlyList<NamedTypeNode> interfaces = definition.Interfaces;
        IReadOnlyList<FieldDefinitionNode> fields = definition.Fields;

        if (extension.Directives.Count > 0)
        {
            var temp = definition.Directives.ToList();
            temp.AddRange(extension.Directives);
            directives = temp;
        }

        if (extension.Interfaces.Count > 0)
        {
            var names = definition.Interfaces.Select(t => t.Name.Value).ToHashSet();
            List<NamedTypeNode>? temp = null;

            foreach (NamedTypeNode type in extension.Interfaces)
            {
                if (names.Add(type.Name.Value))
                {
                    (temp ?? definition.Interfaces.ToList()).Add(type);
                }
            }

            if (temp is not null)
            {
                interfaces = temp;
            }
        }

        if (extension.Fields.Count > 0)
        {
            var map = definition.Fields.ToDictionary(t => t.Name.Value);
            List<FieldDefinitionNode>? temp = null;

            foreach (FieldDefinitionNode extensionField in extension.Fields)
            {
                // By default extensions would only allow to insert new fields.
                // We also use extensions as a vehicle to annotate fields.
                // So if we see that there is already a field we will try to merge it.
                if (map.TryGetValue(extensionField.Name.Value, out FieldDefinitionNode? field))
                {
                    map[extensionField.Name.Value] =
                        ApplyExtensions(definition.Name.Value, field, extensionField);
                }
                else
                {
                    map.Add(extensionField.Name.Value, extensionField);
                    (temp ??= definition.Fields.ToList()).Add(extensionField);
                }
            }
        }

        if (!ReferenceEquals(definition.Directives, directives) ||
            !ReferenceEquals(definition.Interfaces, interfaces) ||
            !ReferenceEquals(definition.Fields, fields))
        {
            return new InterfaceTypeDefinitionNode(
                null,
                definition.Name,
                definition.Description,
                directives,
                interfaces,
                fields);
        }

        return definition;
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
