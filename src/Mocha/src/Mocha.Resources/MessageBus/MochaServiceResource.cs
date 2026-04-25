using System.Text.Json;

namespace Mocha.Resources;

/// <summary>
/// <see cref="MochaResource"/> describing a single service host instance running a Mocha message bus.
/// </summary>
internal sealed class MochaServiceResource : MochaResource
{
    private readonly string _id;
    private readonly string? _serviceName;
    private readonly string? _assemblyName;
    private readonly string _instanceId;

    public MochaServiceResource(HostDescription description)
    {
        _serviceName = description.ServiceName;
        _assemblyName = description.AssemblyName;
        _instanceId = description.InstanceId;
        _id = MochaUrn.Create("core", "service", description.InstanceId);
    }

    public override string Kind => "mocha.service";

    public override string Id => _id;

    public override void Write(Utf8JsonWriter writer)
    {
        if (_serviceName is not null)
        {
            writer.WriteString("service_name", _serviceName);
        }

        if (_assemblyName is not null)
        {
            writer.WriteString("assembly_name", _assemblyName);
        }

        writer.WriteString("instance_id", _instanceId);
    }
}
