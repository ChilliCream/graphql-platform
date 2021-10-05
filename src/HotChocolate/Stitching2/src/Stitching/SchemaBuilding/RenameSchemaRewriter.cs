using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.SchemaBuilding
{
    public class RenameSchemaRewriter : ISchemaRewriter
    {
        private const string _rename = "rename";
        private const string _to = "to";
        private const string _from = "from";

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
                        RegisterRename(rename);
                        break;
                }
            }
        }

        public ISyntaxNode Rewrite(ISyntaxNode node, ISchemaRewriterContext context)
        {
            string? name = null;
            IReadOnlyList<DirectiveNode> directives = Array.Empty<DirectiveNode>();

            switch (node)
            {
                case EnumTypeDefinitionNode type:
                    if (_renames.TryGetValue(type.Name.Value, out name))
                    {
                        return type.WithName(new NameNode(name));
                    }
                    return node;

                case ObjectTypeDefinitionNode type:
                    if (_renames.TryGetValue(type.Name.Value, out name))
                    {
                        return type.WithName(new NameNode(name));
                    }
                    return node;

                case InterfaceTypeDefinitionNode type:
                    return node;

                case UnionTypeDefinitionNode type:
                    return node;

                case InputObjectTypeDefinitionNode type:
                    return node;

                case ScalarTypeDefinitionNode type:
                    return node;

                case EnumValueDefinitionNode value:
                    return node;

                case FieldDefinitionNode field:
                    directives = RemoveRenameDirectives(field.Directives);
                    if (directives.Count != field.Directives.Count)
                    {
                        field = field.WithDirectives(directives);
                    }
                    return node;

                case InputValueDefinitionNode input:
                    directives = RemoveRenameDirectives(input.Directives);
                    if (directives.Count != input.Directives.Count)
                    {
                        input = input.WithDirectives(directives);
                    }
                    return node;

                case SchemaDefinitionNode schema:
                    directives = RemoveRenameDirectives(schema.Directives);
                    if (directives.Count != schema.Directives.Count)
                    {
                        schema = schema.WithDirectives(directives);
                    }
                    return schema;

                case SchemaExtensionNode schema:
                    directives = RemoveRenameDirectives(schema.Directives);
                    if (directives.Count != schema.Directives.Count)
                    {
                        schema = schema.WithDirectives(directives);
                    }
                    return schema;

                default:
                    return node;
            }
        }

        private void RegisterRename(Rename rename)
            => _renames.Add(rename.From.ToString(), rename.To.FieldName ?? rename.To.TypeName);

        private T RemoveRenameDirectives<T>(T node, Func<T, T> rewrite)
            where T : ISyntaxNode, IHasDirectives
        {
            if (node.Directives.Count == 0)
            {
                return node;
            }

            List<DirectiveNode>? rewritten = null;

            for (int i = 0; i < node.Directives.Count; i++)
            {
                DirectiveNode directive = node.Directives[i];
                bool remove = directive.Name.Value.Equals(_rename, StringComparison.Ordinal);

                if (rewritten is not null)
                {

                }
                else if (remove)
                {
                    rewritten = new();

                    if (i > 0)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            rewritten.Add(dire)
                        }
                    }
                }
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

                    if (!argument.Name.Value.Equals(_to, StringComparison.Ordinal))
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
                    if (arg.Name.Value.Equals(_to, StringComparison.Ordinal))
                    {
                        to = arg;
                    }
                    else if (arg.Name.Value.Equals(_from, StringComparison.Ordinal))
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
