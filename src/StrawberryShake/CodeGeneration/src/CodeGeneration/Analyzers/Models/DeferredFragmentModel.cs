using System;

namespace StrawberryShake.CodeGeneration.Analyzers.Models;

public class DeferredFragmentModel
{
    public DeferredFragmentModel(
        string label,
        OutputTypeModel @class,
        OutputTypeModel @interface)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Class = @class ?? throw new ArgumentNullException(nameof(@class));
        Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
    }

    public string Label { get; }

    public OutputTypeModel Class { get; }

    public OutputTypeModel Interface { get; }
}
