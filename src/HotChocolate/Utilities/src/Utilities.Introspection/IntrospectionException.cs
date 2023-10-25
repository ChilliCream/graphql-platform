using System;
using System.Runtime.Serialization;

namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// This exception is thrown if an error happens during introspection.
/// </summary>
[Serializable]
public class IntrospectionException : Exception
{
    public IntrospectionException() { }
        
    public IntrospectionException(string message)
        : base(message) { }

    public IntrospectionException(string message, Exception inner)
        : base(message, inner) { }

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. " +
        "It should not be called or extended by application code.",
        true)]
#endif
    protected IntrospectionException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context) { }
}