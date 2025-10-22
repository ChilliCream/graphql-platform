namespace HotChocolate.Types;

public static class OptInFeatureStabilityDirectiveExtensions
{
    public static ISchemaTypeDescriptor OptInFeatureStability(
        this ISchemaTypeDescriptor descriptor,
        string feature,
        string stability)
    {
        return descriptor.Directive(new OptInFeatureStabilityDirective(feature, stability));
    }
}
