using System.Collections.Generic;

namespace HotChocolate.Language
{
    internal class VisitorFnWrapper<T>
        : ISyntaxNodeVisitor<T>
        where T : ISyntaxNode
    {
        private readonly VisitorFn<T> _enter;
        private readonly VisitorFn<T> _leave;

        public VisitorFnWrapper(VisitorFn<T> enter, VisitorFn<T> leave)
        {
            _enter = enter;
            _leave = leave;
        }

        public VisitorAction Enter(
            T node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_enter == null)
            {
                return VisitorAction.Default;
            }
            return _enter.Invoke(node, parent, path, ancestors);
        }

        public VisitorAction Leave(
            T node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (_leave == null)
            {
                return VisitorAction.Default;
            }
            return _leave.Invoke(node, parent, path, ancestors);
        }
    }
}
