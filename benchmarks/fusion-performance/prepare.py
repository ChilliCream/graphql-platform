"""Build isolated, frozen applications before any measurement session."""
import argparse
import hashlib
import json
import os
import pathlib
import shutil
import subprocess

FIXTURE = pathlib.Path(__file__).resolve().parent
parser = argparse.ArgumentParser()
parser.add_argument("source", type=pathlib.Path)
parser.add_argument("destination", type=pathlib.Path)
parser.add_argument("--tools", required=True, type=pathlib.Path)
args = parser.parse_args()
source, destination = args.source.resolve(), args.destination.resolve()
destination.mkdir(parents=True, exist_ok=False)
environment = dict(os.environ, DOTNET_LOCAL_DEV="1", DOTNET_LOCAL_DEV_SINGLE_TARGET_FRAMEWORK="1")
commands = []


def command(argv, log, cwd=source):
    argv = list(map(str, argv))
    commands.append(dict(command=argv, cwd=str(cwd)))
    (destination / "commands.json").write_text(json.dumps(commands, indent=2))
    with (destination / log).open("w") as output:
        subprocess.run(argv, cwd=cwd, env=environment, stdout=output, stderr=subprocess.STDOUT, check=True)


sha = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=source, text=True).strip()
patch = subprocess.check_output(["git", "diff", "HEAD", "--", "src"], cwd=source)
(destination / "production.patch").write_bytes(patch)
(destination / "identity.json").write_text(json.dumps(dict(
    baseline=sha, productionPatchSha256=hashlib.sha256(patch).hexdigest(),
    source=str(source), sdkEnvironment={k: environment[k] for k in environment if k.startswith("DOTNET_")}), indent=2))
for name in ["gateway", "subgraphs", "trace-analyzer"]:
    shutil.copytree(FIXTURE / name, destination / "build" / name, ignore=shutil.ignore_patterns("bin", "obj"))
shutil.copy2(FIXTURE / "Directory.Build.props", destination / "Directory.Build.props")
for path in FIXTURE.glob("*.py"):
    if path.name not in ["prepare.py", "micro.py"]:
        shutil.copy2(path, destination / path.name)
shutil.copy2(FIXTURE / "load.js", destination / "load.js")
command(["dotnet", "--info"], "dotnet-info.txt")
command(["uname", "-a"], "uname.txt")
command(["lscpu"], "lscpu.txt")
command(["dotnet", "build", destination / "build/gateway/Gateway.csproj", "-c", "Release", "-m:4",
         "--disable-build-servers", f"-p:FusionSourceRoot={source}"], "build-gateway.log")
schemas = []
for role in ["Accounts", "Inventory", "Products", "Reviews"]:
    directory = destination / f"build/subgraphs/eShop.{role}"
    command(["dotnet", "build", directory / f"eShop.{role}.csproj", "-c", "Release", "-m:4", "--disable-build-servers"], f"build-{role}.log")
    command([directory / f"bin/Release/net10.0/eShop.{role}", "schema", "export", "--output", "schema.graphql"], f"export-{role}.log", directory)
    schemas.extend(["-f", directory / "schema.graphql"])
command([args.tools / "nitro", "fusion", "compose", *schemas, "-a", destination / "build/gateway/gateway.far", "--output", "json"], "composition.json")
command(["dotnet", "build", destination / "build/trace-analyzer/TraceAnalyzer.csproj", "-c", "Release", "-m:4", "--disable-build-servers"], "build-analyzer.log")
# Copying makes the measured files independent of MSBuild's source-output hard links.
for name in ["gateway", "subgraphs", "trace-analyzer"]:
    shutil.copytree(destination / "build" / name, destination / name, ignore=shutil.ignore_patterns("obj"))
assets = json.loads((destination / "build/gateway/obj/project.assets.json").read_text())
dependencies = {name: data for name, data in assets["libraries"].items() if name.startswith("HotChocolate")}
assert dependencies and all(data["type"] == "project" for data in dependencies.values()), dependencies
(destination / "gateway-source-dependencies.json").write_text(json.dumps(dependencies, indent=2))
hashes = {}
for path in (destination / "gateway/bin/Release/net10.0").glob("*.dll"):
    hashes[path.name] = hashlib.sha256(path.read_bytes()).hexdigest()
    original = destination / "build/gateway/bin/Release/net10.0" / path.name
    assert path.stat().st_ino != original.stat().st_ino
(destination / "gateway-binaries.json").write_text(json.dumps(hashes, indent=2))
print(destination, flush=True)
