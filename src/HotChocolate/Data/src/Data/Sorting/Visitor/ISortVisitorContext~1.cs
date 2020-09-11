using System.Collections.Generic;

namespace HotChocolate.Data.Sorting
{
    public interface ISortVisitorContext<T>
        : ISortVisitorContext
    {
        ///<summary>
        /// Stores all sort operations
        ///</summary>
        public Queue<T> Operations { get; }

        ///<summary>
        /// Stores the current instance. In case of an expression this would be x.Foo.Bar
        ///</summary>
        public Stack<T> Instance { get; }
    }
}
