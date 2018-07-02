namespace HotChocolate.Language
{
    public interface IValueNode
        : ISyntaxNode
    {
    }

    public interface IValueNode<out T>
        : IValueNode
    {
        T Value { get; }
    }
}
