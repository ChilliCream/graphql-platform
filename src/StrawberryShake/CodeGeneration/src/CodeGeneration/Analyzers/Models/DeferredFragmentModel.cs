namespace StrawberryShake.CodeGeneration.Analyzers.Models;

public class DeferredFragmentModel
{
    public DeferredFragmentModel(
        string label,
        OutputTypeModel @interface,
        OutputTypeModel @class)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
        Class = @class ?? throw new ArgumentNullException(nameof(@class));
    }

    public string Label { get; }

    public OutputTypeModel Interface { get; }

    public OutputTypeModel Class { get; }
}
