using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters
{
    public class FilterVisitorBase
        : SyntaxWalker
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

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            ISyntaxVisitorContext context)
        {
            if (Types.Peek().NamedType() is InputObjectType inputType)
            {
                if (inputType.Fields.TryGetField(node.Name.Value,
                    out IInputField field))
                {
                    Operations.Push(field);
                    Types.Push(field.Type);
                    return Continue;
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

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            ISyntaxVisitorContext context)
        {
            Operations.Pop();
            Types.Pop();
            return Continue;
        }
    }
}
