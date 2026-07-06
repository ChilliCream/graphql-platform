using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Nodes.Serialization;
using HotChocolate.Fusion.Planning;

namespace HotChocolate.Fusion;

// TEMP DIAGNOSTIC (revert): RCA harness for the two gateway follow-on questions.
public sealed class _RcaDiag
{
    private const string OutDir =
        "/private/tmp/claude-501/-Users-michael-local-hc-1-repo/dd0e966b-0c00-47c0-8d06-02fbf51d3d57/scratchpad";

    [Fact]
    public async Task Q1_ByExpert()
    {
        await RunAsync(
            "q1-byexpert",
            """
            {
              feed {
                byExpert
              }
            }
            """,
            (Suites.RequiresCircular.A.ASubgraph.Name, Suites.RequiresCircular.A.ASubgraph.BuildAsync),
            (Suites.RequiresCircular.B.BSubgraph.Name, Suites.RequiresCircular.B.BSubgraph.BuildAsync));
    }

    [Fact]
    public async Task Q1b_ByNovice_Control()
    {
        await RunAsync(
            "q1b-bynovice",
            """
            {
              feed {
                byNovice
              }
            }
            """,
            (Suites.RequiresCircular.A.ASubgraph.Name, Suites.RequiresCircular.A.ASubgraph.BuildAsync),
            (Suites.RequiresCircular.B.BSubgraph.Name, Suites.RequiresCircular.B.BSubgraph.BuildAsync));
    }

    [Fact]
    public async Task Q2_WithArgument_AuthorIds()
    {
        await RunAsync(
            "q2-author-ids",
            """
            {
              feed {
                author {
                  id
                }
              }
            }
            """,
            (Suites.RequiresWithArgument.A.ASubgraph.Name, Suites.RequiresWithArgument.A.ASubgraph.BuildAsync),
            (Suites.RequiresWithArgument.B.BSubgraph.Name, Suites.RequiresWithArgument.B.BSubgraph.BuildAsync),
            (Suites.RequiresWithArgument.C.CSubgraph.Name, Suites.RequiresWithArgument.C.CSubgraph.BuildAsync),
            (Suites.RequiresWithArgument.D.DSubgraph.Name, Suites.RequiresWithArgument.D.DSubgraph.BuildAsync));
    }

    private static async Task RunAsync(
        string label,
        string query,
        params (string Name, Func<Task<SubgraphHost>> Factory)[] subgraphs)
    {
        OperationPlan? capturedPlan = null;
        FusionGatewayBuilder.DiagnosticConfigure = b =>
            b.AddOperationPlannerInterceptor(_ => new CapturingInterceptor(p => capturedPlan = p));

        var capture = new SubgraphRequestCapture();
        var sb = new StringBuilder();

        try
        {
            await using var gateway = await FusionGatewayBuilder.ComposeAsync(capture, subgraphs);
            var result = await gateway.Executor.ExecuteAsync(query);
            var json = result.ToJson(withIndentations: true);

            sb.AppendLine("========== QUERY ==========");
            sb.AppendLine(query);
            sb.AppendLine();
            sb.AppendLine("========== RESULT ==========");
            sb.AppendLine(json);
            sb.AppendLine();
            sb.AppendLine("========== PLAN (yaml) ==========");
            if (capturedPlan is not null)
            {
                sb.AppendLine(new YamlOperationPlanFormatter().Format(capturedPlan));
            }
            else
            {
                sb.AppendLine("<no plan captured>");
            }
            sb.AppendLine();
            sb.AppendLine("========== SUBGRAPH REQUESTS (in order) ==========");
            var requests = capture.Requests;
            sb.AppendLine($"count = {requests.Count}");
            for (var i = 0; i < requests.Count; i++)
            {
                sb.AppendLine($"--- request {i + 1} -> '{requests[i].SubgraphName}' ---");
                sb.AppendLine(Pretty(requests[i].Body));
                sb.AppendLine();
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("========== EXCEPTION ==========");
            sb.AppendLine(ex.ToString());
        }
        finally
        {
            FusionGatewayBuilder.DiagnosticConfigure = null;
        }

        Directory.CreateDirectory(OutDir);
        await File.WriteAllTextAsync(Path.Combine(OutDir, $"rca-{label}.txt"), sb.ToString());
    }

    private static string Pretty(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return JsonSerializer.Serialize(
                doc,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
        }
        catch
        {
            return body;
        }
    }

    private sealed class CapturingInterceptor : IOperationPlannerInterceptor
    {
        private readonly Action<OperationPlan> _onPlan;

        public CapturingInterceptor(Action<OperationPlan> onPlan) => _onPlan = onPlan;

        public void OnAfterPlanCompleted(
            OperationDocumentInfo operationDocumentInfo,
            OperationPlan operationPlan)
            => _onPlan(operationPlan);
    }
}
