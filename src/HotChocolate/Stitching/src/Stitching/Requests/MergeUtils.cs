using HotChocolate.Language;

namespace HotChocolate.Stitching.Requests
{
    internal static class MergeUtils
    {
        public static NameNode CreateNewName(
            this NameNode name,
            NameString requestName)
        {
            return new NameNode($"{requestName}_{name.Value}");
        }

        public static NameString CreateNewName(
            this NameString name,
            NameString requestName)
        {
            return $"{requestName}_{name.Value}";
        }
    }
}
