using System.Runtime.Serialization;

namespace HotChocolate.Fusion.Metadata;

[Serializable]
public class ServiceConfigurationException : Exception
{
    public ServiceConfigurationException() { }

    public ServiceConfigurationException(
        string message)
        : base(message) { }
    public ServiceConfigurationException(
        string message,
        Exception inner)
        : base(message, inner) { }

#if NET8_0_OR_GREATER
    [Obsolete(
        "This API supports obsolete formatter-based serialization. " +
        "It should not be called or extended by application code.",
        true)]
#endif
    protected ServiceConfigurationException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context) { }
}
