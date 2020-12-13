using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// This class helps to group items of the same type on the same level of the tree into a list structure that can be
    /// recognized by visitors.
    /// </summary>
    /// <typeparam name="TType">The children's type</typeparam>
    /// <typeparam name="TTree">The concrete type of the implementing class.</typeparam>
    public abstract class TypedSubtree<TType, TTree> : Visitable
    where TType : Visitable
    where TTree : TypedSubtree<TType, TTree>
    {
        private readonly List<TType> _children;

        protected TypedSubtree(List<TType> children)
        {
            _children = children;
        }

        protected TypedSubtree(TType[] children)
        {
            _children = children.ToList();
        }

        public new void Visit(CypherVisitor visitor)
        {
            visitor.Enter(this);
            _children.ForEach(child => child.Visit(visitor));
            visitor.Leave(this);
        }

        public List<TType> GetChildren() => _children;

        /// <summary>
        /// A hook for interfere with the visitation of child elements.
        /// </summary>
        /// <param name="child">The current child element</param>
        /// <returns>The visitable that has been prepared</returns>
        protected IVisitable PrepareVisit(TType child) => child;
    }
}
