using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// This class helps to group items of the same type on the same level of the tree into a list structure that can be
    /// recognized by visitors.
    /// </summary>
    /// <typeparam name="T">The children's type</typeparam>
    public abstract class TypedSubtree<T> : Visitable where T: IVisitable
    {
        /// <summary>
        /// The content of this typed subtree.
        /// </summary>
        protected readonly List<T> Children;

        protected TypedSubtree(params T[] children)
        {
            Children = children.ToList();
        }

        protected TypedSubtree(List<T> children)
        {
            Children = children;
        }

        public override void Visit(CypherVisitor cypherVisitor)
        {
            cypherVisitor.Enter(this);
            Children.ForEach(child => PrepareVisit(child).Visit(cypherVisitor));
            cypherVisitor.Leave(this);
        }

        /// <summary>
        /// A hook to inject item in visitation of child elements.
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        protected virtual IVisitable PrepareVisit(T child) => child;
    }
}
