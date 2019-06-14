using System;
using System.Collections.Generic;
namespace HotChocolate.Language
{
    public class SyntaxNodeVisitor
        : ISyntaxNodeVisitor<IValueNode>
        , ISyntaxNodeVisitor<ObjectValueNode>
        , ISyntaxNodeVisitor<ObjectFieldNode>
        , ISyntaxNodeVisitor<ListValueNode>
        , ISyntaxNodeVisitor<StringValueNode>
        , ISyntaxNodeVisitor<IntValueNode>
        , ISyntaxNodeVisitor<FloatValueNode>
        , ISyntaxNodeVisitor<BooleanValueNode>
        , ISyntaxNodeVisitor<EnumValueNode>
        , ISyntaxNodeVisitor<VariableNode>
    {
        public virtual VisitorAction Enter(
            IValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            switch (node)
            {
                case ObjectValueNode ov:
                    return Enter(ov, parent, path, ancestors);
                case ListValueNode lv:
                    return Enter(lv, parent, path, ancestors);
                case StringValueNode sv:
                    return Enter(sv, parent, path, ancestors);
                case IntValueNode iv:
                    return Enter(iv, parent, path, ancestors);
                case FloatValueNode fv:
                    return Enter(fv, parent, path, ancestors);
                case BooleanValueNode fv:
                    return Enter(fv, parent, path, ancestors);
                case EnumValueNode ev:
                    return Enter(ev, parent, path, ancestors);
                case VariableNode vv:
                    return Enter(vv, parent, path, ancestors);
                default:
                    throw new NotSupportedException();
            }
        }

        public virtual VisitorAction Enter(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Enter(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Enter(
            ListValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Enter(
            StringValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Enter(
            IntValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Enter(
            FloatValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Enter(
            BooleanValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Enter(
            EnumValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Enter(
            VariableNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Leave(
            IValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            switch (node)
            {
                case ObjectValueNode ov:
                    return Leave(ov, parent, path, ancestors);
                case ListValueNode lv:
                    return Leave(lv, parent, path, ancestors);
                case StringValueNode sv:
                    return Leave(sv, parent, path, ancestors);
                case IntValueNode iv:
                    return Leave(iv, parent, path, ancestors);
                case FloatValueNode fv:
                    return Leave(fv, parent, path, ancestors);
                case BooleanValueNode fv:
                    return Leave(fv, parent, path, ancestors);
                case EnumValueNode ev:
                    return Leave(ev, parent, path, ancestors);
                case VariableNode vv:
                    return Leave(vv, parent, path, ancestors);
                default:
                    throw new NotSupportedException();
            }
        }

        public virtual VisitorAction Leave(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Leave(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Leave(
            ListValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Leave(
            StringValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Leave(
            IntValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Leave(
            FloatValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Leave(
            BooleanValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Leave(
            EnumValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }

        public virtual VisitorAction Leave(
            VariableNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            return VisitorAction.Skip;
        }
    }
}
