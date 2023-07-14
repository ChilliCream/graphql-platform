using System.Text.Json;

namespace HotChocolate.Fusion.Composition.Features;

/// <summary>
/// Defines a feature of the composition process.
/// </summary>
public interface IFusionFeature { }

/// <summary>
/// This interface is used to specify the parser of a feature.
/// </summary>
/// <typeparam name="T">
/// The feature type.
/// </typeparam>
public interface IFusionFeatureParser<out T>
{
    static abstract T Parse(JsonElement value);
}