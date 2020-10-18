using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Data.Projections.Expressions
{
    internal static class ProjectionExpressionBuilder
    {
        public static MemberInitExpression CreateMemberInit(
            Type type,
            IEnumerable<MemberBinding> expressions)
        {
            NewExpression ctor = Expression.New(type);
            return Expression.MemberInit(ctor, expressions);
        }
    }
}
