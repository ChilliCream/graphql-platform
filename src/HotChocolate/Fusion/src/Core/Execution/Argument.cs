using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal readonly struct Argument
{
    public Argument(string name, IValueNode value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; }

    public IValueNode Value { get; }
}
