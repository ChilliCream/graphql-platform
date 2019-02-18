using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Handlers
{
    public class ObjectTypeMergeHandler
         : ITypeMergeHanlder
    {
        private readonly MergeTypeDelegate _next;

        public ObjectTypeMergeHandler(MergeTypeDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public void Merge(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            ITypeInfo left = types.FirstOrDefault(t =>
               t.Definition is ObjectTypeDefinitionNode);

            if (left == null)
            {
                _next.Invoke(context, types);
            }
            else
            {
                var notMerged = new List<ITypeInfo>(types);
                while (notMerged.Count > 0 && left != null)
                {
                    var leftDef = (ObjectTypeDefinitionNode)left.Definition;
                    var readyToMerge = new List<ITypeInfo>();
                    left.MoveType(notMerged, readyToMerge);
                    var next = new List<ITypeInfo>(notMerged);

                    for (int i = 0; i < notMerged.Count; i++)
                    {
                        if (notMerged[i].Definition is
                            ObjectTypeDefinitionNode rightDef
                            && CanBeMerged(leftDef, rightDef))
                        {
                            notMerged[i].MoveType(next, readyToMerge);
                        }
                    }

                    MergeType(context, readyToMerge);
                    notMerged = next;

                    left = notMerged.FirstOrDefault(t =>
                        t.Definition is ObjectTypeDefinitionNode);
                }

                if (notMerged.Count > 0)
                {
                    _next.Invoke(context, notMerged);
                }
            }
        }

        private void MergeType(
            ISchemaMergeContext context,
            IReadOnlyList<ITypeInfo> types)
        {
            string name = types[0].Definition.Name.Value;

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

            List<ObjectTypeDefinitionNode> definitions = types
                .Select(t => t.Definition)
                .Cast<ObjectTypeDefinitionNode>()
                .ToList();

            // ? : how do we handle the interfaces correctly
            var interfaces = new HashSet<string>(
                definitions.SelectMany(d =>
                    d.Interfaces.Select(t => t.Name.Value)));

            ObjectTypeDefinitionNode definition = definitions[0]
                .WithInterfaces(interfaces.Select(t =>
                    new NamedTypeNode(new NameNode(t))).ToList())
                .AddSource(name, types.Select(t => t.Schema.Name));

            context.AddType(definition);
        }

        private static bool CanBeMerged(
            ObjectTypeDefinitionNode left,
            ObjectTypeDefinitionNode right)
        {
            if (left.Name.Value.Equals(
                right.Name.Value,
                StringComparison.Ordinal)
                && left.Fields.Count == right.Fields.Count)
            {
                Dictionary<string, FieldDefinitionNode> leftFields =
                    left.Fields.ToDictionary(t => t.Name.Value);
                Dictionary<string, FieldDefinitionNode> rightField =
                    right.Fields.ToDictionary(t => t.Name.Value);

                foreach (string name in leftFields.Keys)
                {
                    FieldDefinitionNode leftField = leftFields[name];
                    if (!rightField.TryGetValue(name,
                        out FieldDefinitionNode rightArgument)
                        || !HasSameType(leftField.Type, rightArgument.Type))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static bool HasSameShape(
            FieldDefinitionNode left,
            FieldDefinitionNode right)
        {
            if (left.Name.Value.Equals(
                right.Name.Value,
                StringComparison.Ordinal)
                && HasSameType(left.Type, right.Type)
                && left.Arguments.Count == right.Arguments.Count)
            {
                Dictionary<string, InputValueDefinitionNode> leftArgs =
                    left.Arguments.ToDictionary(t => t.Name.Value);
                Dictionary<string, InputValueDefinitionNode> rightArgs =
                    right.Arguments.ToDictionary(t => t.Name.Value);

                foreach (string name in leftArgs.Keys)
                {
                    InputValueDefinitionNode leftArgument = leftArgs[name];
                    if (!rightArgs.TryGetValue(name,
                        out InputValueDefinitionNode rightArgument)
                        || !HasSameType(leftArgument.Type, rightArgument.Type))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static bool HasSameType(ITypeNode left, ITypeNode right)
        {
            if (left is NonNullTypeNode lnntn
                && right is NonNullTypeNode rnntn)
            {
                return HasSameType(lnntn.Type, rnntn.Type);
            }

            if (left is ListTypeNode lltn
                && right is ListTypeNode rltn)
            {
                return HasSameType(lltn.Type, rltn.Type);
            }

            if (left is NamedTypeNode lntn
                && right is NamedTypeNode rntn)
            {
                return lntn.Name.Value.Equals(
                    rntn.Name.Value,
                    StringComparison.Ordinal);
            }

            throw new NotSupportedException();
        }
    }
}
