using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Language
{
    public static partial class VisitorExtensions
    {
        private static readonly Dictionary<Type, IntVisitorFn> _enterVisitors =
            new Dictionary<Type, IntVisitorFn>()
            {
                { typeof(IValueNode), EnterVisitor<IValueNode>() },
                { typeof(ObjectValueNode), EnterVisitor<ObjectValueNode>() },
                { typeof(ObjectFieldNode), EnterVisitor<ObjectFieldNode>() },
                { typeof(ListValueNode), EnterVisitor<ListValueNode>() },
                { typeof(StringValueNode), EnterVisitor<StringValueNode>() },
                { typeof(IntValueNode), EnterVisitor<IntValueNode>() },
                { typeof(FloatValueNode), EnterVisitor<FloatValueNode>() },
                { typeof(BooleanValueNode), EnterVisitor<BooleanValueNode>() },
                { typeof(EnumValueNode), EnterVisitor<EnumValueNode>() },
                { typeof(VariableNode), EnterVisitor<VariableNode>() },
            };

        private static readonly Dictionary<Type, IntVisitorFn> _leaveVisitors =
            new Dictionary<Type, IntVisitorFn>()
            {
                { typeof(IValueNode), LeaveVisitor<IValueNode>() },
                { typeof(ObjectValueNode), LeaveVisitor<ObjectValueNode>() },
                { typeof(ObjectFieldNode), LeaveVisitor<ObjectFieldNode>() },
                { typeof(ListValueNode), LeaveVisitor<ListValueNode>() },
                { typeof(StringValueNode), LeaveVisitor<StringValueNode>() },
                { typeof(IntValueNode), LeaveVisitor<IntValueNode>() },
                { typeof(FloatValueNode), LeaveVisitor<FloatValueNode>() },
                { typeof(BooleanValueNode), LeaveVisitor<BooleanValueNode>() },
                { typeof(EnumValueNode), LeaveVisitor<EnumValueNode>() },
                { typeof(VariableNode), LeaveVisitor<VariableNode>() },
            };

        private static IntVisitorFn EnterVisitor<T>()
            where T : ISyntaxNode =>
            CreateVisitor<T>(true);

        private static IntVisitorFn LeaveVisitor<T>()
            where T : ISyntaxNode =>
            CreateVisitor<T>(false);

        private static IntVisitorFn CreateVisitor<T>(bool enter)
            where T : ISyntaxNode
        {

            return (visitor, node, parent, path, ancestors) =>
            {
                if (visitor is ISyntaxNodeVisitor<T> typedVisitor)
                {
                    if (enter)
                    {
                        return typedVisitor.Enter(
                            (T)node, parent, path, ancestors);
                    }
                    else
                    {
                        return typedVisitor.Leave(
                            (T)node, parent, path, ancestors);
                    }
                }
                return VisitorAction.Skip;
            };
        }
    }
}
