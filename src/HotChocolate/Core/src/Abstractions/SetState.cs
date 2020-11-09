namespace HotChocolate
{
    public delegate void SetState<in T>(T value);

    public delegate void SetState(object value);
}
