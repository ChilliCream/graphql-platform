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

        public static EnumTypeDefinitionNode AddSource(
            this EnumTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return AddSource(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static EnumTypeDefinitionNode AddSource(
            this EnumTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            if (enumTypeDefinition == null)
            {
                throw new ArgumentNullException(nameof(enumTypeDefinition));
            }

            if (schemaNames == null)
            {
                throw new ArgumentNullException(nameof(schemaNames));
            }

            newName.EnsureNotEmpty(nameof(newName));

            NameString originalName = enumTypeDefinition.Name.Value;

            IReadOnlyList<DirectiveNode> directives =
                AddRenamedDirective(
                    enumTypeDefinition.Directives,
                    originalName,
                    schemaNames);

            return enumTypeDefinition
                .WithName(new NameNode(newName))
                .WithDirectives(directives);
        }

        public static UnionTypeDefinitionNode AddSource(
            this UnionTypeDefinitionNode unionTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return AddSource(
                unionTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static UnionTypeDefinitionNode AddSource(
            this UnionTypeDefinitionNode unionTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            if (unionTypeDefinition == null)
            {
                throw new ArgumentNullException(nameof(unionTypeDefinition));
            }

            if (schemaNames == null)
            {
                throw new ArgumentNullException(nameof(schemaNames));
            }

            newName.EnsureNotEmpty(nameof(newName));

            NameString originalName = unionTypeDefinition.Name.Value;

            IReadOnlyList<DirectiveNode> directives =
                AddRenamedDirective(
                    unionTypeDefinition.Directives,
                    originalName,
                    schemaNames);

            return unionTypeDefinition
                .WithName(new NameNode(newName))
                .WithDirectives(directives);
        }

        public static ObjectTypeDefinitionNode AddSource(
            this ObjectTypeDefinitionNode objectTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return AddSource(
                objectTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static ObjectTypeDefinitionNode AddSource(
            this ObjectTypeDefinitionNode objectTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            if (objectTypeDefinition == null)
            {
                throw new ArgumentNullException(nameof(objectTypeDefinition));
            }

            if (schemaNames == null)
            {
                throw new ArgumentNullException(nameof(schemaNames));
            }

            newName.EnsureNotEmpty(nameof(newName));

            NameString originalName = objectTypeDefinition.Name.Value;

            IReadOnlyList<DirectiveNode> directives =
                AddRenamedDirective(
                    objectTypeDefinition.Directives,
                    originalName,
                    schemaNames);

            return objectTypeDefinition
                .WithName(new NameNode(newName))
                .WithDirectives(directives);
        }

        public static InterfaceTypeDefinitionNode AddSource(
            this InterfaceTypeDefinitionNode interfaceTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return AddSource(
                interfaceTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static InterfaceTypeDefinitionNode AddSource(
            this InterfaceTypeDefinitionNode interfaceTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            if (interfaceTypeDefinition == null)
            {
                throw new ArgumentNullException(
                    nameof(interfaceTypeDefinition));
            }

            if (schemaNames == null)
            {
                throw new ArgumentNullException(nameof(schemaNames));
            }

            newName.EnsureNotEmpty(nameof(newName));

            NameString originalName = interfaceTypeDefinition.Name.Value;

            IReadOnlyList<DirectiveNode> directives =
                AddRenamedDirective(
                    interfaceTypeDefinition.Directives,
                    originalName,
                    schemaNames);

            return interfaceTypeDefinition
                .WithName(new NameNode(newName))
                .WithDirectives(directives);
        }

        private static IReadOnlyList<DirectiveNode> AddRenamedDirective(
            IReadOnlyList<DirectiveNode> directives,
            NameString originalName,
            IEnumerable<NameString> schemaNames)
        {
            var list = new List<DirectiveNode>(directives);

            foreach (NameString schemaName in schemaNames)
            {
                list.Add(new DirectiveNode
                (
                    DirectiveNames.Source,
                    new ArgumentNode(
                        DirectiveFieldNames.Renamed_Name,
                        originalName),
                    new ArgumentNode(
                        DirectiveFieldNames.Renamed_Schema,
                        schemaName)
                ));
            }

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
