namespace HotChocolate.Types.Relay;

public interface INodeIdRuntimeTypeLookup
{
    Type? GetNodeIdRuntimeType(string typeName);
}
