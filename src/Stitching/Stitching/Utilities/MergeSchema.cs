using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public interface ISchemaInfo
    {
        string Name { get; }
        DocumentNode Schema { get; }
    }

    public interface ITypeInfo
    {
        ITypeDefinitionNode Definition { get; }

        DocumentNode Schema { get; }

        string SchemaName { get; }
    }

    public interface IMergeSchemaContext
    {
        void AddType(ITypeDefinitionNode type);
    }

    public interface ITypeMerger
    {
        void Merge(
            IMergeSchemaContext context,
            IReadOnlyList<ITypeInfo> types);
    }

    public delegate MergeTypeDelegate MergeTypeFactory(MergeTypeDelegate next);

    public delegate void MergeTypeDelegate(IMergeSchemaContext context, IReadOnlyList<ITypeInfo> types);

    public class MergeEnumType
        : ITypeMerger
    {
        private readonly MergeTypeDelegate _next;

        public MergeEnumType(MergeTypeDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            IMergeSchemaContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            if (types.All(t => t.Definition is EnumTypeDefinitionNode))
            {
                var first = (EnumTypeDefinitionNode)types[0].Definition;
                StringValueNode description = first.Description;
                var values = new HashSet<string>(
                    first.Values.Select(t => t.Name.Value));

                for (int i = 1; i < types.Count; i++)
                {
                    var other = (EnumTypeDefinitionNode)types[i].Definition;
                    if (AreEqual(values, first))
                    {
                        if (description != null && other.Description != null)
                        {
                            description = other.Description;
                        }
                    }
                    else
                    {
                        context.AddType(other.Rename(
                            types[i].CreateUniqueName()));
                    }
                }

                context.AddType(first);
            }
            else
            {
                _next.Invoke(context, types);
            }
        }

        private bool AreEqual(
            ISet<string> left,
            EnumTypeDefinitionNode right)
        {
            if (left.Count == right.Values.Count)
            {
                for (int i = 0; i < right.Values.Count; i++)
                {
                    if (!left.Contains(right.Values[i].Name.Value))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }

    public static class MergeSyntaxNodeExtensions
    {
        public static NameString CreateUniqueName(
            this ITypeInfo typeInfo)
        {
            if (typeInfo == null)
            {
                throw new ArgumentNullException(nameof(typeInfo));
            }

            return $"{typeInfo.SchemaName}_{typeInfo.Definition.Name.Value}";
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
