#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

internal enum SseEventType : byte
{
    Unknown = 0,
    Next = 1,
    Complete = 2
}
