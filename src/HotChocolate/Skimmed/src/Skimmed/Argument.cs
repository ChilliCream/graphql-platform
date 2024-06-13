using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

/// <summary>
/// Represents a field or directive argument value.
/// </summary>
public sealed class Argument
{
    public Argument(string name, string value)
        : this(name, new StringValueNode(value))
    {
    }

    public Argument(string name, int value)
        : this(name, new IntValueNode(value))
    {
    }

    public Argument(string name, double value)
        : this(name, new FloatValueNode(value))
    {
    }

    public Argument(string name, bool value)
        : this(name, new BooleanValueNode(value))
    {
    }

    public Argument(string name, IValueNode value)
    {
        Name = name.EnsureGraphQLName();
        Value = value;
    }

    /// <summary>
    ///
    /// </summary>
    /// <value></value>
    public string Name { get;  }

    public IValueNode Value { get; }

    public override string ToString()
        => RewriteArgument(this).ToString(true);
}
