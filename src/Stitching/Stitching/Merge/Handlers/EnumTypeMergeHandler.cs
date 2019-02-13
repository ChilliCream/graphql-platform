using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching
{
    public class EnumTypeMergeHandler
        : ITypeMergeHanlder
    {
        private readonly MergeTypeDelegate _next;

        public EnumTypeMergeHandler(MergeTypeDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            ITypeInfo left = types.FirstOrDefault(t =>
               t.Definition is EnumTypeDefinitionNode);

            if (left == null)
            {
                _next.Invoke(context, types);
            }
            else
            {
                var notMerged = new List<ITypeInfo>(types);
                while (notMerged.Count > 0 && left != null)
                {
                    var leftDef = (EnumTypeDefinitionNode)left.Definition;
                    var leftValueSet = new HashSet<string>(
                        leftDef.Values.Select(t => t.Name.Value));
                    var readyToMerge = new List<ITypeInfo>();
                    left.MoveType(notMerged, readyToMerge);

                    for (int i = 0; i < notMerged.Count; i++)
                    {
                        if (notMerged[i].Definition is
                            EnumTypeDefinitionNode rightDef
                            && CanBeMerged(leftValueSet, rightDef))
                        {
                            notMerged[i].MoveType(notMerged, readyToMerge);
                        }
                    }

                    MergeType(context, readyToMerge);

                    left = notMerged.FirstOrDefault(t =>
                        t.Definition is EnumTypeDefinitionNode);
                }

                _next.Invoke(context, notMerged);
            }
        }

        private void MergeType(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            var definition = (EnumTypeDefinitionNode)types[0].Definition;

            EnumTypeDefinitionNode descriptionDef =
                types.Select(t => t.Definition)
                .Cast<EnumTypeDefinitionNode>()
                .FirstOrDefault(t => t.Description != null);

            if (descriptionDef != null)
            {
                definition = definition.WithDescription(
                    descriptionDef.Description);
            }

            NameString name = types[0].Definition.Name.Value;

            if (context.ContainsType(name))
            {
                for (int i = 0; i < types.Count; i++)
                {
                    name = types[i].CreateUniqueName();
                    if (!context.ContainsType(name))
                    {
                        break;
                    }
                }
            }

            if (name.Equals(definition.Name.Value))
            {
                context.AddType(definition);
            }
            else
            {
                context.AddType(definition.Rename(name,
                    types.Select(t => t.Schema.Name)));
            }
        }

        private bool CanBeMerged(
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
}
