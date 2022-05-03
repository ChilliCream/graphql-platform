namespace HotChocolate.Language;

public static class ValueNodeExtensions
{
    public static bool IsNull(this IValueNode? value)
        => value is null or NullValueNode;
}
