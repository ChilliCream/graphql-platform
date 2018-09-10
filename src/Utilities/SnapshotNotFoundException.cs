using System;
using System.Runtime.Serialization;

namespace HotChocolate
{
    [Serializable]
    public class SnapshotNotFoundException
        : Exception
    {
        public SnapshotNotFoundException() { }
        public SnapshotNotFoundException(string message)
            : base(message) { }
        public SnapshotNotFoundException(string message, Exception inner)
            : base(message, inner) { }
        protected SnapshotNotFoundException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context) { }
    }
}
