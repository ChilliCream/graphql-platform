using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// Items yielded by a stand alone or in query call.
    /// </summary>
    public class YieldItems : TypedSubtree<Expression, YieldItems>
    {
        public new ClauseKind Kind => ClauseKind.YieldItems;

        private static YieldItems YieldAllOf(Expression[] c) =>
        (c == null || c.Length == 0) ?
        throw new ArgumentException("Cannot yield an empty list of items.") : new YieldItems(c);

        public YieldItems(Expression[] children) : base(children) { }
    }
}