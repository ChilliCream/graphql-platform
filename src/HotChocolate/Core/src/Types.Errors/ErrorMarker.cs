namespace HotChocolate.Types;

public sealed class ErrorMarker
{
    private ErrorMarker()
    {
        // Intentionally left blank.
    }
    
    public static ErrorMarker Instance { get; } = new();
}
