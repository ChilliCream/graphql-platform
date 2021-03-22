using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Data.Filters
{
    /// <summary>
    /// A logical scope of a visitor.
    /// In case of queryable this would be a closure
    /// <code>
    /// //          /------------------------ SCOPE 1 -----------------------------\
    /// //                                        /----------- SCOPE 2 -------------\
    /// users.Where(x => x.Company.Addresses.Any(y => y.Street == "221B Baker Street"))
    /// </code>
    /// </summary>
    /// <typeparam name="T">The type of the filter definition</typeparam>
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
