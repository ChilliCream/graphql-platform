"""Pack the example's dependencies and exercise each CLI package combination."""

import json
import os
import re
import subprocess
import tempfile
import time
import urllib.request
import uuid
import xml.etree.ElementTree as ET
from pathlib import Path

EXAMPLE = Path(__file__).resolve().parent
PROJECT = EXAMPLE / "OptionalHostCommands.csproj"
GRAPHQL = "HotChocolate.AspNetCore.CommandLine"
MOCHA = "Mocha.CommandLine"


def dotnet(*args, cwd=EXAMPLE, timeout=300):
    subprocess.run(["dotnet", *args], cwd=cwd, check=True, timeout=timeout)


def pack(feed, version):
    dotnet("restore", PROJECT)
    spec = json.loads((EXAMPLE / "obj" / (PROJECT.name + ".nuget.dgspec.json")).read_text())
    solution = ET.Element("Solution")
    for project in sorted(spec["projects"]):
        if Path(project) != PROJECT:
            ET.SubElement(solution, "Project", Path=project)
    path = feed.parent / "CommandPackages.slnx"
    ET.ElementTree(solution).write(path)
    dotnet("pack", path, "-c", "Release", "-o", feed, f"-p:Version={version}", "-v:q")


def invoke(app, *args, expected=0):
    run = subprocess.run(["dotnet", app, *args], cwd=app.parent, capture_output=True,
                         text=True, timeout=20)
    if run.returncode != expected:
        raise RuntimeError(f"{args} exited {run.returncode}\n{run.stdout}\n{run.stderr}")
    return run.stdout.strip()


def check_server(app):
    log = app.parent / "server.log"
    with log.open("w") as output, subprocess.Popen(
        ["dotnet", app], cwd=app.parent, stdout=output, stderr=subprocess.STDOUT,
        env={**os.environ, "ASPNETCORE_URLS": "http://127.0.0.1:0"},
    ) as server:
        try:
            deadline = time.monotonic() + 30
            while time.monotonic() < deadline and server.poll() is None:
                address = re.search(r"Now listening on: (http://127\.0\.0\.1:\d+)", log.read_text())
                if address:
                    request = urllib.request.Request(address[1] + "/reviews",
                        data=b'{"query":"{ rating }"}', headers={"Content-Type": "application/json"})
                    with urllib.request.urlopen(request, timeout=10) as response:
                        assert json.load(response) == {"data": {"rating": 5}}
                    break
                time.sleep(0.1)
            else:
                raise RuntimeError(log.read_text())
        finally:
            server.terminate()
            server.wait(timeout=15)


def verify(directory, version, packages):
    directory.mkdir()
    project = ET.Element("Project", Sdk="Microsoft.NET.Sdk.Web")
    props = ET.SubElement(project, "PropertyGroup")
    for key, value in {"TargetFramework": "net10.0", "ImplicitUsings": "enable",
                       "NuGetAudit": "false", "RestorePackagesPath": "../packages"}.items():
        ET.SubElement(props, key).text = value
    refs = ET.SubElement(project, "ItemGroup")
    for name in ["HotChocolate.AspNetCore", "Mocha.Transport.InMemory", *packages]:
        ET.SubElement(refs, "PackageReference", Include=name, Version=version)
    ET.ElementTree(project).write(directory / "Example.csproj")

    program = (EXAMPLE / "Program.cs").read_text()
    if not packages:
        program = program.replace("return await app.RunWithCommandsAsync(args);", "await app.RunAsync();")
    (directory / "Program.cs").write_text(program)
    dotnet("build", "-v:q", cwd=directory)
    app = directory / "bin/Debug/net10.0/Example.dll"

    if not packages:
        libraries = json.loads(app.with_suffix(".deps.json").read_text())["libraries"]
        names = {library.split("/")[0] for library in libraries}
        assert names.isdisjoint({"ChilliCream.Hosting.CommandLine", "System.CommandLine", GRAPHQL, MOCHA})
        check_server(app)
    else:
        help_text = invoke(app, "--help")
        assert len(re.findall(r"^  schema\s", help_text, re.M)) == int(GRAPHQL in packages)
        assert len(re.findall(r"^  mocha\s", help_text, re.M)) == int(MOCHA in packages)
        if GRAPHQL in packages:
            assert sorted(invoke(app, "schema", "list").splitlines()) == ["catalog", "reviews"]
            for name, field in [("catalog", "product"), ("reviews", "rating")]:
                path = directory / (name + ".graphqls")
                invoke(app, "schema", "export", "--schema-name", name, "--output", path)
                assert re.search(r"^  " + field + r":", path.read_text(), re.M)
        else:
            invoke(app, "schema", "list", expected=1)
        if MOCHA in packages:
            assert invoke(app, "mocha", "transports") == "orders"
        else:
            invoke(app, "mocha", "transports", expected=1)
        invoke(app, "schmea", "export", expected=1)
        if len(packages) == 2:
            check_server(app)
    print(f"PASS {directory.name}", flush=True)


if __name__ == "__main__":
    os.environ["DOTNET_LOCAL_DEV_SINGLE_TARGET_FRAMEWORK"] = "1"
    with tempfile.TemporaryDirectory(prefix="optional-command-packages-") as temp:
        scratch = Path(temp)
        feed = scratch / "feed"
        version = "0.0.0-prototype." + uuid.uuid4().hex
        pack(feed, version)
        cache = subprocess.check_output(
            ["dotnet", "nuget", "locals", "global-packages", "--list", "--force-english-output"],
            text=True).strip().split(": ", 1)[1]
        config = ET.Element("configuration")
        sources = ET.SubElement(config, "packageSources")
        ET.SubElement(sources, "clear")
        ET.SubElement(sources, "add", key="prototype", value=str(feed))
        ET.SubElement(sources, "add", key="restored-dependencies", value=cache)
        ET.ElementTree(config).write(scratch / "nuget.config")
        for label, packages in [("core-only", []), ("graphql-only", [GRAPHQL]),
                                ("mocha-only", [MOCHA]), ("both", [GRAPHQL, MOCHA])]:
            verify(scratch / label, version, packages)
