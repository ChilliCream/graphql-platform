using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;
using static HotChocolate.Stitching.Types.ThrowHelper;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyExtensions;

internal abstract class ApplyComplexTypeExtension<TDef, TExt>
    : ApplyExtension<TDef, TExt>
    where TDef : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
    where TExt : ComplexTypeDefinitionNodeBase, ITypeExtensionNode
{
    protected override TDef Apply(TDef definition, TExt extension)
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
            var touched = false;

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
            return CreateDefinition(
                definition.Name,
                definition.Description,
                directives,
                interfaces,
                fields);
        }

        return definition;
    }

    protected abstract TDef CreateDefinition(
        NameNode name,
        StringValueNode? description,
        IReadOnlyList<DirectiveNode> directives,
        IReadOnlyList<NamedTypeNode> interfaces,
        IReadOnlyList<FieldDefinitionNode> fields);

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

                if (!arg.Name.Equals(argExt.Name, SyntaxComparison.Syntax))
                {
                    throw ApplyExtensionsMiddleware_UnexpectedArgumentName(
                        arg.Name.Value,
                        argExt.Name.Value,
                        i,
                        typeName,
                        definition.Name.Value);
                }

                if (!arg.Type.Equals(argExt.Type, SyntaxComparison.Syntax))
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
}
