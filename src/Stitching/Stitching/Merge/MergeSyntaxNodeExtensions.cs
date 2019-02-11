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

        public static EnumTypeDefinitionNode Rename(
            this EnumTypeDefinitionNode enumTypeDefinition,
            NameString newName)
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
                    originalName);

            return enumTypeDefinition
                .WithName(new NameNode(newName))
                .WithDirectives(directives);
        }

        public static UnionTypeDefinitionNode Rename(
            this UnionTypeDefinitionNode enumTypeDefinition,
            NameString newName)
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
                    originalName);

            return enumTypeDefinition
                .WithName(new NameNode(newName))
                .WithDirectives(directives);
        }

        private static IReadOnlyList<DirectiveNode> AddRenamedDirective(
            IReadOnlyList<DirectiveNode> directives,
            NameString originalName)
        {
            var list = new List<DirectiveNode>(directives);

            list.RemoveAll(t =>
                DirectiveNames.Renamed.Equals(t.Name.Value));

            list.Add(new DirectiveNode
            (
                DirectiveNames.Renamed,
                new ArgumentNode(
                    DirectiveFieldNames.Renamed_Name,
                    originalName)
            ));

            return list;
        }
    }
}
