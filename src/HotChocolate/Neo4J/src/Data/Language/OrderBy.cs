using System;
using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// (O,R,D,E,R), SP, (B,Y), SP, SortItem, { ',', [SP], SortItem } ;
    /// </summary>
    public class OrderBy : Visitable
    {
        public override ClauseKind Kind => ClauseKind.OrderBy;
        private readonly List<SortItem> _items;
        public OrderBy(List<SortItem> items)
        {
            _items = items;
        }

        public override void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _items.ForEach(element => element.Visit(visitor));
            visitor.Leave(this);
        }
    }
}
