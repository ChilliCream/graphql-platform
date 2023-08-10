namespace HotChocolate.OpenApi.Exceptions;

public sealed class OpenApiFieldNameNull : Exception
{
    public OpenApiFieldNameNull() : base("Field name cannot be null")
    {
    }
}
