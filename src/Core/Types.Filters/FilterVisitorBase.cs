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
}
