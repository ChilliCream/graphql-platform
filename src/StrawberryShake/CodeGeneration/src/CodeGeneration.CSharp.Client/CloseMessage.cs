namespace StrawberryShake.CodeGeneration.CSharp;

public sealed class CloseMessage : IMessage
{
    public MessageKind Kind => MessageKind.Close;
}
