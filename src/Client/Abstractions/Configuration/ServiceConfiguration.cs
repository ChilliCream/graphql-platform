using System.Collections.Generic;

namespace StrawberryShake.Configuration
{
    public class ServiceConfiguration
        : List<ServiceDescriptor>
        , IServiceConfiguration
    {
    }
}
