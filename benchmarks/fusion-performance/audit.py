"""Audit source identities, fixed-heap counters, fixtures, accounting, and cleanup."""
import argparse
import datetime
import hashlib
import json
import pathlib
import socket
import subprocess

parser = argparse.ArgumentParser()
parser.add_argument("baseline", type=pathlib.Path)
parser.add_argument("candidate", type=pathlib.Path)
parser.add_argument("source", type=pathlib.Path)
parser.add_argument("--output", required=True, type=pathlib.Path)
args = parser.parse_args()
result = dict(binaryHashes={}, requests={}, schemas={}, counters=[], processes=[], ports={})
baseline_identity = json.loads((args.baseline / "identity.json").read_text())
candidate_identity = json.loads((args.candidate / "identity.json").read_text())
patch = subprocess.check_output(["git", "diff", candidate_identity["baseline"], "--", "src"], cwd=args.source)
result["candidatePatchSha256"] = hashlib.sha256(patch).hexdigest()
assert result["candidatePatchSha256"] == candidate_identity["productionPatchSha256"]
assert not (args.baseline / "production.patch").read_bytes()
for variant in [args.baseline, args.candidate]:
    hashes = json.loads((variant / "gateway-binaries.json").read_text())
    for name, expected in hashes.items():
        actual = hashlib.sha256((variant / "gateway/bin/Release/net10.0" / name).read_bytes()).hexdigest()
        assert actual == expected, (variant, name)
    result["binaryHashes"][variant.name] = hashes
    dependencies = json.loads((variant / "gateway-source-dependencies.json").read_text())
    assert all(item["type"] == "project" for item in dependencies.values())
    for path in variant.rglob("counter-decreases.json"):
        relative = str(path.relative_to(variant))
        if relative.startswith("matrix/") or relative.startswith("confirm-fixed-"):
            assert not json.loads(path.read_text()), path
    for path in (variant / "profile").rglob("counters.json"):
        events = json.loads(path.read_text())["Events"]
        rows = [json.loads(line) for line in (path.parent / "processes.jsonl").read_text().splitlines()]

        def epoch(value):
            return datetime.datetime.fromisoformat(value).timestamp()

        def interpolate(instant, getter):
            for first, last in zip(rows, rows[1:]):
                if first["time"] <= instant <= last["time"]:
                    fraction = (instant - first["time"]) / (last["time"] - first["time"])
                    return getter(first) + (getter(last) - getter(first)) * fraction
            raise ValueError("Counter boundary is outside the process samples")

        start = min(epoch(e["timestamp"]) for e in events if e["name"].startswith("dotnet.process.cpu.count"))
        cpu_events = [e for e in events if e["name"].startswith("dotnet.process.cpu.time")]
        end = max(epoch(e["timestamp"]) for e in cpu_events)
        pid = json.loads((path.parent / "boundaries.json").read_text())["before"]["processes"]["Gateway"]["pid"]
        getter = lambda row: next(p["cpu"] for p in row["processes"] if p["pid"] == pid)
        process_cpu = interpolate(end, getter) - interpolate(start, getter)
        endpoint_cpu = (interpolate(end, lambda row: row["gateway"]["cpuMs"]) - interpolate(start, lambda row: row["gateway"]["cpuMs"])) / 1000
        allocation = interpolate(end, lambda row: row["gateway"]["allocated"]) - interpolate(start, lambda row: row["gateway"]["allocated"])
        allocation_events = [e for e in events if e["name"].startswith("dotnet.gc.heap.total_allocated")]
        assert all(e["value"] >= 0 for e in allocation_events)
        result["counters"].append(dict(variant=variant.name, start=start, end=end,
            runtimeCpuIncrementSum=sum(e["value"] for e in cpu_events), procCpuSeconds=process_cpu,
            endpointCpuSeconds=endpoint_cpu, runtimeAllocationIncrementSum=sum(e["value"] for e in allocation_events),
            endpointAllocationDelta=allocation))
    for path in variant.glob("*-pids.json"):
        for name, pid in json.loads(path.read_text()).items():
            command_file = pathlib.Path(f"/proc/{pid}/cmdline")
            command = command_file.read_bytes().replace(b"\0", b" ").decode() if command_file.exists() else ""
            owned_alive = str(variant.resolve()) in command
            assert not owned_alive, (name, pid, command)
            result["processes"].append(dict(session=path.name, name=name, pid=pid, ownedAlive=owned_alive))
for name in sorted(path.stem for path in (args.baseline / "requests").glob("*.json")):
    first, last = [(variant / "requests" / f"{name}.json").read_bytes() for variant in [args.baseline, args.candidate]]
    assert first == last, name
    result["requests"][name] = hashlib.sha256(first).hexdigest()
for role in ["Accounts", "Inventory", "Products", "Reviews"]:
    first, last = [(variant / f"build/subgraphs/eShop.{role}/schema.graphql").read_bytes() for variant in [args.baseline, args.candidate]]
    assert first == last, role
    result["schemas"][role] = hashlib.sha256(first).hexdigest()
for port in range(5320, 5325):
    with socket.socket() as connection:
        connection.settimeout(.1)
        result["ports"][port] = "closed" if connection.connect_ex(("127.0.0.1", port)) else "open"
        assert result["ports"][port] == "closed", port
args.output.write_text(json.dumps(result, indent=2))
print("Audit passed:", args.output)
