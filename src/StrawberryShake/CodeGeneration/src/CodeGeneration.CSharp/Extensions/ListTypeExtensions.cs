namespace StrawberryShake.CodeGeneration.CSharp.Extensions
{
    public static class ListTypeExtensions
    {
        public static string IfListPrint(this ListType condition, string textToPrint)
        {
            return condition != ListType.NoList ? textToPrint : "";
        }

        public static string IfNullableListPrint(this ListType condition, string textToPrint)
        {
            return condition == ListType.NullableList ? textToPrint : "";
        }
    }
}
