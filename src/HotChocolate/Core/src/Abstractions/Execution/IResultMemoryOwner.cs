using System;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// This interface represents the owner of the rented objects associated 
    /// with the result data structure. 
    /// 
    /// When this object is disposed it will return the objects representing the 
    /// <see cref="Data" /> object structure back to the object pools.
    /// </summary>
    public interface IResultMemoryOwner : IDisposable
    {
        /// <summary>
        /// The data object structure representing the GraphQL result.
        /// </summary>
        IResultMap? Data { get; }
    }
}
