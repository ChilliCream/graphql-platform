namespace HotChocolate.Transport.Http;

internal enum SseEventType : byte
{
    Unknown = 0,
    Next = 1,
    Complete = 2
}
