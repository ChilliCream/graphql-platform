using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Data.Projections
{
    /// <summary>
    /// A logical scope of the projection visitor.
    /// </summary>
    /// <typeparam name="T">The type of the filter definition</typeparam>
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
