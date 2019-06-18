using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters
{
    public class DummyFilter
        : FilterVisitorBase
    {
        public DummyFilter(InputObjectType initialType)
            : base(initialType)
        {
            Level.Push(new Queue<string>());
        }

        public string Query => Level.Peek().Peek();

        protected Stack<Queue<string>> Level { get; } =
            new Stack<Queue<String>>();

        public override VisitorAction Enter(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Level.Push(new Queue<string>());
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Queue<string> operations = Level.Pop();
            if (operations.Count == 1)
            {
                Level.Peek().Enqueue(operations.Dequeue());
            }
            else
            {
                Level.Peek().Enqueue("(" + string.Join(" AND ", operations) + ")");
            }
            return VisitorAction.Continue;
        }

        public override VisitorAction Enter(
            ListValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            switch (Operations.Peek())
            {
                case AndField and:
                case OrField or:
                    Level.Push(new Queue<string>());
                    break;
            }

            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ListValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Queue<string> operations;

            switch (Operations.Peek())
            {
                case AndField and:
                case FilterOperationField op:
                    operations = Level.Pop();
                    if (operations.Count == 1)
                    {
                        Level.Peek().Enqueue(operations.Dequeue());
                    }
                    else
                    {
                        Level.Peek().Enqueue("(" + string.Join(" AND ", operations) + ")");
                    }
                    break;
                case OrField or:
                    operations = Level.Pop();
                    if (operations.Count == 1)
                    {
                        Level.Peek().Enqueue(operations.Dequeue());
                    }
                    else
                    {
                        Level.Peek().Enqueue("(" + string.Join(" OR ", operations) + ")");
                    }
                    break;
            }

            return VisitorAction.Continue;
        }

        public override VisitorAction Enter(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            base.Enter(node, parent, path, ancestors);

            if (Operations.Peek() is FilterOperationField field)
            {
                if (field.Operation.Type == typeof(string))
                {
                    if (field.Operation.Kind == FilterOperationKind.Equals)
                    {
                        Level.Peek().Enqueue(field.Operation.Property.Name + " = " + node.Value.Value);
                    }
                }
                return VisitorAction.Skip;
            }
            return VisitorAction.Continue;
        }
    }
}
