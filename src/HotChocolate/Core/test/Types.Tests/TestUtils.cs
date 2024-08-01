namespace HotChocolate;

public static class TestUtils
{
    public static string CreateTypeName()
    {
        return "Type_" + Guid.NewGuid().ToString("N");
    }

    public static string CreateFieldName()
    {
        return "field_" + Guid.NewGuid().ToString("N");
    }
}
