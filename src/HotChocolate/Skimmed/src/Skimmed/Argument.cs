using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Skimmed;

public sealed class Argument
{
    public Argument(string name, IValueNode value)
    {
        Name = name.EnsureGraphQLName();
        Value = value;
    }

    public string Name { get;  }

    public IValueNode Value { get; }
}
