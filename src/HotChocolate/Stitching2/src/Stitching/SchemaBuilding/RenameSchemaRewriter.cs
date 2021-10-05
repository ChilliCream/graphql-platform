using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.SchemaBuilding
{
    public class RenameSchemaRewriter : ISchemaRewriter
    {
        private readonly Dictionary<string, string> _renames = new();

        public void Inspect(DirectiveNode directive, ISchemaRewriterContext context)
        {
            if (directive.Name.Value.Equals("rename", StringComparison.Ordinal))
            {
                Rename rename = Rename.Parse(directive);
                ISyntaxNode parent = context.Path.Peek();

                switch (parent.Kind)
                {
                    case SyntaxKind.SchemaExtension:
                    case SyntaxKind.SchemaDefinition:
                        rename.EnsureFromHasValue();
                        _renames.Add(rename);
                        break;
                }
            }
        }

        public ISyntaxNode Rewrite(ISyntaxNode node, ISchemaRewriterContext context)
        {
            switch (node)
            {
                case EnumTypeDefinitionNode type:
                    if (_renames.TryGetValue(type.Name.Value, out var name))
                    { 
                        return type.WithName(new NameNode(name));
                    }
                    return node;

                case SyntaxKind.ObjectTypeDefinition:
                case SyntaxKind.InterfaceTypeDefinition:
                case SyntaxKind.UnionTypeDefinition:
                case SyntaxKind.InputObjectTypeDefinition:
                case SyntaxKind.ScalarTypeDefinition:
                case SyntaxKind.EnumValueDefinition:
                case SyntaxKind.InputValueDefinition:
                case SyntaxKind.FieldDefinition:
                    break;

                default:
                    return node;
            }
        }

        private struct Rename
        {
            public SchemaCoordinate From { get; set; }
            public SchemaCoordinate To { get; set; }

            public void EnsureFromHasValue()
            {
                if (!From.HasValue)
                {
                    throw new Exception("");
                }
            }

            public static Rename Parse(DirectiveNode directive)
            {
                if (directive.Arguments.Count == 0 || directive.Arguments.Count > 2)
                {
                    throw new Exception("");
                }

                if (directive.Arguments.Count == 1)
                {
                    ArgumentNode argument = directive.Arguments[0];

                    if (!argument.Name.Value.Equals("to", StringComparison.Ordinal))
                    {
                        throw new Exception("");
                    }

                    if (argument.Value is not StringValueNode sv)
                    {
                        throw new Exception("");
                    }

                    return new Rename { To = sv.Value };
                }

                ArgumentNode? to = null;
                ArgumentNode? from = null;

                foreach (ArgumentNode arg in directive.Arguments)
                {
                    if (arg.Name.Value.Equals("to", StringComparison.Ordinal))
                    {
                        to = arg;
                    }
                    else if (arg.Name.Value.Equals("from", StringComparison.Ordinal))
                    {
                        from = arg;
                    }
                }

                if (to is null || from is null)
                {
                    throw new Exception("");
                }

                if (to.Value is not StringValueNode tov ||
                    from.Value is not StringValueNode fov)
                {
                    throw new Exception("");
                }

                return new Rename { To = tov.Value, From = fov.Value };
            }
        }
    }
}
