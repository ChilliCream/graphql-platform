namespace HotChocolate.Types.Analyzers.FileBuilders;

public class GroupedDataLoaderInfo
{
    public GroupedDataLoaderInfo(string name, string interfaceName, bool isPublic)
    {
        Name = name;
        InterfaceName = interfaceName;
        IsPublic = isPublic;
        FieldName = "_" + name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
    }

    public string Name { get; }

    public string InterfaceName { get; }

    public string FieldName { get; }

    public bool IsPublic { get; }
}
