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

        /// <summary>
        /// Creates a new typed subtree with the given content.
        /// </summary>
        /// <param name="children">The content of this subtree.</param>
        protected TypedSubtree(params T[] children)
        {
            Children = children.ToList();
        }

        /// <summary>
        /// Creates a new typed subtree with the given content.
        /// </summary>
        /// <param name="children">The content of this subtree.</param>
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
        /// <param name="child">The current child element.</param>
        /// <returns>The visitable that has been prepared</returns>
        protected virtual IVisitable PrepareVisit(T child) => child;
    }
}
