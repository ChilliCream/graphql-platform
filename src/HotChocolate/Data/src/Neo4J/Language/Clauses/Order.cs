using System;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// (O,R,D,E,R), SP, (B,Y), SP, SortItem, { ',', [SP], SortItem } ;
    /// </summary>
    public class Order : Visitable
    {
        public Order() { }
    }
}