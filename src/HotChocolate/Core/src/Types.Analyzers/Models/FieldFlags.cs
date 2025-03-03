namespace HotChocolate.Types.Analyzers.Models;

[Flags]
public enum FieldFlags
{
    None = 0,
    ConnectionEdgesField = 524288,
    ConnectionNodesField = 1048576
}
