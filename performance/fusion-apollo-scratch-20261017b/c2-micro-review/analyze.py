#!/usr/bin/env python3
import hashlib
import json
import statistics
import sys
from pathlib import Path

root = Path(sys.argv[1])
output = Path(sys.argv[2])
launches = json.loads((root / "launches.json").read_text())
assert len(launches) == 8
assert all(x["status"] == "completed" and x["exitCode"] == 0 and not x["ownedChildAlive"] for x in launches)
reports = [(x["label"], json.loads((root / (x["label"] + ".json")).read_text())) for x in launches]
expected = {"ordinary": 0, "unique1": 64, "unique5": 224, "unique6": 264,
            "unique9": 384, "duplicate96-to1": 64, "mixed96-to6": 264,
            "batch9": 768, "transport9": 0, "batch-fallback9": 384}
names = list(expected)
allocation = {}
timing = {}
valid_blocks = 0
for label, report in reports:
    assert report["runtime"] == ".NET 10.0.12" and report["IsServerGC"]
    assert report["variableValuesSize"] == 40
    assert [row["shape"]["Name"] for row in report["rows"]] == names
    config = report["gcConfiguration"]
    assert config["GCDynamicAdaptationMode"] == (0 if report["mode"] == "allocation" else 1)
    if report["mode"] == "allocation":
        assert config["HeapCount"] == 4
    for row in report["rows"]:
        name = row["shape"]["Name"]
        assert row["warmupMilliseconds"] == 3000
        assert row["completedOperations"] == row["warmIterations"] + sum(x["iterations"] for x in row["blocks"])
        assert row["oracle"] == next(r for r in reports[0][1]["rows"] if r["shape"]["Name"] == name)["oracle"]
        if report["mode"] == "allocation":
            assert len(row["blocks"]) == 5
            for block in row["blocks"]:
                assert block["noGc"] and block["noCollections"] and block["collections"] == [0, 0, 0]
                assert block["regionMode"] == 4 and block["endMode"] == 1 and block["endError"] is None
                assert block["completed"] == block["iterations"] == 1000
                assert block["calibrationExpected"] == 523240
                assert all(block[key] == {"ThreadBytes": 523240, "TotalBytes": 523240} for key in ("calibrationBefore", "calibrationAfter"))
                valid_blocks += 1
            values = [b["totalBytes"] / b["iterations"] for b in row["blocks"]]
            allocation.setdefault(name, []).append({
                "label": label, "bytesPerOperation": values,
                "median": statistics.median(values), "mean": statistics.mean(values),
                "min": min(values), "max": max(values),
                "targetMilliseconds": [b["targetElapsedMilliseconds"] for b in row["blocks"]],
                "idleObservations": [{"before": b["idleBefore"], "after": b["idleAfter"]} for b in row["blocks"]]
            })
        else:
            assert len(row["blocks"]) == 12
            values = [b["nanosecondsPerOperation"] for b in row["blocks"]]
            timing.setdefault(name, []).append({
                "label": label, "nanosecondsPerOperation": values,
                "mean": statistics.mean(values), "median": statistics.median(values),
                "min": min(values), "max": max(values), "stdev": statistics.stdev(values),
                "firstThreeMean": statistics.mean(values[:3]), "lastThreeMean": statistics.mean(values[-3:]),
                "lastThreeOverFirstThree": statistics.mean(values[-3:]) / statistics.mean(values[:3]),
                "gcCounts": [sum(b["gc"][g] for b in row["blocks"]) for g in range(3)],
                "warmIterations": row["warmIterations"]
            })
allocation_comparisons = {}
timing_comparisons = {}
for name in names:
    b1, c, b2 = allocation[name]
    allocation_comparisons[name] = {
        "expectedArraySaving": expected[name],
        "medianSavingVersusBefore": b1["median"] - c["median"],
        "medianSavingVersusAfter": b2["median"] - c["median"],
        "allBlockPairSavingRange": [min(b1["min"], b2["min"]) - c["max"], max(b1["max"], b2["max"]) - c["min"]]
    }
    assert allocation_comparisons[name]["medianSavingVersusBefore"] == expected[name]
    assert allocation_comparisons[name]["medianSavingVersusAfter"] == expected[name]
    values = timing[name]
    pairs = []
    for i in (1, 3):
        ratios = [values[i]["mean"] / values[j]["mean"] for j in (i - 1, i + 1)]
        pairs.append({"candidateLabel": values[i]["label"], "ratiosToBothAdjacentControls": ratios,
                      "overFivePercentSlowerThanBoth": all(ratio > 1.05 for ratio in ratios)})
    timing_comparisons[name] = {"pairs": pairs, "repeatedFivePercentRegressionGateFailed": all(p["overFivePercentSlowerThanBoth"] for p in pairs)}
result = {
    "inputs": {x["label"] + ".json": hashlib.sha256((root / (x["label"] + ".json")).read_bytes()).hexdigest() for x in launches},
    "validAllocationBlocks": valid_blocks,
    "timingBlocks": sum(len(x["nanosecondsPerOperation"]) for value in timing.values() for x in value),
    "allocationPolicy": "Four fixed server GC heaps, NoGC, complete process-scoped request/disposal, calibration and raw idle observations.",
    "timingPolicy": "Default dynamic server GC, natural async request/disposal, no allocation instrumentation, all twelve blocks retained.",
    "allocation": allocation, "allocationComparisons": allocation_comparisons,
    "timing": timing, "timingComparisons": timing_comparisons,
    "limitations": ["Process allocation totals include concurrent/background work and operation-state variation. Exact known calibration does not prove request-only attribution.",
                    "Idle observations are empirical context, not a bound on all target-window interference; none were subtracted.",
                    "All block-pair saving ranges are observed extrema, not statistical confidence intervals.",
                    "Three-second warmup does not establish stationarity; all first/last means and GC counts are retained.",
                    "No general CPU speedup is claimed. The allocation proof supports HTTP evaluation only."]
}
output.write_text(json.dumps(result, indent=2) + "\n")
print(json.dumps({"validAllocationBlocks": valid_blocks, "timingBlocks": result["timingBlocks"], "regressionGateFailures": [name for name, value in timing_comparisons.items() if value["repeatedFivePercentRegressionGateFailed"]]}))
