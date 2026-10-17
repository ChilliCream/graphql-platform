"""Reduce saved HTTP accounting after measurement has stopped."""
import argparse
import bisect
import datetime
import json
import pathlib
import statistics

parser = argparse.ArgumentParser()
parser.add_argument("variants", nargs="+", type=pathlib.Path)
parser.add_argument("--output", required=True, type=pathlib.Path)
parser.add_argument("--phase", default="matrix")
args = parser.parse_args()
runs = []


def utc(value):
    return datetime.datetime.fromisoformat(value).timestamp()


def interpolate(samples, instant):
    times = [row[0] for row in samples]
    index = bisect.bisect_right(times, instant)
    if index == 0 or index == len(samples):
        return None
    (start, first), (end, last) = samples[index - 1:index + 1]
    return first + (last - first) * (instant - start) / (end - start)


for variant in args.variants:
    for path in sorted(path for phase in variant.glob(args.phase) for path in phase.glob("*/rep-*/accounting.json")):
        folder = path.parent
        data = json.loads(path.read_text())
        first, last = data["before"], data["after"]
        summary = json.loads((folder / "summary.json").read_text())["metrics"]
        boundaries = json.loads((folder / "boundaries.json").read_text())
        count = summary["completed"]["values"]["count"]
        elapsed = (last["timestamp"] - first["timestamp"]) / first["frequency"]
        cpu = (last["cpuMs"] - first["cpuMs"]) / 1000
        allocation = last["allocated"] - first["allocated"]
        observations = [json.loads(line) for line in (folder / "processes.jsonl").read_text().splitlines()]
        decreases = json.loads((folder / "counter-decreases.json").read_text())
        assert not decreases and allocation > 0, folder
        assert summary["success"]["values"]["rate"] == 1, folder
        assert boundaries["exitCode"] == 0, folder
        utilization = {}
        for name, process in boundaries["before"]["processes"].items():
            pid = process["pid"]
            samples = [(boundaries["before"]["utc"], process["cpu"])]
            for row in observations:
                matches = [p for p in row["processes"] if p["pid"] == pid]
                if matches:
                    samples.append((row["time"], matches[0]["cpu"]))
            samples.append((boundaries["after"]["utc"], boundaries["after"]["processes"][name]["cpu"]))
            samples.sort()
            start = interpolate(samples, utc(first["utc"]))
            end = interpolate(samples, utc(last["utc"]))
            utilization[name] = (end - start) / elapsed if start is not None and end is not None else None
        gateway_pid = boundaries["before"]["processes"]["Gateway"]["pid"]
        peak_rss = max(p["rss"] for row in observations for p in row["processes"] if p["pid"] == gateway_pid)
        peak_rss = max(peak_rss, first["workingSet"], last["workingSet"],
            *(row["gateway"]["workingSet"] for row in observations))
        usage = json.loads((folder / "loadgen-usage.json").read_text())
        row = dict(variant=variant.name, case=folder.parent.name, repetition=folder.name,
            artifact=str(folder), count=count, elapsed=elapsed, requestsPerSecond=count / elapsed,
            cpuSeconds=cpu, cpuMsPerRequest=cpu * 1000 / count, allocationBytesPerRequest=allocation / count,
            allocationBytesPerSecond=allocation / elapsed,
            gatewayCores=cpu / elapsed, processCores=utilization,
            subgraphCores=sum(value for name, value in utilization.items() if name != "Gateway"),
            procCpuSeconds=utilization["Gateway"] * elapsed if utilization["Gateway"] is not None else None,
            loadGeneratorCores=(usage["userCpu"] + usage["systemCpu"]) / elapsed,
            gen0=last["gen0"] - first["gen0"], gen1=last["gen1"] - first["gen1"], gen2=last["gen2"] - first["gen2"],
            gcPauseMs=last["pauseMs"] - first["pauseMs"],
            gcPausePercent=100 * (last["pauseMs"] - first["pauseMs"]) / (elapsed * 1000),
            heapEnd=last["heap"], fragmentationEnd=last["fragmented"], committedEnd=last["committed"],
            workingSetEnd=last["workingSet"], observedPeakRss=peak_rss,
            failures=summary["success"]["values"]["fails"],
            droppedStarts=summary.get("dropped_iterations", {}).get("values", {}).get("count", 0),
            responseBytesMin=summary["body_bytes"]["values"]["min"], responseBytesMax=summary["body_bytes"]["values"]["max"],
            start=first["utc"], end=last["utc"])
        for percentile in [50, 95, 99]:
            row[f"p{percentile}Ms"] = summary["iteration_duration"]["values"][f"p({percentile})"]
            row[f"httpP{percentile}Ms"] = summary["latency_ms"]["values"][f"p({percentile})"]
        runs.append(row)

groups = []
for key in sorted({(row["variant"], row["case"]) for row in runs}):
    members = [row for row in runs if (row["variant"], row["case"]) == key]
    assert len(members) == 3, (key, len(members))
    metrics = {}
    for name in members[0]:
        values = [row[name] for row in members]
        if all(isinstance(value, (int, float)) for value in values):
            metrics[name] = dict(median=statistics.median(values), min=min(values), max=max(values), total=sum(values))
    groups.append(dict(variant=key[0], case=key[1], metrics=metrics))
args.output.mkdir(parents=True, exist_ok=True)
(args.output / "runs.json").write_text(json.dumps(runs, indent=2))
(args.output / "groups.json").write_text(json.dumps(groups, indent=2))
for group in groups:
    metrics = group["metrics"]
    print(group["variant"], group["case"], {name: metrics[name] for name in
        ["cpuMsPerRequest", "allocationBytesPerRequest", "p95Ms", "requestsPerSecond"]})
