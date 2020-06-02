using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters.Expressions
{
    public static class SkipFieldHandler
    {
        public static bool Enter(
            FilterOperationField _,
            ObjectFieldNode __,
            IFilterVisitorContext<Expression> ___,
            [NotNullWhen(true)] out ISyntaxVisitorAction? action)
        {
            action = SyntaxVisitor.Skip;
            return true;
        }
    }
}
