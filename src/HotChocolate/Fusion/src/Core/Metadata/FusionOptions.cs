using System.Diagnostics;

namespace HotChocolate.Fusion.Metadata;

public class FusionOptions : IFusionOptionsAccessor
{
    /// <summary>
    /// Gets or sets a value indicating whether the <c>Fusion</c> query plan
    /// can be requested on a per request basis.
    ///
    /// The default is <c>false</c>.
    /// </summary>
    public bool AllowFusionQueryPlan { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether <c>Fusion</c> debugging
    /// information should be included in the response.
    ///
    /// The default value is <see cref="Debugger.IsAttached"/>.
    /// </summary>
    public bool IncludeFusionDebugInfo { get; set; } = Debugger.IsAttached;
}
