#if FUSION
namespace HotChocolate.Fusion.Transport.Sockets;
#else
namespace HotChocolate.Transport.Sockets;
#endif

internal static class Delimiter
{
    public const byte EndOfText = 3;
}
