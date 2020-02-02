namespace HotChocolate.Types
{
    internal static class ScalarExtensions
    {
        public static bool IsDefinedInSpec(this ScalarType type)
        {
            return type.GetType().IsDefined(
                typeof(SpecScalarAttribute),
                false);
        }
    }
}
