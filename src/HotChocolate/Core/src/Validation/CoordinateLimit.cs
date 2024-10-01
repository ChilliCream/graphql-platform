namespace HotChocolate.Validation;

internal sealed class CoordinateLimit
{
    public ushort MaxAllowed { get; private set; }

    public ushort Count { get; private set; }

    public bool Add()
    {
        if (Count < MaxAllowed)
        {
            Count++;
            return true;
        }

        return false;
    }

    public void Remove() => Count--;

    public void Reset(ushort maxAllowed)
    {
        MaxAllowed = maxAllowed;
        Count = 0;
    }
}
