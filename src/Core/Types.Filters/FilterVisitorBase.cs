using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Types.Filters
{
    public class FilterVisitorBase
        : SyntaxNodeVisitor
    {
        protected FilterVisitorBase(InputObjectType initialType)
        {
            if (initialType is null)
            {
                throw new ArgumentNullException(nameof(initialType));
            }
            Types.Push(initialType);
        }

        protected Stack<IType> Types { get; } =
            new Stack<IType>();

        protected Stack<IInputField> Operations { get; } =
            new Stack<IInputField>();

        public override VisitorAction Enter(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            if (Types.Peek().NamedType() is InputObjectType inputType)
            {
                if (inputType.Fields.TryGetField(node.Name.Value,
                    out IInputField field))
                {
                    Operations.Push(field);
                    Types.Push(field.Type);
                    return VisitorAction.Continue;
                }

                // TODO : resources - invalid field
                throw new InvalidOperationException();
            }
            else
            {
                // TODO : resources - invalid type
                throw new InvalidOperationException();
            }
        }

        public override VisitorAction Leave(
            ObjectFieldNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Operations.Pop();
            Types.Pop();
            return VisitorAction.Continue;
        }
    }

    public class DummyFilter
        : FilterVisitorBase
    {
        public DummyFilter()

        {

        }

        protected Stack<Stack<string>> Level { get; } =
            new Stack<Stack<String>>();

        public override VisitorAction Enter(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Level.Push(new Stack<string>());
            return VisitorAction.Continue;
        }

        public override VisitorAction Leave(
            ObjectValueNode node,
            ISyntaxNode parent,
            IReadOnlyList<object> path,
            IReadOnlyList<ISyntaxNode> ancestors)
        {
            Stack<string> operations = Level.Pop();
            Level.Peek().Push(string.Join(" AND ", operations));
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
                    Level.Push(new Stack<string>());
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
            Stack<string> operations;

            switch (Operations.Peek())
            {
                case AndField and:
                case FilterOperationField op:
                    operations = Level.Pop();
                    Level.Peek().Push(string.Join(" AND ", operations));
                    break;
                case OrField or:
                    operations = Level.Pop();
                    Level.Peek().Push(string.Join(" OR ", operations));
                    break;
            }

            return VisitorAction.Continue;
        }
    }
}
