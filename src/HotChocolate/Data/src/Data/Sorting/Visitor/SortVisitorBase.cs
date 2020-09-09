using System;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    public abstract class SortVisitorBase<TContext>
        : SyntaxWalker<TContext>
        where TContext : ISortVisitorContext
    {
        protected SortVisitorBase()
        {
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            TContext context)
        {
            context.Fields.Pop();
            context.Types.Pop();
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            TContext context)
        {
            if (context.Types.Peek().NamedType() is InputObjectType inputType)
            {
                if (inputType.Fields.TryGetField(node.Name.Value,
                    out IInputField field))
                {
                    context.Fields.Push(field);
                    context.Types.Push(field.Type);
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
    }
}
