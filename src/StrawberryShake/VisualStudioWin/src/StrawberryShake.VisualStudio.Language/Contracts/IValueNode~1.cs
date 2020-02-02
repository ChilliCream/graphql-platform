namespace StrawberryShake.VisualStudio.Language
{
    public interface IValueNode<out T>
        : IValueNode
    {
        new T Value { get; }
    }
}
