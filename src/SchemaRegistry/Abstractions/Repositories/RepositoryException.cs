using System;

namespace MarshmallowPie.Repositories
{
    [System.Serializable]
    public class RepositoryException : Exception
    {
        public RepositoryException() { }
        public RepositoryException(string message) : base(message) { }
        public RepositoryException(string message, System.Exception inner) : base(message, inner) { }
        protected RepositoryException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [System.Serializable]
    public class DuplicateKeyException : RepositoryException
    {
        public DuplicateKeyException() { }
        public DuplicateKeyException(string message) : base(message) { }
        public DuplicateKeyException(string message, System.Exception inner) : base(message, inner) { }
        protected DuplicateKeyException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
