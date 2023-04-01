using System.Runtime.Serialization;

namespace HotChocolate.Fusion;

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

    protected ServiceConfigurationException(
        SerializationInfo info,
        StreamingContext context)
        : base(info, context) { }
}
