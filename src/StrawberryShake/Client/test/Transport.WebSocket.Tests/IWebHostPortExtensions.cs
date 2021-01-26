using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace StrawberryShake.Transport.WebSockets
{
    public static class IWebHostPortExtensions
    {
        public static int GetPort(this IWebHost host)
        {
            return host.GetPorts().First();
        }

        public static IEnumerable<int> GetPorts(this IWebHost host)
        {
            return host.GetUris()
                .Select(u => u.Port);
        }

        public static IEnumerable<Uri> GetUris(this IWebHost host)
        {
            return host.ServerFeatures.Get<IServerAddressesFeature>()
                .Addresses
                .Select(a => new Uri(a));
        }
    }
}
