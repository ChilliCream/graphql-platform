using System.Diagnostics;

namespace HotChocolate.Fusion;

public sealed class FusionOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the <c>Fusion</c> query plan
    /// can be requested on a per-request basis.
    ///
    /// The default is <c>false</c>.
    /// </summary>
    public bool AllowQueryPlan { get; set; } = Debugger.IsAttached;

    /// <summary>
    /// Gets or sets a value indicating whether <c>Fusion</c> debugging
    /// information should be included in the response.
    ///
    /// The default value is <see cref="Debugger.IsAttached"/>.
    /// </summary>
    public bool IncludeDebugInfo { get; set; } = Debugger.IsAttached;
}
