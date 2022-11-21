namespace HotChocolate.Stitching;

public sealed class DelegateDirective
{
    private string? path;
    private string schema;

    public string? Path
    {
        get
        {
            return path;
        }
        set
        {
            path = value;
        }
    }

    public string Schema
    {
        get
        {
            return schema;
        }
        set
        {
            schema = value;
        }
    }
}
