using HotChocolate.Language;

namespace HotChocolate.Stitching.Requests;

internal static class MergeUtils
{
    public static NameNode CreateNewName(this NameNode name, string requestName)
        => new($"{requestName}_{name.Value}");

    public static string CreateNewName(this string name, string requestName)
        => $"{requestName}_{name}";
}
