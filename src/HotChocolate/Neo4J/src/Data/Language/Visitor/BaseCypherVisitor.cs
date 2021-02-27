using System.Collections.Generic;

namespace HotChocolate.Data.Neo4J.Language
{
    /// <summary>
    /// This is a convenience class.
    /// </summary>
    public abstract class BaseCypherVisitor : IVisitor
    {
        private readonly Queue<IVisitable> _currentVisitedElements = new ();

        /// <summary>
        /// This is a hook that is called with the uncasted, raw visitable just before entering a visitable.
        /// The hook is called regardless wither a matching enter is found or not.
        /// </summary>
        /// <param name="visitable">The visitable that is passed on to a matching enter after this call.</param>
        protected abstract bool PreEnter(IVisitable visitable);

        /// <summary>
        /// This is a hook that is called with the uncasted, raw visitable just after leaving the visitable.
        /// The hook is called regardless wither a matching leave is found or not.
        /// </summary>
        /// <param name="visitable">The visitable that is passed on to a matching leave after this call.</param>
        protected abstract void PostLeave(IVisitable visitable);


        public void Enter(IVisitable visitable)
        {
            if (PreEnter(visitable))
            {
                _currentVisitedElements.Enqueue(visitable);
            }
        }

        public void Leave(IVisitable visitable)
        {
            if (!Equals(_currentVisitedElements.Peek(), visitable)) return;
            PostLeave(visitable);
            _currentVisitedElements.Dequeue();
        }
    }
}
