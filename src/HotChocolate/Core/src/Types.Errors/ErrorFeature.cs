namespace HotChocolate.Types;

internal sealed class ErrorFieldFeature
{
    public List<ErrorConfiguration> ErrorConfigurations { get; } = [];
}

internal sealed class ErrorSchemaFeature
{
    public Type ErrorInterface { get; set; } = typeof(ErrorInterfaceType);

    public ExtendedTypeReference? ErrorInterfaceRef { get; set; }
}

internal sealed class ErrorTypeFeature;
