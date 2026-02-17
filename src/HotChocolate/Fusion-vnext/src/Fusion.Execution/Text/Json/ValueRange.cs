namespace HotChocolate.Fusion.Text.Json;

internal readonly ref struct ValueRange(int location, int size)
{
    public int Location { get; } = location;
    public int Size { get; } = size;
}
