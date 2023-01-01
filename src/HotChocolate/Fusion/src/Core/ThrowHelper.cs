using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using HotChocolate.Language;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion;

internal static class ThrowHelper
{
    public static ServiceConfigurationException ServiceConfDocumentMustContainSchemaDef()
        => new(ThrowHelper_ServiceConfDocumentMustContainSchemaDef);

    public static ServiceConfigurationException ServiceConfNoClientsSpecified()
        => new(ThrowHelper_ServiceConfNoClientsSpecified);

    public static ServiceConfigurationException ServiceConfNoTypesSpecified()
        => new(ThrowHelper_ServiceConfNoTypesSpecified);

    public static ServiceConfigurationException ServiceConfInvalidValue(
        Type expected,
        IValueNode actualValue)
        => new(string.Format(
            ThrowHelper_ServiceConfInvalidValue,
            expected.Name,
            actualValue.ToString(),
            actualValue.Kind));

    public static ServiceConfigurationException ServiceConfInvalidDirectiveName(
        string expectedName,
        string actualName)
        => new(string.Format(
            ThrowHelper_ServiceConfInvalidDirectiveName,
            expectedName,
            actualName));

    public static ServiceConfigurationException ServiceConfNoDirectiveArgs(string directiveName)
        => new(string.Format(ThrowHelper_ServiceConfNoDirectiveArgs, directiveName));

    public static ServiceConfigurationException ServiceConfInvalidDirectiveArgs(
        string directiveName,
        IEnumerable<string> expectedArguments,
        IEnumerable<string> actualArguments,
        int line)
        => new(string.Format(
            ThrowHelper_ServiceConfInvalidDirectiveArgs,
            directiveName,
            string.Join(", ", expectedArguments),
            string.Join(", ", actualArguments),
            line));
}

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
