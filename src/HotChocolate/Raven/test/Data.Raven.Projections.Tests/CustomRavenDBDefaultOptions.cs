using System.Runtime.InteropServices;
using Squadron;

namespace HotChocolate.Data.Raven;

public sealed class CustomRavenDBDefaultOptions : RavenDBDefaultOptions
{
    public override void Configure(ContainerResourceBuilder builder)
    {
        base.Configure(builder);
        if (RuntimeInformation.ProcessArchitecture is Architecture.Arm64)
        {
            builder.Image("ravendb/ravendb:5.4-ubuntu-arm64v8-latest");
        }

        builder.WaitTimeout(120);
    }
}
