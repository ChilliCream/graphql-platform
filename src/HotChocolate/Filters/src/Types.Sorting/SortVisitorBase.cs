using System;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Sorting.Properties;

namespace HotChocolate.Types.Sorting
{
    public class SortVisitorBase<TContext>
        : SyntaxWalker<TContext>
        where TContext : ISortVisitorContextBase
    {
        protected SortVisitorBase()
        {
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            TContext context)
        {
            if (context.Types.Peek().NamedType() is InputObjectType inputType)
            {
                if (inputType.Fields.TryGetField(node.Name.Value, out IInputField? field))
                {
                    context.Operations.Push(field);
                    context.Types.Push(field.Type);
                    return Continue;
                }

                throw new InvalidOperationException(
                    SortingResources.SortObjectTypeFieldVisitor_InvalidType);
            }
            else
            {
                throw new InvalidOperationException(
                    SortingResources.SortObjectTypeVisitor_InvalidType);
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
