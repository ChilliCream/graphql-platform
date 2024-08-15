using System.Runtime.InteropServices;
using Squadron;

namespace HotChocolate.Data.Raven.Test;

public sealed class CustomRavenDBDefaultOptions : RavenDBDefaultOptions
{
    public override void Configure(ContainerResourceBuilder builder)
    {
        base.Configure(builder);

        builder.Image(RuntimeInformation.ProcessArchitecture is Architecture.Arm64
            ? "ravendb/ravendb:5.4-ubuntu-arm64v8-latest"
            : "ravendb/ravendb:5.4-ubuntu-latest");

        builder.WaitTimeout(120);
    }
}
