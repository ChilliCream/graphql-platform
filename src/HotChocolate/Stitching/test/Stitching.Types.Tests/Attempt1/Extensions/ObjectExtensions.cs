namespace HotChocolate.Stitching.Types.Attempt1;

public static class ObjectExtensions
{
    public static bool IsEqualTo(this object? x, object? y)
    {
        if (x is null)
        {
            return y is null;
        }

        if (y is null)
        {
            return false;
        }

        return x.Equals(y);
    }
}
