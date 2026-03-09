using HotChocolate.Features;

namespace HotChocolate.Fusion.Execution.Introspection;

internal interface ITypeResolverInterceptor
{
    void OnApplyResolver(string fieldName, IFeatureCollection features);
}
