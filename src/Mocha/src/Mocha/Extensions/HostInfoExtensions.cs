namespace Mocha.Middlewares;

/// <summary>
/// Extension members for <see cref="IHostInfo"/>.
/// </summary>
internal static class HostInfoExtensions
{
    extension(IHostInfo host)
    {
        /// <summary>
        /// Gets the effective service name of the host, falling back to the assembly name and
        /// then the instance id when no service name is configured.
        /// </summary>
        internal string EffectiveServiceName
        {
            get
            {
                if (!string.IsNullOrEmpty(host.ServiceName))
                {
                    return host.ServiceName;
                }

                if (!string.IsNullOrEmpty(host.AssemblyName))
                {
                    return host.AssemblyName;
                }

                return host.InstanceId.ToString("D");
            }
        }
    }
}
