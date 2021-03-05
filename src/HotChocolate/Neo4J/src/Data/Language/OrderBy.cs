using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// (O,R,D,E,R), SP, (B,Y), SP, SortItem, { ',', [SP], SortItem } ;
    /// </summary>
    public class OrderBy : TypedSubtree<SortItem>, ITypedSubtree
    {
        public override ClauseKind Kind => ClauseKind.OrderBy;
        public OrderBy(List<SortItem> items) : base(items) { }
    }
}
