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

                    case SyntaxKind.ObjectTypeDefinition:
                    case SyntaxKind.ObjectTypeExtension:
                    case SyntaxKind.InterfaceTypeDefinition:
                    case SyntaxKind.InterfaceTypeExtension:
                    case SyntaxKind.UnionTypeDefinition:
                    case SyntaxKind.UnionTypeExtension:
                    case SyntaxKind.InputObjectTypeDefinition:
                    case SyntaxKind.InputObjectTypeExtension:
                    case SyntaxKind.EnumTypeDefinition:
                    case SyntaxKind.EnumTypeExtension:
                    case SyntaxKind.ScalarTypeDefinition:
                    case SyntaxKind.ScalarTypeExtension:
                        rename.EnsureFromHasNoValue();
                        rename.From = ((IHasName)parent).Name.Value;
                        RegisterRename(rename);
                        break;

                    case SyntaxKind.FieldDefinition:
                        rename.EnsureFromHasNoValue();
                        rename.From = new SchemaCoordinate(
                            ((IHasName)context.Path.Peek(2)).Name.Value, 
                            ((IHasName)parent).Name.Value);
                        RegisterRename(rename);
                        break;

                    case SyntaxKind.InputValueDefinition:
                        rename.EnsureFromHasNoValue();
                        rename.From = new SchemaCoordinate(
                            ((IHasName)context.Path.Peek(2)).Name.Value, 
                            ((IHasName)parent).Name.Value);
                        RegisterRename(rename);
                        break;
                }
            }
        }

        public ISyntaxNode Rewrite(ISyntaxNode node, ISchemaRewriterContext context)
        {
            ISyntaxNode? parent = null;

            switch (node)
            {
                case EnumTypeDefinitionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case EnumTypeExtensionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case ObjectTypeDefinitionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case ObjectTypeExtensionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case InterfaceTypeDefinitionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case InterfaceTypeExtensionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case UnionTypeDefinitionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case UnionTypeExtensionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case InputObjectTypeDefinitionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case InputObjectTypeExtensionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case ScalarTypeDefinitionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case ScalarTypeExtensionNode type:
                    type = ApplyRenameDirective(type, type.WithName);
                    return RemoveRenameDirectives(type, type.WithDirectives);

                case EnumValueDefinitionNode value:
                    parent = context.Path.Peek();

                    if (parent is EnumTypeDefinitionNodeBase enumType)
                    {
                        value = ApplyRenameDirective(value, enumType, value.WithName);
                    }

                    return RemoveRenameDirectives(value, value.WithDirectives);

                case FieldDefinitionNode field:
                    parent = context.Path.Peek();

                    if (parent.Kind is SyntaxKind.ObjectTypeDefinition ||
                        parent.Kind is SyntaxKind.ObjectTypeExtension ||
                        parent.Kind is SyntaxKind.InterfaceTypeDefinition ||
                        parent.Kind is SyntaxKind.InterfaceTypeExtension)
                    {
                        field = ApplyRenameDirective(field, (IHasName)parent, field.WithName);
                    }

                    return RemoveRenameDirectives(field, field.WithDirectives);

                case InputValueDefinitionNode input:
                    parent = context.Path.Peek();

                    if (parent.Kind is SyntaxKind.InputObjectTypeDefinition ||
                        parent.Kind is SyntaxKind.InputObjectTypeExtension)
                    {
                        input = ApplyRenameDirective(input, (IHasName)parent, input.WithName);
                    }

                    return RemoveRenameDirectives(input, input.WithDirectives);

                case SchemaDefinitionNode schema:
                    return RemoveRenameDirectives(schema, schema.WithDirectives);

                case SchemaExtensionNode schema:
                    return RemoveRenameDirectives(schema, schema.WithDirectives);

                default:
                    return node;
            }
        }

        private void RegisterRename(Rename rename)
            => _renames.Add(rename.From.ToString(), rename.To.FieldName ?? rename.To.TypeName);

        private T ApplyRenameDirective<T>(T node, Func<NameNode, T> rewrite)
            where T : ISyntaxNode, IHasName
            => _renames.TryGetValue(node.Name.Value, out string? name)
                ? rewrite(new NameNode(name))
                : node;

        private T ApplyRenameDirective<T>(T node, IHasName parent, Func<NameNode, T> rewrite)
            where T : ISyntaxNode, IHasName
            => _renames.TryGetValue($"{parent.Name.Value}.{node.Name.Value}", out string? name)
                ? rewrite(new NameNode(name))
                : node;

        private T RemoveRenameDirectives<T>(T node, Func<IReadOnlyList<DirectiveNode>, T> rewrite)
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

                if (!remove && rewritten is not null)
                {
                    rewritten.Add(directive);
                }
                else if (remove)
                {
                    rewritten = new();

                    if (i > 0)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            rewritten.Add(node.Directives[j]);
                        }
                    }
                }
            }

            return rewritten is null ? node : rewrite(rewritten);
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

            public void EnsureFromHasNoValue()
            {
                if (From.HasValue)
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
