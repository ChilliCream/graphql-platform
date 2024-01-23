namespace HotChocolate.Fusion.Metadata;

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
}
