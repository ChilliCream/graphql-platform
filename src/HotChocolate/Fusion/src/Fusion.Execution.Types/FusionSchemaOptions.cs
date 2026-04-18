namespace HotChocolate.Fusion.Types;

internal struct FusionSchemaOptions : IFusionSchemaOptions
{
    public bool ApplySerializeAsToScalars { get; private set; }

    public bool EnableSemanticIntrospection { get; private set; }

    public static FusionSchemaOptions From(IFusionSchemaOptions? options)
    {
        var copy = new FusionSchemaOptions();

        if (options is not null)
        {
            copy.ApplySerializeAsToScalars = options.ApplySerializeAsToScalars;
            copy.EnableSemanticIntrospection = options.EnableSemanticIntrospection;
        }

        return copy;
    }
}
