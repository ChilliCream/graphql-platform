using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters
{
    public class FilterScope<T>
    {
        public FilterScope()
        {
            Level = new Stack<Queue<T>>();
            Instance = new Stack<T>();
            Level.Push(new Queue<T>());
        }

        ///<summary>
        /// Contains a queue for each level of the AST. The queues contain all operations of a level
        /// A new queue is neeeded when entering new <see cref="ObjectValueNode"/>
        ///</summary>
        public Stack<Queue<T>> Level { get; }

        ///<summary>
        /// Stores the current instance. In case of an expression this would be x.Foo.Bar
        ///</summary>
        public Stack<T> Instance { get; }
    }
}
