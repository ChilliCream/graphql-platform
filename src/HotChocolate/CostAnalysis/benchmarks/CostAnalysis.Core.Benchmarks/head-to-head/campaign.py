#!/usr/bin/env python3
"""Run randomized fresh-process cost benchmark replicates on one host."""

from __future__ import annotations

import argparse
import csv
import datetime as dt
import json
import platform
import random
import statistics
import subprocess
from pathlib import Path


CSV_COLUMNS = (
    "backend",
    "object_types",
    "abstract_types",
    "incidences_per_object",
    "query_spreads",
    "type_cost",
    "field_cost",
    "iterations",
    "median_total_ns",
    "median_ns_per_op",
    "sample_0_total_ns",
    "sample_1_total_ns",
    "sample_2_total_ns",
    "sample_3_total_ns",
    "sample_4_total_ns",
    "checksum",
)
ENGINES = ("rust-exact-case", "hotchocolate-cold", "hotchocolate-warm")
SEED = 20260829


def command_output(*command: str) -> str:
    return subprocess.run(
        command,
        check=True,
        text=True,
        capture_output=True,
    ).stdout.strip()


def parse_row(output: str) -> dict[str, str]:
    rows = list(csv.DictReader(output.splitlines()))
    if len(rows) != 1 or tuple(rows[0]) != CSV_COLUMNS:
        raise RuntimeError(f"unexpected benchmark output:\n{output}")
    return rows[0]


def run_rust(binary: Path, scenario: dict[str, object]) -> dict[str, str]:
    if scenario["axis"] == "pathological-booleans":
        command = (
            str(binary),
            "cost-pathological-point",
            str(scenario["booleanVariablesPerRegion"]),
            "exact-case",
        )
    else:
        command = (
            str(binary),
            "cost-point",
            str(scenario["objectTypes"]),
            str(scenario["querySpreads"]),
            "exact-case",
        )
    row = parse_row(command_output(*command))
    row["backend"] = "rust-exact-case"
    return row


def run_hotchocolate(
    dotnet: str,
    assembly: Path,
    corpus: Path,
    scenario: dict[str, object],
    phase: str,
) -> dict[str, str]:
    return parse_row(
        command_output(
            dotnet,
            str(assembly),
            "head-to-head-point",
            str(corpus),
            str(scenario["id"]),
            phase,
        )
    )


def assert_expected(row: dict[str, str], scenario: dict[str, object]) -> None:
    actual = (float(row["type_cost"]), float(row["field_cost"]))
    expected = (
        float(scenario["expectedTypeCost"]),
        float(scenario["expectedFieldCost"]),
    )
    if actual != expected:
        raise RuntimeError(
            f"{row['backend']} produced {actual} for {scenario['id']}, expected {expected}"
        )
    if int(row["median_total_ns"]) < 100_000_000:
        raise RuntimeError(
            f"{row['backend']} calibrated below 100ms for {scenario['id']}: "
            f"{row['median_total_ns']}ns"
        )


def write_csv(path: Path, rows: list[dict[str, str]]) -> None:
    with path.open("w", newline="") as destination:
        writer = csv.DictWriter(destination, fieldnames=CSV_COLUMNS)
        writer.writeheader()
        writer.writerows(rows)


def print_summary(rows: list[tuple[dict[str, object], dict[str, str]]]) -> None:
    grouped: dict[tuple[str, str], list[int]] = {}
    for scenario, row in rows:
        key = (str(scenario["id"]), row["backend"])
        grouped.setdefault(key, []).append(int(row["median_ns_per_op"]))

    for (scenario_id, backend), values in sorted(grouped.items()):
        print(f"{scenario_id},{backend},{statistics.median(values):.0f}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--corpus", type=Path, required=True)
    parser.add_argument("--rust-binary", type=Path, required=True)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--assembly", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    parser.add_argument("--selection", choices=("endpoints", "all"), required=True)
    parser.add_argument("--replicates", type=int, default=10)
    parser.add_argument("--git-root", type=Path, required=True)
    arguments = parser.parse_args()
    if arguments.replicates <= 0:
        parser.error("--replicates must be positive")

    corpus = json.loads(arguments.corpus.read_text())
    if corpus["sourceCommit"] != "fec57fd7a980b5399637fa464a9bdce0781d91dd":
        raise RuntimeError("benchmark corpus came from an unexpected oracle revision")
    if corpus["defaultListSize"] != 1:
        raise RuntimeError("benchmark corpus must use default_list_size(1)")

    scenarios = corpus["scenarios"]
    if arguments.selection == "endpoints":
        scenarios = [item for item in scenarios if item["axis"] == "cost-endpoints"]

    scenario_directory = arguments.corpus.parent / "scenarios"
    scenario_directory.mkdir()
    scenario_corpora: dict[str, Path] = {}
    for scenario in scenarios:
        scenario_path = scenario_directory / f"{scenario['id']}.json"
        scenario_path.write_text(
            json.dumps(
                {
                    "sourceCommit": corpus["sourceCommit"],
                    "defaultListSize": corpus["defaultListSize"],
                    "scenarios": [scenario],
                }
            )
            + "\n"
        )
        scenario_corpora[str(scenario["id"])] = scenario_path

    plan = [
        (replicate, scenario, engine)
        for replicate in range(arguments.replicates)
        for scenario in scenarios
        for engine in ENGINES
    ]
    random.Random(SEED).shuffle(plan)
    rows_by_axis: dict[str, list[dict[str, str]]] = {}
    observed: list[tuple[dict[str, object], dict[str, str]]] = []

    for run_order, (_, scenario, engine) in enumerate(plan, start=1):
        print(f"[{run_order}/{len(plan)}] {scenario['id']} {engine}", flush=True)
        if engine == "rust-exact-case":
            row = run_rust(arguments.rust_binary, scenario)
        else:
            phase = "Cold" if engine == "hotchocolate-cold" else "Warm"
            row = run_hotchocolate(
                arguments.dotnet,
                arguments.assembly,
                scenario_corpora[str(scenario["id"])],
                scenario,
                phase,
            )
        assert_expected(row, scenario)
        rows_by_axis.setdefault(str(scenario["axis"]), []).append(row)
        observed.append((scenario, row))

    arguments.output_dir.mkdir(parents=True, exist_ok=True)
    for axis in (
        "cost-endpoints",
        "cost-schema-size",
        "cost-query-size",
        "pathological-booleans",
    ):
        (arguments.output_dir / f"{axis}.csv").unlink(missing_ok=True)
    for axis, rows in rows_by_axis.items():
        write_csv(arguments.output_dir / f"{axis}.csv", rows)

    provenance = {
        "createdAtUtc": dt.datetime.now(dt.UTC).isoformat(),
        "gitRev": command_output("git", "-C", str(arguments.git_root), "rev-parse", "HEAD"),
        "oracleGitRev": command_output(
            "git", "-C", str(arguments.rust_binary.parents[3]), "rev-parse", "HEAD"
        ),
        "rustc": command_output("rustc", "-Vv"),
        "cargo": command_output("cargo", "-V"),
        "dotnet": command_output(arguments.dotnet, "--version"),
        "platform": platform.platform(),
        "machine": platform.machine(),
        "seed": SEED,
        "replicates": arguments.replicates,
        "selection": arguments.selection,
        "generatedCorpusBytes": arguments.corpus.stat().st_size,
        "methodology": (
            "two warmups, >=100ms doubling calibration, >=200ms confirmation, "
            "five samples replaced after doubling when median is <100ms, median sample total"
        ),
    }
    (arguments.output_dir / "provenance.json").write_text(
        json.dumps(provenance, indent=2) + "\n"
    )
    print("scenario,backend,median_ns_per_op")
    print_summary(observed)


if __name__ == "__main__":
    main()
