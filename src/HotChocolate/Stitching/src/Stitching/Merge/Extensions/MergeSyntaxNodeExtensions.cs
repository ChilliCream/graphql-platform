using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HotChocolate.Language;
using HotChocolate.Stitching.Delegation;
using HotChocolate.Stitching.Properties;

namespace HotChocolate.Stitching.Merge
{
    public static class MergeSyntaxNodeExtensions
    {
        public static T Rename<T>(
            this T enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
            where T : ITypeDefinitionNode
        {
            return Rename(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static T Rename<T>(
            this T typeDefinitionNode,
            NameString newName,
            IEnumerable<NameString> schemaNames)
            where T : ITypeDefinitionNode
        {
            ITypeDefinitionNode node = typeDefinitionNode;

            switch (node)
            {
                case ObjectTypeDefinitionNode otd:
                    node = Rename(otd, newName, schemaNames);
                    break;

                case InterfaceTypeDefinitionNode itd:
                    node = Rename(itd, newName, schemaNames);
                    break;

                case UnionTypeDefinitionNode utd:
                    node = Rename(utd, newName, schemaNames);
                    break;

                case InputObjectTypeDefinitionNode iotd:
                    node = Rename(iotd, newName, schemaNames);
                    break;

                case EnumTypeDefinitionNode etd:
                    node = Rename(etd, newName, schemaNames);
                    break;

                case ScalarTypeDefinitionNode std:
                    node = Rename(std, newName, schemaNames);
                    break;

                default:
                    throw new NotSupportedException();
            }

            return (T)node;
        }

        public static FieldDefinitionNode Rename(
            this FieldDefinitionNode enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return Rename(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static FieldDefinitionNode Rename(
            this FieldDefinitionNode enumTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(enumTypeDefinition, newName, schemaNames,
                (n, d) => enumTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        public static InputValueDefinitionNode Rename(
            this InputValueDefinitionNode enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return Rename(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static InputValueDefinitionNode Rename(
            this InputValueDefinitionNode enumTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(enumTypeDefinition, newName, schemaNames,
                (n, d) => enumTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        public static ScalarTypeDefinitionNode Rename(
            this ScalarTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return Rename(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static ScalarTypeDefinitionNode Rename(
            this ScalarTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(enumTypeDefinition, newName, schemaNames,
                (n, d) => enumTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        public static DirectiveDefinitionNode Rename(
           this DirectiveDefinitionNode directiveDefinition,
           NameString newName,
           params NameString[] schemaNames)
        {
            return Rename(
                directiveDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static DirectiveDefinitionNode Rename(
            this DirectiveDefinitionNode directiveDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return directiveDefinition.WithName(new NameNode(newName));
        }

        public static EnumTypeDefinitionNode Rename(
            this EnumTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return Rename(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static EnumTypeDefinitionNode Rename(
            this EnumTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(enumTypeDefinition, newName, schemaNames,
                (n, d) => enumTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        public static InputObjectTypeDefinitionNode Rename(
            this InputObjectTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return Rename(
                enumTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static InputObjectTypeDefinitionNode Rename(
            this InputObjectTypeDefinitionNode enumTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(enumTypeDefinition, newName, schemaNames,
                (n, d) => enumTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        public static UnionTypeDefinitionNode Rename(
            this UnionTypeDefinitionNode unionTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return Rename(
                unionTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static UnionTypeDefinitionNode Rename(
            this UnionTypeDefinitionNode unionTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(unionTypeDefinition, newName, schemaNames,
                (n, d) => unionTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        public static ObjectTypeDefinitionNode Rename(
            this ObjectTypeDefinitionNode objectTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return Rename(
                objectTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static ObjectTypeDefinitionNode Rename(
            this ObjectTypeDefinitionNode objectTypeDefinition,
            NameString newName,
            IEnumerable<NameString> schemaNames)
        {
            return AddSource(objectTypeDefinition, newName, schemaNames,
                (n, d) => objectTypeDefinition
                    .WithName(n).WithDirectives(d));
        }

        public static InterfaceTypeDefinitionNode Rename(
            this InterfaceTypeDefinitionNode interfaceTypeDefinition,
            NameString newName,
            params NameString[] schemaNames)
        {
            return Rename(
                interfaceTypeDefinition,
                newName,
                (IEnumerable<NameString>)schemaNames);
        }

        public static InterfaceTypeDefinitionNode Rename(
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
            IEnumerable<DirectiveNode> directives,
            NameString originalName,
            IEnumerable<NameString> schemaNames)
        {
            var list = new List<DirectiveNode>(directives);
            bool hasSchemas = false;

            foreach (NameString schemaName in schemaNames)
            {
                hasSchemas = true;
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

            if (!hasSchemas)
            {
                throw new ArgumentException(
                    StitchingResources.MergeSyntaxNodeExtensions_NoSchema,
                    nameof(schemaNames));
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
                    DirectiveFieldNames.Source_Schema.Equals(t.Name.Value));
                return argument != null
                    && argument.Value is StringValueNode sv
                    && schemaName.Equals(sv.Value);
            }
            return false;
        }

        public static FieldDefinitionNode AddDelegationPath(
            this FieldDefinitionNode field,
            NameString schemaName) =>
            AddDelegationPath(field, schemaName, false);

        public static FieldDefinitionNode AddDelegationPath(
            this FieldDefinitionNode field,
            NameString schemaName,
            bool overwrite) =>
            AddDelegationPath(field, schemaName, (string)null);

        public static FieldDefinitionNode AddDelegationPath(
            this FieldDefinitionNode field,
            NameString schemaName,
            SelectionPathComponent selectionPath) =>
            AddDelegationPath(field, schemaName, selectionPath, false);

        public static FieldDefinitionNode AddDelegationPath(
            this FieldDefinitionNode field,
            NameString schemaName,
            SelectionPathComponent selectionPath,
            bool overwrite)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (selectionPath == null)
            {
                throw new ArgumentNullException(nameof(selectionPath));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return AddDelegationPath(
                field,
                schemaName,
                selectionPath.ToString(),
                overwrite);
        }

        public static FieldDefinitionNode AddDelegationPath(
            this FieldDefinitionNode field,
            NameString schemaName,
            IReadOnlyCollection<SelectionPathComponent> selectionPath) =>
            AddDelegationPath(field, schemaName, selectionPath, false);

        public static FieldDefinitionNode AddDelegationPath(
            this FieldDefinitionNode field,
            NameString schemaName,
            IReadOnlyCollection<SelectionPathComponent> selectionPath,
            bool overwrite)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (selectionPath == null)
            {
                throw new ArgumentNullException(nameof(selectionPath));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            if (selectionPath.Count == 0)
            {
                return AddDelegationPath(field, schemaName);
            }

            if (selectionPath.Count == 1)
            {
                return AddDelegationPath(
                    field,
                    schemaName,
                    selectionPath.Single());
            }

            var path = new StringBuilder();
            path.Append(selectionPath.First().ToString());

            foreach (SelectionPathComponent component in selectionPath.Skip(1))
            {
                path.Append('.');
                path.Append(component.ToString());
            }

            return AddDelegationPath(
                field, schemaName, path.ToString(), overwrite);
        }

        public static FieldDefinitionNode AddDelegationPath(
            this FieldDefinitionNode field,
            NameString schemaName,
            string delegationPath) =>
            AddDelegationPath(field, schemaName, delegationPath, false);

        public static FieldDefinitionNode AddDelegationPath(
            this FieldDefinitionNode field,
            NameString schemaName,
            string delegationPath,
            bool overwrite)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            if (!overwrite && field.Directives.Any(t =>
                DirectiveNames.Delegate.Equals(t.Name.Value)))
            {
                return field;
            }

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

        public static NameString GetOriginalName(
            this INamedSyntaxNode typeDefinition,
            NameString schemaName)
        {
            if (typeDefinition == null)
            {
                throw new ArgumentNullException(nameof(typeDefinition));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            DirectiveNode? sourceDirective = typeDefinition.Directives
                .FirstOrDefault(t => HasSourceDirective(t, schemaName));

            if (sourceDirective is not null)
            {
                ArgumentNode argument = sourceDirective.Arguments.First(t =>
                    DirectiveFieldNames.Source_Name.Equals(t.Name.Value));
                if (argument.Value is StringValueNode value)
                {
                    return value.Value;
                }
            }

            return typeDefinition.Name.Value;
        }

        public static bool IsFromSchema(
            this INamedSyntaxNode typeDefinition,
            NameString schemaName)
        {
            if (typeDefinition == null)
            {
                throw new ArgumentNullException(nameof(typeDefinition));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return typeDefinition.Directives.Any(t =>
                HasSourceDirective(t, schemaName));
        }
    }
}
