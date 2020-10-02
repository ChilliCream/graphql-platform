using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections
{
    public class ProjectionScope<T>
    {
        public ProjectionScope()
        {
            Instance = new Stack<T>();
        }

        ///<summary>
        /// Stores the current instance. In case of an expression this would be x.Foo.Bar
        ///</summary>
        public Stack<T> Instance { get; }
    }
}
