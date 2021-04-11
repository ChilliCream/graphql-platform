using System;

namespace HotChocolate.Data.Neo4J.Language
{
    public class YieldItems
        : TypedSubtree<Expression>, ITypedSubtree
    {
        public override ClauseKind Kind => ClauseKind.YieldItems;

        public static YieldItems YieldAllOf(params Expression[] c)
        {
            if (c == null || c.Length == 0)
            {
                throw new InvalidOperationException("Cannot yield an empty list of items.");
            }

            return new YieldItems(c);
        }

        private YieldItems(params Expression[] children)
            : base(children) { }
    }
}
