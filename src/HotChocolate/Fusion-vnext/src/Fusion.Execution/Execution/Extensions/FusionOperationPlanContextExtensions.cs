using HotChocolate.Fusion.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Extensions;

internal static class FusionOperationPlanContextExtensions
{
    public static IFusionExecutionDiagnosticEvents GetDiagnosticEvents(this OperationPlanContext context)
        => context.Schema.Services.GetRequiredService<IFusionExecutionDiagnosticEvents>();
}
