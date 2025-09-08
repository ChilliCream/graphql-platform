namespace HotChocolate.Types.Descriptors;

/// <summary>
/// I am the most base class.
/// </summary>
public abstract class BaseBaseClass
{
    /// <summary>Summary of foo.</summary>
    public abstract string Foo { get; }

    /// <summary>Method doc.</summary>
    /// <param name="baz">Parameter details.</param>
    public abstract void Bar(string baz);
}
