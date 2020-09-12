using System;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public abstract class FilterVisitorBase<TContext>
        : SyntaxWalker<TContext>
        where TContext : IFilterVisitorContext
    {
        protected FilterVisitorBase()
        {
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            TContext context)
        {
            context.Operations.Pop();
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
                    out IInputField? field))
                {
                    context.Operations.Push(field);
                    context.Types.Push(field.Type);
                    return Continue;
                }

                throw new InvalidOperationException(DataResources.FilterVisitor_InvalidField);
            }
            else
            {
                throw new InvalidOperationException(DataResources.FilterVisitor_InvalidType);
            }
        }
    }
}
