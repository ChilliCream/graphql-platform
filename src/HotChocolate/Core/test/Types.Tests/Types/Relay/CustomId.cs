using System.Reflection.Metadata.Ecma335;

namespace HotChocolate.Types.Relay;

public record struct CustomId(int Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}
