namespace HotChocolate.Language
{
    public interface IValueNode<out T>
        : IValueNode
    {
        new T Value { get; }
    }
}
