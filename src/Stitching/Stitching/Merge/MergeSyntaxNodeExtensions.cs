using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge
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

        public static T AddSource<T>(
            this T enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
            where T : ITypeDefinitionNode
        {
            return AddSource(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static T AddSource<T>(
            this T typeDefinitionNode,
            NameString newName,
            IEnumerable<NameString> schemaNames)
            where T : ITypeDefinitionNode
        {
            ITypeDefinitionNode node = typeDefinitionNode;

            switch (node)
            {
                case ObjectTypeDefinitionNode otd:
                    node = AddSource(otd, newName, schemaNames);
                    break;

                case InterfaceTypeDefinitionNode itd:
                    node = AddSource(itd, newName, schemaNames);
                    break;

                case UnionTypeDefinitionNode utd:
                    node = AddSource(utd, newName, schemaNames);
                    break;

                case InputObjectTypeDefinitionNode iotd:
                    node = AddSource(iotd, newName, schemaNames);
                    break;

                case EnumTypeDefinitionNode etd:
                    node = AddSource(etd, newName, schemaNames);
                    break;

                case ScalarTypeDefinitionNode std:
                    node = AddSource(std, newName, schemaNames);
                    break;

                default:
                    throw new NotSupportedException();
            }

            return (T)node;
        }

        public static FieldDefinitionNode AddSource(
            this FieldDefinitionNode enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return AddSource(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static FieldDefinitionNode AddSource(
            this FieldDefinitionNode enumTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(enumTypeDefinition, newName, schemaNames,
                (n, d) => enumTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        public static InputValueDefinitionNode AddSource(
            this InputValueDefinitionNode enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return AddSource(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static InputValueDefinitionNode AddSource(
            this InputValueDefinitionNode enumTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(enumTypeDefinition, newName, schemaNames,
                (n, d) => enumTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        public static ScalarTypeDefinitionNode AddSource(
            this ScalarTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return AddSource(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static ScalarTypeDefinitionNode AddSource(
            this ScalarTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(enumTypeDefinition, newName, schemaNames,
                (n, d) => enumTypeDefinition
                    .WithName(n).WithDirectives(d));
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
            return AddSource(enumTypeDefinition, newName, schemaNames,
                (n, d) => enumTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        public static InputObjectTypeDefinitionNode AddSource(
            this InputObjectTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return AddSource(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static InputObjectTypeDefinitionNode AddSource(
            this InputObjectTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(enumTypeDefinition, newName, schemaNames,
                (n, d) => enumTypeDefinition
                    .WithName(n).WithDirectives(d));
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
            return AddSource(unionTypeDefinition, newName, schemaNames,
                (n, d) => unionTypeDefinition
                    .WithName(n).WithDirectives(d));
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
            return AddSource(objectTypeDefinition, newName, schemaNames,
                (n, d) => objectTypeDefinition
                    .WithName(n).WithDirectives(d));
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
            return AddSource(interfaceTypeDefinition, newName, schemaNames,
                (n, d) => interfaceTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        private static T AddSource<T>(
            T interfaceTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames,
            Func<NameNode, IReadOnlyList<DirectiveNode>, T> rewrite)
            where T : NamedSyntaxNode
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

            return rewrite(new NameNode(newName), directives);
        }

        private static IReadOnlyList<DirectiveNode> AddRenamedDirective(
            IReadOnlyList<DirectiveNode> directives,
            NameString originalName,
            IEnumerable<NameString> schemaNames)
        {
            var list = new List<DirectiveNode>(directives);

            foreach (NameString schemaName in schemaNames)
            {
                if (!list.Any(t => HasSourceDirective(t, schemaName)))
                {
                    list.Add(new DirectiveNode
                    (
                        DirectiveNames.Source,
                        new ArgumentNode(
                            DirectiveFieldNames.Source_Name,
                            originalName),
                        new ArgumentNode(
                            DirectiveFieldNames.Source_Schema,
                            schemaName)
                    ));
                }
            }

            return list;
        }

        private static bool HasSourceDirective(
            DirectiveNode directive,
            NameString schemaName)
        {
            if (DirectiveNames.Source.Equals(directive.Name.Value))
            {
                ArgumentNode argument = directive.Arguments.FirstOrDefault(t =>
                    DirectiveFieldNames.Source_Schema.Equals(t.Name));
                return argument != null
                    && argument.Value is StringValueNode sv
                    && schemaName.Equals(sv.Value);
            }
            return false;
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
