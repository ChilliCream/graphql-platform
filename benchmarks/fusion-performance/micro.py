"""Run the same BDN fixture against a gateway's audited, source-built binaries."""
import argparse
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import threading
import time

parser = argparse.ArgumentParser()
parser.add_argument("variant", type=pathlib.Path)
parser.add_argument("--prepare-only", action="store_true")
parser.add_argument("--run-only", action="store_true")
args = parser.parse_args()
variant = args.variant.resolve()
destination = variant / "micro"
fixture = pathlib.Path(__file__).resolve().parent
if not args.run_only:
    shutil.copytree(fixture / "micro", destination)
    shutil.copy2(fixture / "Directory.Build.props", destination / "Directory.Build.props")
project = destination / "HotChocolate.Fusion.Execution.Benchmarks.csproj"
gateway = variant / "gateway/bin/Release/net10.0"
if not args.run_only:
    project.write_text(project.read_text().replace("<OutputType>", f"<MeasuredGateway>{gateway}</MeasuredGateway><OutputType>"))
assembly_hash = hashlib.sha256((gateway / "HotChocolate.Buffers.dll").read_bytes()).hexdigest()
environment = dict(os.environ, DOTNET_GCDynamicAdaptationMode="0", FUSION_PERF_ASSEMBLY_SHA256=assembly_hash)
commands = [
    ["dotnet", "build", str(project), "-c", "Release", "-m:4", "--disable-build-servers"],
    ["taskset", "-c", "2,3,10,11", "dotnet", str(destination / "bin/Release/net10.0/HotChocolate.Fusion.Execution.Benchmarks.dll"),
     "--filter", "*SegmentTables*", "--artifacts", str(destination / "results"), "--exporters", "json"],
]
(destination / "commands.json").write_text(json.dumps(dict(commands=commands, assemblySha256=assembly_hash,
    gatewayEnvironment={"DOTNET_GCDynamicAdaptationMode": "0"}), indent=2))
for command, label in zip(commands, ["build", "run"]):
    if (args.prepare_only and label != "build") or (args.run_only and label != "run"):
        continue
    print("micro", label, variant.name, flush=True)
    with (destination / f"{label}.log").open("w") as output:
        if label == "build":
            subprocess.run(command, cwd=destination, env=environment, stdout=output, stderr=subprocess.STDOUT, check=True)
            continue
        stopped = threading.Event()

        def monitor():
            with (destination / "processes.jsonl").open("w", buffering=1) as stream:
                while not stopped.is_set():
                    processes = []
                    for directory in pathlib.Path("/proc").iterdir():
                        if not directory.name.isdigit():
                            continue
                        try:
                            fields = (directory / "stat").read_text().rsplit(")", 1)[1].split()
                            processes.append(dict(pid=int(directory.name), cpu=(int(fields[11]) + int(fields[12])) / os.sysconf("SC_CLK_TCK"),
                                rss=int(fields[21]) * os.sysconf("SC_PAGE_SIZE"), command=(directory / "cmdline").read_bytes().replace(b"\0", b" ").decode()))
                        except (FileNotFoundError, ProcessLookupError, PermissionError):
                            pass
                    stream.write(json.dumps(dict(time=time.time(), processes=processes)) + "\n")
                    stopped.wait(1)

        observer = threading.Thread(target=monitor, daemon=True)
        observer.start()
        process = subprocess.Popen(command, cwd=destination, env=environment, stdout=output, stderr=subprocess.STDOUT)
        (destination / "pid.json").write_text(json.dumps(dict(pid=process.pid, command=command)))
        try:
            _, status, usage = os.wait4(process.pid, 0)
            process.returncode = os.waitstatus_to_exitcode(status)
            (destination / "usage.json").write_text(json.dumps(dict(userCpuSeconds=usage.ru_utime, systemCpuSeconds=usage.ru_stime,
                maxRssKiB=usage.ru_maxrss, exitCode=process.returncode), indent=2))
            if process.returncode:
                raise RuntimeError(f"Microbenchmark failed: {destination}")
        finally:
            if process.poll() is None:
                process.terminate()
                process.wait()
            stopped.set()
            observer.join()
        reports = list((destination / "results/results").glob("*-full-compressed.json"))
        benchmarks = [case for report in reports for case in json.loads(report.read_text())["Benchmarks"]]
        assert len(benchmarks) == 2 and all(case["Statistics"] is not None for case in benchmarks), "BDN must report both completed cases"
        for case in benchmarks:
            actual = [m for m in case["Measurements"] if m["IterationMode"] == "Workload" and m["IterationStage"] == "Actual"]
            assert len(actual) == 45 and len({m["LaunchIndex"] for m in actual}) == 3, "BDN must complete three launches of fifteen measured iterations"
