using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace HotChocolate.AspNetCore.Authorization;

public class OpaProcess
{
    private readonly IContainer _container;

    public OpaProcess(IContainer container)
    {
        _container = container;
    }
    public static async Task<OpaProcess> StartServerAsync()
    {
        var opaProcess = new OpaProcess(new ContainerBuilder()
            .WithImage("openpolicyagent/opa")
            .WithPortBinding(8181, 8181)
            .WithCommand(
                "run", "--server",
                "--addr", ":8181",
                "--log-level", "debug",
                "--set", "decision_logs.console=true")
            // Wait until the HTTP endpoint of the container is available.
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8181)))
            // Build the container configuration.
            .Build());
        await opaProcess._container.StartAsync();
        return opaProcess;
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
