namespace HotChocolate.Execution.Serialization;

public sealed partial class MultiPartResultFormatter
{
    private static byte[] ContentType { get; } = "Content-Type: application/json; charset=utf-8"u8.ToArray();

    private static byte[] Start { get; } = "---"u8.ToArray();

    private static byte[] End { get; } = "-----"u8.ToArray();

    private static byte[] CrLf { get; } = "\r\n"u8.ToArray();
}
