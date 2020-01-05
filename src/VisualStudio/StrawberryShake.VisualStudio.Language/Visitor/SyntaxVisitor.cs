using System.Collections.Generic;

namespace StrawberryShake.VisualStudio.Language
{
    public class SyntaxVisitor
        : ISyntaxVisitor
    {
        private static readonly SyntaxNodeListPool _listPool = new SyntaxNodeListPool();
        private static readonly List<ISyntaxNode> _empty = new List<ISyntaxNode>();

        public SyntaxVisitor()
        {
            DefaultAction = Skip;
        }

        public SyntaxVisitor(ISyntaxVisitorAction defaultResult)
        {
            DefaultAction = defaultResult;
        }

        protected virtual ISyntaxVisitorAction DefaultAction { get; }

        public static ISyntaxVisitorAction Break { get; } = new BreakSyntaxVisitorAction();

        public static ISyntaxVisitorAction Skip { get; } = new SkipSyntaxVisitorAction();

        public static ISyntaxVisitorAction Continue { get; } = new ContinueSyntaxVisitorAction();

        public ISyntaxVisitorAction Visit(
            ISyntaxNode node,
            ISyntaxVisitorContext context)
        {
            var levels = new List<List<ISyntaxNode>>();
            List<ISyntaxNode> ancestors = _listPool.Get();
            List<ISyntaxNode> root = _listPool.Get();
            ISyntaxVisitorContext localContext = context;
            int index = 0;

            root.Push(node);
            levels.Push(root);

            ISyntaxNode? parent = null;
            ISyntaxVisitorAction result = DefaultAction;

            while (levels.Count > 0)
            {
                bool isLeaving = levels[index].Count == 0;
                ISyntaxNode? current;

                if (isLeaving)
                {
                    if (index == 0)
                    {
                        break;
                    }

                    _listPool.Return(levels.Pop());
                    current = ancestors.Pop();
                    ancestors.TryPeek(out parent);
                    result = Leave(current, localContext);
                    localContext = OnAfterLeave(current, parent, ancestors, localContext);
                    index--;
                }
                else
                {
                    current = levels[index].Pop();
                    localContext = OnBeforeEnter(current, parent, ancestors, localContext);
                    result = Enter(current, localContext);

                    if (result is ContinueSyntaxVisitorAction)
                    {
                        List<ISyntaxNode> nextLevel = _listPool.Get();
                        nextLevel.AddRange(GetNodes(current, localContext));
                        levels.Push(nextLevel);
                    }
                    else if (result is SkipSyntaxVisitorAction)
                    {
                        levels.Push(_empty);
                    }

                    parent = current;
                    ancestors.Push(current);
                    index++;
                }

                if (result is BreakSyntaxVisitorAction)
                {
                    break;
                }
            }

            if (levels.Count > 0)
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    _listPool.Return(levels[i]);
                }
            }

            _listPool.Return(ancestors);
            return result;
        }

        protected virtual ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual ISyntaxVisitorAction Leave(
            ISyntaxNode node,
            ISyntaxVisitorContext context) =>
            DefaultAction;

        protected virtual IEnumerable<ISyntaxNode> GetNodes(
            ISyntaxNode node,
            ISyntaxVisitorContext context) =>
            node.GetNodes();

        protected virtual ISyntaxVisitorContext OnBeforeEnter(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            ISyntaxVisitorContext context) =>
            context;

        protected ISyntaxVisitorContext OnAfterLeave(
            ISyntaxNode node,
            ISyntaxNode? parent,
            IReadOnlyList<ISyntaxNode> ancestors,
            ISyntaxVisitorContext context) =>
            context;
    }
}
