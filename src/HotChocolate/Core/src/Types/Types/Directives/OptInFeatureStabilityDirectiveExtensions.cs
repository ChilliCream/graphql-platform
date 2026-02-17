namespace HotChocolate.Types;

public static class OptInFeatureStabilityDirectiveExtensions
{
    /// <summary>
    /// Adds an opt-in feature stability directive to the schema,
    /// allowing you to specify the stability level of a feature.
    /// </summary>
    /// <param name="descriptor">
    /// The <see cref="ISchemaTypeDescriptor"/>.
    /// </param>
    /// <param name="feature">
    /// The name of the feature for which to set the stability.
    /// </param>
    /// <param name="stability">
    /// The stability level of the feature.
    /// </param>
    /// <returns>
    /// The <see cref="ISchemaTypeDescriptor"/> so that configuration can be chained.
    /// </returns>
    public static ISchemaTypeDescriptor OptInFeatureStability(
        this ISchemaTypeDescriptor descriptor,
        string feature,
        string stability)
    {
        return descriptor.Directive(new OptInFeatureStabilityDirective(feature, stability));
    }
}
