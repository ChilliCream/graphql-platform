namespace HotChocolate.Language
{
    public static class ValueNodeExtensions
    {
        public static bool IsNull(this IValueNode? value)
        {
            return value is null || value is NullValueNode;
        }

        public static bool HasNull(this IValueNode? value) => !IsNull(value);
    }
}
