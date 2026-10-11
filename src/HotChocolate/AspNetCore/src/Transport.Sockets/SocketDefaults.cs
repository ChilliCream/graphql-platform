#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets;
#else
namespace HotChocolate.Transport.Sockets;
#endif

/// <summary>
/// Socket default setting values.
/// </summary>
public static class SocketDefaults
{
    /// <summary>
    /// The default buffer size.
    /// </summary>
    public const int BufferSize = 1024 * 4;
}
