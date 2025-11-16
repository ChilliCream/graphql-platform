namespace HotChocolate.Fusion.Types;

/// <summary>
/// The fusion schema options.
/// </summary>
public interface IFusionSchemaOptions
{
    /// <summary>
    /// Applies the @serializeAs directive to scalar types that specify a serialization format.
    /// </summary>
    bool ApplySerializeAsToScalars { get; }
}
