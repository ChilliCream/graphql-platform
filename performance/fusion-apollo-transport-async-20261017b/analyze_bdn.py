#!/usr/bin/env python3
from pathlib import Path
import hashlib
import json
import statistics
import sys

root = Path(sys.argv[1])
launches = []
for path in sorted(root.glob("0[1-5]-benchmark-*/results/*-full.json")):
    report = json.loads(path.read_text())
    cases = {}
    for result in report["Benchmarks"]:
        values = {}
        for stage in ["Actual", "Result"]:
            samples = [row["Nanoseconds"] / row["Operations"]
                       for row in result["Measurements"]
                       if row["IterationMode"] == "Workload" and row["IterationStage"] == stage]
            assert len(samples) == 12
            values[stage] = samples
        shape = result["Parameters"].split("=", 1)[1]
        cases[shape] = {
            "nanosecondsPerOperation": values,
            "meanNanoseconds": statistics.mean(values["Result"]),
            "standardDeviationNanoseconds": statistics.stdev(values["Result"]),
            "nativeMemory": result["Memory"]
        }
    assert set(cases) == {"ordinary", "async-ordinary", "async-alias2", "async-custom"}
    launches.append({"label": path.parents[1].name,
                     "report": str(path.relative_to(root)),
                     "reportSha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                     "cases": cases})
assert len(launches) == 5
comparisons = []
for index in [1, 3]:
    for shape, candidate in launches[index]["cases"].items():
        controls = []
        for control_index in [index - 1, index + 1]:
            baseline = launches[control_index]["cases"][shape]
            controls.append({"label": launches[control_index]["label"],
                             "elapsedRatio": candidate["meanNanoseconds"] / baseline["meanNanoseconds"],
                             "elapsedPercent": 100 * (candidate["meanNanoseconds"] / baseline["meanNanoseconds"] - 1),
                             "nativeByteDelta": candidate["nativeMemory"]["BytesAllocatedPerOperation"] - baseline["nativeMemory"]["BytesAllocatedPerOperation"]})
        comparisons.append({"candidate": launches[index]["label"], "shape": shape, "controls": controls})
allocation = []
for path in sorted(root.glob("*allocation*/allocation.jsonl")):
    allocation.append({"label": path.parent.name,
                       "observations": [json.loads(line) for line in path.read_text().splitlines()]})
output = {"method": "Arithmetic mean of all12 native WorkloadResult observations, without trimming; WorkloadActual and full original reports retained. Elapsed is not measured process CPU.",
          "nativeLaunchCount": 5, "nativeMemoryRecordCount": 20,
          "actualObservationCount": 240, "launches": launches,
          "comparisonsAgainstBothAdjacentControls": comparisons,
          "controlledAllocation": allocation,
          "controlledAllocationComplete": False,
          "primaryVerdict": "ABANDON",
          "reason": "Reliable native allocation savings do not justify repeated or unresolved material elapsed costs. Optional allocation calibration failure is not the rejection basis."}
(root / "analysis.json").write_text(json.dumps(output, indent=2) + "\n")
print(json.dumps({key: output[key] for key in ["nativeLaunchCount", "nativeMemoryRecordCount", "actualObservationCount", "primaryVerdict"]}))
