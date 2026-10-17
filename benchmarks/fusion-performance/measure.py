"""Run one variant at a time. Builds and offline analysis are separate commands."""
import argparse
import json
import os
import pathlib
import subprocess
import sys
import time

from lifecycle import ROOT, request, start, stop
from workloads import CASES

parser = argparse.ArgumentParser()
parser.add_argument("mode", choices=["pilot", "matrix", "profile", "memory", "normal-memory", "confirm"])
parser.add_argument("--gc", choices=["default", "fixed"], default="default")
parser.add_argument("--label")
parser.add_argument("--repetitions", type=int, default=3)
args = parser.parse_args()
phase = args.label or args.mode
if args.gc == "fixed":
    os.environ["FUSION_PERF_REQUIRE_MONOTONIC"] = "1"
environment = {"DOTNET_GCDynamicAdaptationMode": "0"} if args.gc == "fixed" else {}
(ROOT / f"{phase}-configuration.json").write_text(json.dumps(dict(gatewayEnvironment=environment, arguments=sys.argv), indent=2))
pids = start(environment)
(ROOT / f"{phase}-pids.json").write_text(json.dumps(pids, indent=2))
try:
    import campaign

    if args.mode in ["pilot", "matrix", "confirm"]:
        subprocess.run(["python3", ROOT / "validate.py"], check=True)
    selected = [c for c in CASES if c["kind"] == "lookup"] if args.mode in ["pilot", "matrix"] else [c for c in CASES if c["name"] == ("list-1000-lookup" if args.mode == "profile" else "list-10-lookup")]
    for case in selected:
        directory = ROOT / phase / case["name"]
        if args.mode == "pilot":
            campaign.run(case, directory, 15, rate=case["rate"])
        elif args.mode in ["matrix", "confirm"]:
            for repeat in [1, 2]:
                campaign.run(case, directory / f"warmup-{repeat}", 30, rate=case["rate"])
            for repeat in range(1, args.repetitions + 1):
                campaign.run(case, directory / f"rep-{repeat}", 60, rate=case["rate"])
        elif args.mode == "profile":
            campaign.run(case, directory / "warmup", 60, rate=case["rate"])
            campaign.run(case, directory, 95, rate=case["rate"], diagnostic=True)
        elif args.mode in ["memory", "normal-memory"]:
            tools = pathlib.Path(os.environ["FUSION_PERF_TOOLS"])
            pid = json.loads((ROOT / "pids.json").read_text())["Gateway"]

            def heap(label):
                folder = ROOT / "heaps" / phase
                folder.mkdir(parents=True, exist_ok=True)
                before = campaign.snapshot()
                command = [str(tools / "dotnet-gcdump"), "collect", "-p", str(pid), "-o", str(folder / f"{label}.gcdump")]
                with (folder / f"{label}.log").open("w") as output:
                    subprocess.run(command, stdout=output, stderr=subprocess.STDOUT, check=True)
                (folder / f"{label}-state.json").write_text(json.dumps(dict(before=before, after=campaign.snapshot(), command=command), indent=2))

            campaign.run(case, directory / "small-before", 60, rate=case["rate"])
            heap("normal")
            if args.mode == "memory":
                large = next(c for c in CASES if c["size"] == 20000)
                campaign.run(large, directory / "burst", 60, vus=4)
                heap("post-burst")
                campaign.run(case, directory / "small-after", 60, rate=case["rate"])
                for interval in [30, 60, 90]:
                    time.sleep(30)
                    (ROOT / "heaps" / phase / f"idle-{interval}-state.json").write_text(json.dumps(campaign.snapshot(), indent=2))
                    print("idle", interval, flush=True)
                heap("idle")
finally:
    stop()
    (ROOT / f"{phase}-cleanup.json").write_text((ROOT / "cleanup.json").read_text())
