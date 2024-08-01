namespace HotChocolate.AspNetCore.Authorization;

// ReSharper disable once InconsistentNaming
public sealed class IPAndPort
{
    public IPAndPort(string ipAddress, int port = 0)
    {
        if (port <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(port));
        }

        IPAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        Port = port;
    }

    // ReSharper disable once InconsistentNaming
    public string IPAddress { get; }

    public int Port { get; }
}
