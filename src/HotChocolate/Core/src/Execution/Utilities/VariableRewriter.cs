using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Execution.Utilities
{
    internal static class VariableRewriter
    {
        public static IValueNode Rewrite(
            IValueNode node,
            IVariableValueCollection variableValues)
        {
            if (node is null)
            {
                throw new System.ArgumentNullException(nameof(node));
            }

            if (variableValues is null)
            {
                throw new System.ArgumentNullException(nameof(variableValues));
            }

            if (TryRewriteValue(node, variableValues, out IValueNode? rewritten))
            {
                return rewritten;
            }
            return node;
        }

        private static ObjectValueNode Rewrite(
            ObjectValueNode node,
            IVariableValueCollection variableValues)
        {
            if (node.Fields.Count == 0)
            {
                return node;
            }

            if (node.Fields.Count == 1)
            {
                if (TryRewriteField(node.Fields[0], variableValues, out ObjectFieldNode? rewritten))
                {
                    return node.WithFields(new[] { rewritten });
                }
                return node;
            }

            ObjectFieldNode[]? rewrittenItems = null;

            for (int i = 0; i < node.Fields.Count; i++)
            {
                if (TryRewriteField(node.Fields[i], variableValues, out ObjectFieldNode? rewritten))
                {
                    if (rewrittenItems is null)
                    {
                        rewrittenItems = new ObjectFieldNode[node.Fields.Count];
                        for (int j = 0; j < i; i++)
                        {
                            rewrittenItems[j] = node.Fields[j];
                        }
                    }
                    rewrittenItems[i] = rewritten;
                }
            }

            if (rewrittenItems is { })
            {
                return node.WithFields(rewrittenItems);
            }

            return node;
        }

        private static bool TryRewriteField(
            ObjectFieldNode original,
            IVariableValueCollection variableValues,
            [NotNullWhen(true)] out ObjectFieldNode? rewritten)
        {
            if (TryRewriteValue(original.Value, variableValues, out IValueNode? rewrittenValue))
            {
                rewritten = original.WithValue(rewrittenValue);
                return true;
            }

            rewritten = null;
            return false;
        }

        private static ListValueNode Rewrite(
            ListValueNode node,
            IVariableValueCollection variableValues)
        {
            if (node.Items.Count == 0)
            {
                return node;
            }

            if (node.Items.Count == 1)
            {
                if (TryRewriteValue(node.Items[0], variableValues, out IValueNode? rewritten))
                {
                    return node.WithItems(new[] { rewritten });
                }
                return node;
            }

            IValueNode[]? rewrittenItems = null;

            for (int i = 0; i < node.Items.Count; i++)
            {
                IValueNode original = node.Items[i];
                if (TryRewriteValue(original, variableValues, out IValueNode? rewritten))
                {
                    if (rewrittenItems is null)
                    {
                        rewrittenItems = new IValueNode[node.Items.Count];
                        for (int j = 0; j < i; i++)
                        {
                            rewrittenItems[j] = node.Items[j];
                        }
                    }
                    rewrittenItems[i] = rewritten;
                }
            }

            if (rewrittenItems is { })
            {
                return node.WithItems(rewrittenItems);
            }

            return node;
        }

        private static bool TryRewriteValue(
            IValueNode original,
            IVariableValueCollection variableValues,
            [NotNullWhen(true)] out IValueNode? rewritten)
        {
            switch (original.Kind)
            {
                case NodeKind.Variable:
                    rewritten = Rewrite((VariableNode)original, variableValues);
                    return true;

                case NodeKind.ObjectValue:
                    rewritten = Rewrite((ObjectValueNode)original, variableValues);
                    if (ReferenceEquals(rewritten, original))
                    {
                        rewritten = null;
                        return false;
                    }
                    return true;

                case NodeKind.ListValue:
                    rewritten = Rewrite((ListValueNode)original, variableValues);
                    if (ReferenceEquals(rewritten, original))
                    {
                        rewritten = null;
                        return false;
                    }
                    return true;

                default:
                    rewritten = null;
                    return false;
            }
        }

        private static IValueNode Rewrite(
            VariableNode node,
            IVariableValueCollection variableValues)
        {
            if (variableValues.TryGetVariable(node.Name.Value, out IValueNode value))
            {
                return value;
            }

            throw ThrowHelper.VariableNotFound(node);
        }
    }
}