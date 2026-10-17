"""Reuse the published native/process accounting for retained stopped-stage cases."""
import argparse
import importlib.util
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("campaign", type=Path)
parser.add_argument("analyzer", type=Path)
args = parser.parse_args()
spec = importlib.util.spec_from_file_location("retained_analysis", args.analyzer)
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
session = args.campaign / "delay0-00-B"
rows = [json.loads(line) for line in (session / "process-samples.jsonl").read_text().splitlines()]
counters, complete = m.baseline.load_counters(session / "counters.json")
reports = []
for name in ["TestQuery", "entities9", "ordinary-fixed"]:
    case = session / name
    r = m.baseline.analyze(case, rows, counters, complete)
    r["validation_valid"] = r["metric_sums"].get("dropped_iterations", 0) == 0
    r["scope"] = "Retained shared baseline only; no candidate comparisons exist"
    r["iteration_boundaries"] = m.iteration_boundaries(case)
    metrics = json.loads((case / "k6_summary.json").read_text())["metrics"]
    r["summary_dropped_iterations"] = metrics.get("dropped_iterations", {}).get("values", {}).get("count", 0)
    assert r["setup_requests_excluded"] == 100
    assert r["summary_dropped_iterations"] == r["metric_sums"].get("dropped_iterations", 0)
    matching = [x for x in rows if r["window_start_epoch"] <= x["time"] <= r["window_end_epoch"]]
    def total_ticks(x):
        values = [int(i) for i in x["system_cpu"].split()[1:]]
        return sum(values[:8]), values[3] + values[4]
    first, last = total_ticks(matching[0]), total_ticks(matching[-1])
    r["system_busy_fraction"] = (last[0] - first[0] - (last[1] - first[1])) / (last[0] - first[0])
    r["monitor_sample_max_gap_seconds"] = max(b["time"] - a["time"] for a, b in zip(matching, matching[1:]))
    r["notes"] += [
        "Parent monitor is pinned to0,8 but is not individually sampled; aggregate systemCPU and countersCPU do not independently establish parent headroom.",
        "No per-core samples exist; aggregate assigned-core headroom cannot exclude brief single-core stalls.",
        "Ordinary fixed-rate drop invalidates that workload control even with complete successful response consistency.",
    ]
    (case / "analysis.json").write_text(json.dumps(r, indent=2) + "\n")
    reports.append(r)
(args.campaign / "retained-baseline-analysis.json").write_text(json.dumps(reports, indent=2) + "\n")
