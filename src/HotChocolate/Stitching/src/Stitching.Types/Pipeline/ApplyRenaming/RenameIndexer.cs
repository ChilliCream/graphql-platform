using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyRenaming;

internal sealed class RenameIndexer : SyntaxWalker<RenameContext>
{
    protected override ISyntaxVisitorAction Enter(ISyntaxNode node, RenameContext context)
    {
        context.Navigator.Push(node);

        switch (node)
        {
            case ComplexTypeDefinitionNodeBase type:
            {
                if (TryGetRenameInformation(type.Directives, out var dn, out var to))
                {
                    context.RenamedTypes[type.Name.Value] = new RenameInfo(to, dn);
                }

                if (type.Interfaces.Count > 0)
                {
                    for (var i = 0; i < type.Interfaces.Count; i++)
                    {
                        var interfaceName = type.Interfaces[i].Name.Value;

                        if(!context.ImplementedBy.TryGetValue(interfaceName, out var types))
                        {
                            types = new HashSet<string>(StringComparer.Ordinal);
                            context.ImplementedBy.Add(interfaceName, types);
                        }

                        types.Add(type.Name.Value);
                    }
                }
                break;
            }

            case UnionTypeDefinitionNode type
                when TryGetRenameInformation(type.Directives, out var dn, out var to):
                context.RenamedTypes[type.Name.Value] = new RenameInfo(to, dn);
                break;

            case InputObjectTypeDefinitionNode type
                when TryGetRenameInformation(type.Directives, out var dn, out var to):
                context.RenamedTypes[type.Name.Value] = new RenameInfo(to, dn);
                break;

            case EnumTypeDefinitionNode type
                when TryGetRenameInformation(type.Directives, out var dn, out var to):
                context.RenamedTypes[type.Name.Value] = new RenameInfo(to, dn);
                break;

            case ScalarTypeDefinitionNode type
                when TryGetRenameInformation(type.Directives, out var dn, out var to):
                context.RenamedTypes[type.Name.Value] = new RenameInfo(to, dn);
                break;

            case ScalarTypeExtensionNode type
                when Scalars.IsBuiltIn(type.Name.Value) &&
                    TryGetRenameInformation(type.Directives, out var dn, out var to):
                context.RenamedTypes[type.Name.Value] = new RenameInfo(to, dn);
                break;

            case FieldDefinitionNode field
                when TryGetRenameInformation(field.Directives, out var dn, out var to):
                SchemaCoordinateNode coordinateNode = context.Navigator.CreateCoordinate();
                context.RenamedFields[coordinateNode] = new RenameInfo(to, dn);
                break;
        }

        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Leave(ISyntaxNode node, RenameContext context)
    {
        context.Navigator.Pop();
        return base.Leave(node, context);
    }

    private bool TryGetRenameInformation(
        IReadOnlyList<DirectiveNode> directives,
        [NotNullWhen(true)] out DirectiveNode? directive,
        [NotNullWhen(true)] out string? to)
    {
        if (directives.Count == 0)
        {
            directive = null;
            to = null;
            return false;
        }

        for (var i = 0; i < directives.Count; i++)
        {
            DirectiveNode node = directives[0];
            if (RenameDirective.TryParse(node, out RenameDirective? rename))
            {
                directive = node;
                to = rename.To;
                return true;
            }
        }

        directive = null;
        to = null;
        return false;
    }
}
