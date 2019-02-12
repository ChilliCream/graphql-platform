using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public static class MergeSyntaxNodeExtensions
    {
        public static NameString CreateUniqueName(
            this ITypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return $"{typeInfo.Schema.Name}_{typeInfo.Definition.Name.Value}";
        }

        public static NameString CreateUniqueName(
            this ITypeInfo typeInfo, NamedSyntaxNode namedSyntaxNode)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            if (namedSyntaxNode == null)
            {
                throw new ArgumentNullException(nameof(namedSyntaxNode));
            }

            return $"{typeInfo.Schema.Name}_{namedSyntaxNode.Name.Value}";
        }

        public static EnumTypeDefinitionNode Rename(
            this EnumTypeDefinitionNode enumTypeDefinition,
            NameString newName, NameString schemaName)
        {
            if (enumTypeDefinition == null)
            {
                throw new ArgumentNullException(nameof(enumTypeDefinition));
            }

            newName.EnsureNotEmpty(nameof(newName));

            NameString originalName = enumTypeDefinition.Name.Value;

            IReadOnlyList<DirectiveNode> directives =
                AddRenamedDirective(
                    enumTypeDefinition.Directives,
                    originalName,
                    schemaName);

            return enumTypeDefinition
                .WithName(new NameNode(newName))
                .WithDirectives(directives);
        }

        public static UnionTypeDefinitionNode Rename(
            this UnionTypeDefinitionNode enumTypeDefinition,
            NameString newName, NameString schemaName)
        {
            if (enumTypeDefinition == null)
            {
                throw new ArgumentNullException(nameof(enumTypeDefinition));
            }

            newName.EnsureNotEmpty(nameof(newName));

            NameString originalName = enumTypeDefinition.Name.Value;

            IReadOnlyList<DirectiveNode> directives =
                AddRenamedDirective(
                    enumTypeDefinition.Directives,
                    originalName,
                    schemaName);

            return enumTypeDefinition
                .WithName(new NameNode(newName))
                .WithDirectives(directives);
        }

        private static IReadOnlyList<DirectiveNode> AddRenamedDirective(
            IReadOnlyList<DirectiveNode> directives,
            NameString originalName, NameString schemaName)
        {
            var list = new List<DirectiveNode>(directives);

            list.Add(new DirectiveNode
            (
                DirectiveNames.Renamed,
                new ArgumentNode(
                    DirectiveFieldNames.Renamed_Name,
                    originalName),
                new ArgumentNode(
                    DirectiveFieldNames.Renamed_Schema,
                    schemaName)
            ));

            return list;
        }

        public static FieldDefinitionNode AddDelegationPath(
            this FieldDefinitionNode field,
            NameString schemaName) =>
            AddDelegationPath(field, schemaName, null);

        public static FieldDefinitionNode AddDelegationPath(
            this FieldDefinitionNode field,
            NameString schemaName,
            string delegationPath)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            var list = new List<DirectiveNode>(field.Directives);

            list.RemoveAll(t =>
                DirectiveNames.Delegate.Equals(t.Name.Value));

            var arguments = new List<ArgumentNode>();
            arguments.Add(new ArgumentNode(
                DirectiveFieldNames.Delegate_Schema,
                schemaName));

            if (!string.IsNullOrEmpty(delegationPath))
            {
                arguments.Add(new ArgumentNode(
                    DirectiveFieldNames.Delegate_Path,
                    delegationPath));
            }

            list.Add(new DirectiveNode
            (
                DirectiveNames.Delegate,
                arguments
            ));

            return field.WithDirectives(list);
        }
    }
}
