using System;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public class FilterVisitorBase<TContext>
        : SyntaxWalker<TContext>
        where TContext : IFilterVisitorContextBase
    {
        protected FilterVisitorBase()
        {
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
                    context.Operations.Push(field);
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

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            TContext context)
        {
            context.Operations.Pop();
            context.Types.Pop();
            return Continue;
        }
    }
}
