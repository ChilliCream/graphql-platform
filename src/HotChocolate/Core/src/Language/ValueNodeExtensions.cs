namespace HotChocolate.Language
{
    public static class ValueNodeExtensions
    {
        public static bool IsNull(this IValueNode value)
        {
            return value is null || value is NullValueNode;
        }
    }
}
