namespace HotChocolate.AspNetCore.Authorization;

// ReSharper disable once InconsistentNaming
/// <summary>
/// A structure to store information about IP address and port in OPA query request input.
/// </summary>
public sealed class IPAndPort
{
    /// <summary>
    /// Public constructor.
    /// </summary>
    /// <param name="ipAddress">IP address.</param>
    /// <param name="port">Port number.</param>
    /// <exception cref="ArgumentNullException">Thrown if port values is out of range: [0:65535].</exception>
    public IPAndPort(string ipAddress, int port = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, (1 << 16) - 1);

        IPAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        Port = port;
    }

    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// IP address string.
    /// </summary>
    public string IPAddress { get; }

    /// <summary>
    /// Port value.
    /// </summary>
    public int Port { get; }
}
