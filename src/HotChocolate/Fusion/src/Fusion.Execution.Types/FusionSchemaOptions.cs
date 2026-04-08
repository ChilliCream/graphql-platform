namespace HotChocolate.Fusion.Types;

internal struct FusionSchemaOptions : IFusionSchemaOptions
{
    public bool ApplySerializeAsToScalars { get; private set; }

    public bool EnableDefer { get; private set; } = true;

    public FusionSchemaOptions() { }

    public static FusionSchemaOptions From(IFusionSchemaOptions? options)
    {
        var copy = new FusionSchemaOptions();

        if (options is not null)
        {
            copy.ApplySerializeAsToScalars = options.ApplySerializeAsToScalars;
            copy.EnableDefer = options.EnableDefer;
        }

        return copy;
    }
}
