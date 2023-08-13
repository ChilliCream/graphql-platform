namespace HotChocolate.OpenApi.Exceptions;

public sealed class OpenApiFieldNameNullException : Exception
{
    public OpenApiFieldNameNullException() : base("Field name cannot be null")
    {
    }
}
