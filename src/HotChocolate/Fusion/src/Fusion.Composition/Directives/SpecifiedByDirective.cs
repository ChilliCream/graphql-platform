using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Properties.CompositionResources;
using ArgumentNames = HotChocolate.Types.DirectiveNames.SpecifiedBy.Arguments;

namespace HotChocolate.Fusion.Directives;

internal sealed class SpecifiedByDirective(string url)
{
    public string Url { get; } = url;

    public static SpecifiedByDirective From(IDirective directive)
    {
        if (!directive.Arguments.TryGetValue(ArgumentNames.Url, out var urlArg)
            || urlArg is not StringValueNode url)
        {
            throw new InvalidOperationException(SpecifiedByDirective_UrlArgument_Invalid);
        }

        return new SpecifiedByDirective(url.Value);
    }
}
