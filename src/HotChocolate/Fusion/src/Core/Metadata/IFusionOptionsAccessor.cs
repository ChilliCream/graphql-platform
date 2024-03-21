using System.Diagnostics;

namespace HotChocolate.Fusion.Metadata;

public interface IFusionOptionsAccessor
{
    /// <summary>
    /// Gets or sets a value indicating whether the <c>Fusion</c> query plan
    /// can be requested on a per request basis.
    ///
    /// The default is <c>false</c>.
    /// </summary>
    bool AllowFusionQueryPlan { get; }

    /// <summary>
    /// Gets or sets a value indicating whether <c>Fusion</c> debugging
    /// information should be included in the response.
    ///
    /// The default value is <see cref="Debugger.IsAttached"/>.
    /// </summary>
    bool IncludeFusionDebugInfo { get; }
}
