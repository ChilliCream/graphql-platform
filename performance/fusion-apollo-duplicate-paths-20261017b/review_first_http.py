"""Reproduce first HTTP launch accounting from retained native data, without traffic."""
import importlib.util
import json
from pathlib import Path
import sys

here = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("baseline_analysis", here / "http/vendor/analyze.py")
analysis = importlib.util.module_from_spec(spec)
spec.loader.exec_module(analysis)
base = Path(sys.argv[1]) if len(sys.argv) > 1 else here / "results/http-initial-baseline"
session = base / "delay0-00-B"
rows = [json.loads(line) for line in (session / "process-samples.jsonl").read_text().splitlines()]
counters, complete = analysis.load_counters(session / "counters.json")
reports = [analysis.analyze(session / name, rows, counters, complete)
           for name in ["TestQuery", "entities9", "ordinary-fixed"]]
(base / "partial-analysis.json").write_text(json.dumps(reports, indent=2) + "\n")
for result in reports:
    print(Path(result["case"]).name, result["successes"], result["metric_sums"].get("dropped_iterations", 0),
          result["gateway_cpu_ms_per_success"])
