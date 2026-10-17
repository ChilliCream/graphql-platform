#!/usr/bin/env python3
"""Generate RESULTS_CONSTANT.md and RESULTS_BURST.md from benchmark artifacts."""

import csv
import importlib.util
import json
import os
import statistics
import sys
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path


def find_results(artifacts_dir):
    """Find all k6_summary.json files and their associated metadata."""
    results = []
    for summary_path in Path(artifacts_dir).rglob("k6_summary.json"):
        result_dir = summary_path.parent
        metadata_path = result_dir / "metadata.json"
        if not metadata_path.exists():
            print(f"  Skipping {summary_path} (no metadata.json)")
            continue

        with open(metadata_path) as f:
            metadata = json.load(f)
        with open(summary_path) as f:
            summary = json.load(f)

        txt_path = result_dir / "k6_summary.txt"
        k6_txt = txt_path.read_text() if txt_path.exists() else ""

        memory_path = result_dir / "memory.json"
        memory = {}
        if memory_path.exists():
            with open(memory_path) as f:
                memory = json.load(f)

        gateway_core_pct = []
        data_path = result_dir / "data.csv"
        if data_path.exists():
            with open(data_path, newline="") as f:
                for row in csv.DictReader(f):
                    try:
                        gateway_core_pct.append(float(row["Gateway_Core_CPU"]))
                    except (KeyError, TypeError, ValueError):
                        continue

        results.append({
            "metadata": metadata,
            "summary": summary,
            "k6_txt": k6_txt,
            "memory": memory,
            "gateway_core_pct": gateway_core_pct,
        })
    return results


def extract_rps(summary):
    """Extract RPS from k6 summary JSON."""
    return int(summary.get("metrics", {}).get("http_reqs", {}).get("values", {}).get("rate", 0))


def extract_metrics(summary):
    """Extract key metrics from k6 summary JSON."""
    metrics = summary.get("metrics", {})

    http_reqs = metrics.get("http_reqs", {}).get("values", {})
    http_duration = metrics.get("http_req_duration", {}).get("values", {})
    http_failed = metrics.get("http_req_failed", {}).get("values", {})
    success_rate = metrics.get("success_rate", {}).get("values", {})

    rps = http_reqs.get("rate", 0)
    total = int(http_reqs.get("count", 0))
    failed = int(http_failed.get("passes", 0))  # passes = matched "failed" condition
    avg_ms = http_duration.get("avg", 0)
    p95_ms = http_duration.get("p(95)", 0)

    # Determine notes from success_rate and check failures
    sr_fails = int(success_rate.get("fails", 0))
    notes = ""
    if sr_fails > 0:
        notes = f"non-compatible response structure ({sr_fails})"
    elif failed > 0:
        notes = f"{failed} failed requests"

    return {
        "rps": int(rps),
        "total": total,
        "failed": failed,
        "sr_fails": sr_fails,
        "sr_passes": int(success_rate.get("passes", 0)),
        "has_error": (sr_fails > 0 or failed > 0),
        "avg_ms": avg_ms,
        "p95_ms": p95_ms,
        "notes": notes,
    }


def normalize_subgraph_tech(value):
    """Normalize subgraph technology labels for stable grouping keys."""
    tech = str(value or "").strip().lower()
    if tech in {"rust", ".net", "net"}:
        return ".net" if tech in {"net", ".net"} else "rust"
    return tech or "unknown"


def tech_display_label(subgraph_tech):
    """User-facing label for a subgraph technology key.

    Functional keys (".net", "rust") are unchanged; only the rendered label
    leads with HotChocolate instead of .NET.
    """
    if subgraph_tech == ".net":
        return "HotChocolate"
    return subgraph_tech


def get_gateway_name(metadata):
    """Get gateway identifier."""
    return metadata.get("gateway", metadata.get("display_name", "unknown"))


def get_subgraph_tech(metadata):
    """Get subgraph technology label."""
    tech = normalize_subgraph_tech(metadata.get("subgraph_tech", ""))
    if tech in {"rust", ".net"}:
        return tech

    variant = str(metadata.get("subgraphs", "")).strip().lower()
    if variant == "subgraphs-rust":
        return "rust"
    if variant == "subgraphs-net":
        return ".net"

    display_name = str(metadata.get("display_name", "")).lower()
    if "(hotchocolate subgraphs)" in display_name:
        return ".net"
    if "(.net subgraphs)" in display_name or "(net subgraphs)" in display_name:
        return ".net"
    if "(rust subgraphs)" in display_name:
        return "rust"

    return "unknown"


def get_version(metadata):
    """Get version string."""
    return metadata.get("version", "")


def collect_hardware_profiles(results):
    """Collect unique benchmark hardware profiles from metadata."""
    profiles = defaultdict(int)
    for r in results:
        metadata = r.get("metadata", {})
        profile = (
            str(metadata.get("machine_hostname", "")).strip(),
            str(metadata.get("machine_os", "")).strip(),
            str(metadata.get("machine_cpu", "")).strip(),
            str(metadata.get("machine_cores", "")).strip(),
            str(metadata.get("machine_ram", "")).strip(),
        )
        if any(profile):
            profiles[profile] += 1
    return sorted(profiles.items(), key=lambda item: item[1], reverse=True)


def parse_expected_gateways_env(var_name):
    """Parse expected gateway list from environment variable JSON."""
    raw = os.environ.get(var_name, "").strip()
    if not raw:
        return []
    try:
        parsed = json.loads(raw)
    except json.JSONDecodeError as exc:
        print(f"Warning: failed to parse {var_name}: {exc}")
        return []
    if not isinstance(parsed, list):
        print(f"Warning: {var_name} is not a list, ignoring.")
        return []
    return parsed


def to_expected_gateway_entry(item):
    """Convert matrix entry into normalized expected gateway descriptor."""
    name = str(item.get("name", "")).strip()
    base_name = str(item.get("base_name", "")).strip()
    gateway = base_name or name

    subgraph_tech = normalize_subgraph_tech(item.get("subgraph_tech", ""))

    display_name = str(item.get("display_name", "")).strip()
    if not display_name and gateway:
        display_name = f"{gateway} ({tech_display_label(subgraph_tech)} subgraphs)"

    if not gateway:
        return None

    return {
        "gateway": gateway,
        "subgraph_tech": subgraph_tech,
        "display_name": display_name or gateway,
    }


def dedupe_expected_gateways(entries):
    """Drop duplicate expected gateways by (gateway, subgraph_tech)."""
    deduped = []
    seen = set()
    for entry in entries:
        if not entry:
            continue
        key = (entry["gateway"], entry["subgraph_tech"])
        if key in seen:
            continue
        seen.add(key)
        deduped.append(entry)
    return deduped


def load_expected_gateways_by_mode():
    """Load expected gateways for each benchmark mode from env.

    All modes run against the HotChocolate subgraphs; a single expected list (from
    EXPECTED_GATEWAYS_JSON) applies to every mode.
    """
    expected_raw = parse_expected_gateways_env("EXPECTED_GATEWAYS_JSON")

    expected = dedupe_expected_gateways(
        [to_expected_gateway_entry(item) for item in expected_raw]
    )

    return {
        "constant": expected,
        "burst": expected,
        "ramping": expected,
        "constant-latency": expected,
    }


def load_not_run_gateways_by_mode():
    """Load gateways deliberately not run for each mode (from env).

    These are reported as "not run" placeholders rather than "benchmark run
    failed" — the gateway was intentionally excluded from the mode, not a crash.
    """
    not_run_raw = parse_expected_gateways_env("NOT_RUN_GATEWAYS_JSON")
    not_run = dedupe_expected_gateways(
        [to_expected_gateway_entry(item) for item in not_run_raw]
    )
    keys = {(e["gateway"], e["subgraph_tech"]) for e in not_run}
    # NOT_RUN_GATEWAYS_JSON is populated per-invocation for the mode being generated
    # (the workflow runs one generate step per mode and pairs each with that mode's
    # not-run set), so the same keys apply to whichever mode is produced.
    return {
        "constant": keys,
        "burst": keys,
        "ramping": keys,
        "constant-latency": keys,
    }


def merge_failed_expected_entries(entries, expected_entries, not_run_keys=frozenset()):
    """Append placeholders for gateways with no successful result artifact.

    A missing gateway whose key is in ``not_run_keys`` is labelled "not run"
    (deliberately excluded from this mode); any other missing gateway is labelled
    "benchmark run failed".
    """
    existing_keys = {(e["gateway"], e["subgraph_tech"]) for e in entries}
    merged = list(entries)

    missing = [
        expected for expected in expected_entries
        if (expected["gateway"], expected["subgraph_tech"]) not in existing_keys
    ]
    missing.sort(key=lambda e: e["gateway"])

    for expected in missing:
        is_not_run = (expected["gateway"], expected["subgraph_tech"]) in not_run_keys
        merged.append({
            "gateway": expected["gateway"],
            "subgraph_tech": expected["subgraph_tech"],
            "version": "",
            "median_rps": None,
            "best_rps": None,
            "worst_rps": None,
            "cv_pct": None,
            "notes": "not run" if is_not_run else "benchmark run failed",
            "metrics": {},
            "k6_txt": "",
            "summary": {},
            "display_name": expected.get("display_name", expected["gateway"]),
            "failed_run": True,
            "not_run": is_not_run,
        })
    return merged


def has_any_expected_gateways(expected_by_mode):
    """Check whether any mode has expected gateways configured."""
    return any(expected_by_mode.get(mode) for mode in expected_by_mode)


def sort_entries(entries):
    """Sort successful entries by performance and failed placeholders last."""
    return sorted(
        entries,
        key=lambda e: (
            1 if e.get("failed_run") or e.get("median_rps") is None else 0,
            -(e["median_rps"] or 0),
            e["gateway"],
        ),
    )


def format_metric(value):
    """Format numeric table cells or emit em dash for unavailable values."""
    if value is None:
        return "—"
    return f"{int(value):,}"


def format_cv(value):
    """Format CV% table cells or emit em dash for unavailable values."""
    if value is None:
        return "—"
    return f"{float(value):.1f}%"


def rps_stats(runs):
    """Median/best/worst/CV% of runs' RPS; ties broken by execution order."""
    if not runs:
        return None
    ordered = sorted(runs, key=lambda x: (x["rps"], x["run"]))
    rps = [r["rps"] for r in ordered]
    median_run = ordered[len(ordered) // 2]
    median_rps = median_run["rps"]
    if len(rps) > 1 and median_rps > 0:
        cv_pct = round(statistics.stdev(rps) / median_rps * 100, 1)
    else:
        cv_pct = 0.0
    return {
        "median_run": median_run,
        "median_rps": median_rps,
        "best_rps": max(rps),
        "worst_rps": min(rps),
        "cv_pct": cv_pct,
    }


def select_median_runs(results):
    """Group runs by gateway, compute aggregate stats (median, best, worst, CV%)."""
    groups = defaultdict(list)
    for r in results:
        metadata = r["metadata"]
        name = get_gateway_name(metadata)
        subgraph_tech = get_subgraph_tech(metadata)
        version = get_version(r["metadata"])
        rps = extract_rps(r["summary"])
        metrics = extract_metrics(r["summary"])
        groups[(name, subgraph_tech)].append({
            "result": r,
            "run": metadata.get("run", 0),
            "rps": rps,
            "version": version,
            "metrics": metrics,
            "display_name": metadata.get("display_name", f"{name} ({tech_display_label(subgraph_tech)} subgraphs)"),
        })

    selected = []
    for (gateway, subgraph_tech), runs in groups.items():
        total_run_count = len(runs)
        expected_run_count = max(
            int(r["result"]["metadata"].get("total_runs") or total_run_count)
            for r in runs
        )
        execution_failed = total_run_count < expected_run_count
        successful_runs = [r for r in runs if not r["metrics"]["has_error"]]
        errored_runs = [r for r in runs if r["metrics"]["has_error"]]
        successful_run_count = len(successful_runs)

        stats = rps_stats(successful_runs)
        if successful_runs:
            successful_runs.sort(key=lambda x: (x["rps"], x["run"]))
            median_run = stats["median_run"]
            median_rps, best_rps = stats["median_rps"], stats["best_rps"]
            worst_rps, cv_pct = stats["worst_rps"], stats["cv_pct"]

        version = runs[0]["version"]

        # Aggregate errors across ALL runs.
        total_failed = sum(r["metrics"]["failed"] for r in runs)
        total_sr_fails = sum(r["metrics"]["sr_fails"] for r in runs)
        runs_with_errors = len(errored_runs)

        notes = ""
        if total_sr_fails > 0:
            notes = f"non-compatible response ({total_sr_fails} across {runs_with_errors}/{total_run_count} runs)"
        elif total_failed > 0:
            notes = f"{total_failed} failed requests across {runs_with_errors}/{total_run_count} runs"

        # Not http_reqs: it also counts the setup() warm-up requests.
        successful = sum(r["metrics"]["sr_passes"] for r in runs)
        failed = sum(r["metrics"]["sr_fails"] for r in runs)
        json_data = {
            "stats": rps_stats(runs),
            "runs_rps": [r["rps"] for r in sorted(runs, key=lambda x: x["run"])],
            "requests": {"total": successful + failed, "successful": successful, "failed": failed},
        }

        if successful_runs:
            successful_rps_str = ", ".join(str(r["rps"]) for r in successful_runs)
            print(
                f"  {gateway} [{subgraph_tech}]: total={total_run_count}, successful={successful_run_count}, "
                f"errors={runs_with_errors}, successful RPS=[{successful_rps_str}], "
                f"median={median_rps}, best={best_rps}, worst={worst_rps}, CV={cv_pct}%"
            )
            selected.append({
                "gateway": gateway,
                "subgraph_tech": subgraph_tech,
                "version": version,
                "median_rps": median_rps,
                "best_rps": best_rps,
                "worst_rps": worst_rps,
                "cv_pct": cv_pct,
                "notes": notes,
                "metrics": median_run["metrics"],
                "k6_txt": median_run["result"]["k6_txt"],
                "summary": median_run["result"]["summary"],
                "display_name": runs[0]["display_name"],
                "execution_failed": execution_failed,
                "json": json_data,
            })
        else:
            print(
                f"  {gateway} [{subgraph_tech}]: total={total_run_count}, successful=0, errors={runs_with_errors} "
                f"(reporting as failed row)"
            )
            selected.append({
                "gateway": gateway,
                "subgraph_tech": subgraph_tech,
                "version": version,
                "median_rps": None,
                "best_rps": None,
                "worst_rps": None,
                "cv_pct": None,
                "notes": notes or "benchmark run failed",
                "metrics": {},
                "k6_txt": "",
                "summary": {},
                "display_name": runs[0]["display_name"],
                "failed_run": True,
                "execution_failed": execution_failed,
                "json": json_data,
            })

    return selected


SCENARIOS = {
    "constant": (
        "constant-vus-over-time",
        "This scenario executes a constant load of **{vus} VUs** over **{duration}**.",
    ),
    "constant-latency": (
        "constant-vus-with-latency",
        "This scenario executes a constant load of **{vus} VUs** over **{duration}** "
        "with a simulated **{delay}ms IO delay** on each subgraph request.",
    ),
    "burst": (
        "burst-vus",
        "This scenario is a burst stress test with peaks up to **{vus} VUs** over **{duration}**.",
    ),
}

# Must match k6/benchmark.sh; used for runs that predate subgraph_delay_ms.
SUBGRAPH_DELAY_MS = {"constant-latency": 4}


def generate_markdown(mode, results, expected_by_mode, not_run_by_mode=None):
    """Generate results markdown for a given mode."""
    mode_results = [r for r in results if r["metadata"]["mode"] == mode]
    expected_mode_gateways = expected_by_mode.get(mode, [])
    not_run_keys = (not_run_by_mode or {}).get(mode, set())
    if not mode_results and not expected_mode_gateways:
        return None

    entries = []
    if mode_results:
        print(f"\nSelecting median runs for mode '{mode}':")
        entries = select_median_runs(mode_results)
    else:
        print(f"\nNo successful result artifacts for mode '{mode}', generating failure placeholders.")

    entries = merge_failed_expected_entries(entries, expected_mode_gateways, not_run_keys)
    entries = sort_entries(entries)

    if mode_results:
        first = mode_results[0]["summary"]
        first_metadata = mode_results[0].get("metadata", {})
        vus = first.get("vus", 50 if mode == "constant" else 500)
        duration = first.get("duration", "120s")
        try:
            measured_runs = int(first_metadata.get("total_runs", 9))
        except (TypeError, ValueError):
            measured_runs = 9
        measured_runs = max(1, measured_runs)
    else:
        vus = 50 if mode in {"constant", "constant-latency"} else 500
        duration = "120s"
        measured_runs = 9

    total_runs = measured_runs + 1

    lines = []

    # The setup sentence describes the current HotChocolate-only configuration. When
    # the input still contains historical non-HotChocolate measurements (preserved,
    # never relabeled), qualify the claim and call out the historical rows
    # instead of describing every benchmark as a HotChocolate run.
    techs_present = {e.get("subgraph_tech") for e in entries}
    historical_kinds = sorted(t for t in techs_present if t and t != ".net")
    setup_subject = (
        "Current benchmarks run" if historical_kinds else "Each benchmark runs"
    )

    SCENARIO_DESCRIPTION = (
        f"{setup_subject} a GraphQL gateway with 4 HotChocolate subgraphs "
        "([HotChocolate](https://github.com/ChilliCream/graphql-platform)) and executes "
        "a heavy nested query that exercises federation/composition capabilities.\n"
        "\n"
        "**Methodology:** Each gateway executes {total_runs} runs of {duration} each. The first run is a "
        "full-duration warmup (discarded). The remaining {measured_runs} runs are measured. Results are ranked "
        "by **median RPS** across the {measured_runs} measured runs, with best/worst/CV% reported for "
        "transparency."
    ).format(duration=duration, total_runs=total_runs, measured_runs=measured_runs)

    if historical_kinds:
        historical_labels = []
        if "rust" in historical_kinds:
            historical_labels.append("Rust")
        historical_labels.extend(
            t for t in historical_kinds if t != "rust"
        )
        SCENARIO_DESCRIPTION += (
            "\n\n> Historical note: this table also preserves earlier measurements taken "
            f"with {' and '.join(historical_labels)} subgraphs; only the HotChocolate rows "
            "reflect the current setup."
        )

    name, sentence = SCENARIOS.get(mode, SCENARIOS["burst"])
    lines.append(f"## Overview for: `{name}`")
    lines.append("")
    lines.append(SCENARIO_DESCRIPTION)
    lines.append("")
    lines.append(sentence.format(vus=vus, duration=duration, delay=SUBGRAPH_DELAY_MS.get(mode, 0)))

    # Split entries by subgraph technology
    rust_entries = [e for e in entries if e["subgraph_tech"] == "rust"]
    net_entries = [e for e in entries if e["subgraph_tech"] == ".net"]
    other_entries = [e for e in entries if e["subgraph_tech"] not in ("rust", ".net")]

    subgraph_groups = []
    if rust_entries:
        subgraph_groups.append(("Rust Subgraphs", rust_entries))
    if net_entries:
        subgraph_groups.append(("HotChocolate Subgraphs", net_entries))
    if other_entries:
        subgraph_groups.append(("Other Subgraphs", other_entries))

    # Emit comparison tables (no details yet)
    for group_title, group_entries in subgraph_groups:
        lines.append("")
        lines.append("")
        lines.append(f"### {group_title}")
        lines.append("")

        # Table header (no Subgraphs column — implicit from heading)
        lines.append(
            "| Gateway | Version | Median RPS | Best RPS | Worst RPS | CV% | Notes |"
        )
        lines.append(
            "| :------ | :------ | ---------: | -------: | --------: | --: | :---- |"
        )

        for entry in group_entries:
            gw = entry["gateway"]
            ver = entry["version"] or "—"
            notes = entry.get("notes", "")
            lines.append(
                f"| {gw} | {ver} | {format_metric(entry.get('median_rps'))} | "
                f"{format_metric(entry.get('best_rps'))} | {format_metric(entry.get('worst_rps'))} | "
                f"{format_cv(entry.get('cv_pct'))} | {notes} |"
            )

    # Expandable details — all at the bottom, grouped by subgraph tech
    lines.append("")
    lines.append("")
    lines.append("### Details")
    lines.append("")

    has_detail_entries = False
    for group_title, group_entries in subgraph_groups:
        for entry in group_entries:
            if entry.get("failed_run"):
                continue
            has_detail_entries = True
            gw = entry["gateway"]
            ver = entry["version"]
            display_name = entry.get("display_name", gw)
            gw_label = f"{display_name} ({ver})" if ver else display_name
            k6_txt = entry["k6_txt"].strip()

            lines.append("<details>")
            lines.append(f"  <summary>Summary for: {gw_label}</summary>")
            lines.append("")
            lines.append("  **K6 Output**")
            lines.append("")
            lines.append("")
            lines.append("```")
            lines.append(k6_txt)
            lines.append("```")
            lines.append("")
            lines.append("")
            lines.append("  **Performance Overview**")
            lines.append("")
            lines.append("")
            lines.append("  **no-image-available**")
            lines.append("")
            lines.append("")
            lines.append("")
            lines.append("  **HTTP Overview**")
            lines.append("")
            lines.append("")
            lines.append("  **no-image-available**")
            lines.append("")
            lines.append("")
            lines.append("</details>")
            lines.append("")

    if not has_detail_entries:
        lines.append("No successful benchmark runs available for detailed output.")
        lines.append("")

    lines.append("### Footnotes")
    lines.append("")
    hardware_profiles = collect_hardware_profiles(mode_results)
    if hardware_profiles:
        for idx, (profile, count) in enumerate(hardware_profiles, 1):
            host, os_name, cpu, cores, ram = profile
            parts = []
            if host:
                parts.append(f"host={host}")
            if os_name:
                parts.append(f"os={os_name}")
            if cpu:
                parts.append(f"cpu={cpu}")
            if cores:
                parts.append(f"cores={cores}")
            if ram:
                parts.append(f"ram={ram}")
            suffix = f" (observed in {count} run metadata entries)" if len(hardware_profiles) > 1 else ""
            lines.append(f"- Benchmark hardware #{idx}: " + ", ".join(parts) + suffix)
    else:
        lines.append("- Benchmark hardware: unavailable in metadata.")

    lines.append("")

    return "\n".join(lines)


JSON_MODES = ("constant", "constant-latency", "burst")

_REPORT_METADATA = None


def load_report_metadata():
    """Load k6/build-matrix.py (gateway catalog)."""
    global _REPORT_METADATA
    if _REPORT_METADATA is None:
        path = Path(__file__).with_name("build-matrix.py")
        spec = importlib.util.spec_from_file_location("build_matrix", path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        _REPORT_METADATA = module
    return _REPORT_METADATA


def parse_leading_number(value):
    """'16 logical (16 CPUs)' -> 16, '122 GB' -> 122."""
    token = str(value or "").strip().split(" ", 1)[0]
    try:
        number = float(token)
    except ValueError:
        return None
    return int(number) if number.is_integer() else number


def json_hardware(results):
    """Most common hardware profile across the run's metadata."""
    profiles = collect_hardware_profiles(results)
    if not profiles:
        return None
    host, os_name, cpu, cores, ram = profiles[0][0]
    hardware_threads = parse_leading_number(cores)
    memory_gb = parse_leading_number(ram)
    if not (host and os_name and cpu and hardware_threads and memory_gb is not None):
        return None
    hardware = {
        "host": host,
        "cpuModel": cpu,
        "hardwareThreads": int(hardware_threads),
    }
    topology = cpu_topology(results)
    if topology:
        threads_by_core = defaultdict(list)
        for thread, core in sorted(topology.items()):
            threads_by_core[core].append(thread)
        hardware["physicalCores"] = len(threads_by_core)
        hardware["threadsPerCore"] = max(len(threads) for threads in threads_by_core.values())
        hardware["coreThreads"] = [threads_by_core[core] for core in sorted(threads_by_core)]
    hardware["memoryGb"] = memory_gb
    hardware["os"] = os_name
    return hardware


def json_version(value):
    """Gateway version without a leading v."""
    version = str(value or "").strip()
    if version[:1] in ("v", "V"):
        version = version[1:]
    return version or None


def json_latency(summary):
    """http_req_duration trend stats (ms) of a k6 summary."""
    values = summary.get("metrics", {}).get("http_req_duration", {}).get("values", {})
    keys = [
        ("avg", "avg"), ("min", "min"), ("med", "med"), ("p90", "p(90)"),
        ("p95", "p(95)"), ("p99", "p(99)"), ("p99_9", "p(99.9)"), ("max", "max"),
    ]
    latency = {name: round(values[stat], 3) for name, stat in keys if stat in values}
    required = {"avg", "med", "p90", "p95", "p99_9", "max"}
    return latency if required <= latency.keys() else None


def parse_cpuset(cpuset):
    """'2-3,10-11' -> {2, 3, 10, 11}."""
    cpus = set()
    for part in str(cpuset or "").replace(" ", "").split(","):
        if not part:
            continue
        if "-" in part:
            low, high = part.split("-", 1)
            cpus.update(range(int(low), int(high) + 1))
        else:
            cpus.add(int(part))
    return cpus


def cpu_topology(results):
    """Most common hardware thread -> physical core map recorded by benchmark.sh, or {}."""
    topologies = Counter(
        tuple(tuple(pair) for pair in r.get("metadata", {}).get("cpu_topology") or [])
        for r in results
    )
    topologies.pop((), None)
    if not topologies:
        return {}
    return dict(topologies.most_common(1)[0][0])


CPU_ROLES = ("system", "k6", "gateway", "subgraphs")


def run_cpuset(metadata, role):
    """Cpuset of `role` from metadata, else profiles.json as benchmark.sh picks it."""
    key = f"{role}_cpuset"
    if key in metadata:
        return metadata[key] or ""
    hardware_threads = parse_leading_number(metadata.get("machine_cores"))
    if not hardware_threads:
        return ""
    profiles_path = Path(__file__).resolve().parent.parent / "profiles.json"
    if not profiles_path.exists():
        return ""
    with open(profiles_path) as f:
        profiles = json.load(f)
    profile = "16" if hardware_threads >= 16 else "8"
    k6_mode = "ramping" if metadata.get("mode") in ("burst", "ramping") else "constant"
    return profiles.get(profile, {}).get(k6_mode, {}).get(role, "")


def json_cpu_allocation(mode_results):
    """Most common per-run hardware threads of each role in a scenario."""
    allocations = Counter(
        tuple(run_cpuset(r.get("metadata", {}), role) for role in CPU_ROLES)
        for r in mode_results
    )
    if not allocations:
        return None
    cpusets = allocations.most_common(1)[0][0]
    topology = cpu_topology(mode_results)
    allocation = {}
    for role, cpuset in zip(CPU_ROLES, cpusets):
        threads = sorted(parse_cpuset(cpuset))
        if not threads:
            continue
        allocation[role] = {"threadIds": threads, "hardwareThreads": len(threads)}
        if topology and all(thread in topology for thread in threads):
            allocation[role]["physicalCores"] = len({topology[thread] for thread in threads})
    return allocation or None


def json_cpu(run):
    """Utilization of the gateway's pinned hardware threads, in percent."""
    samples = run.get("gateway_core_pct") or []
    if not samples:
        return None
    return {
        "avgUtilizationPercent": round(sum(samples) / len(samples), 1),
        "maxUtilizationPercent": round(max(samples), 1),
    }


def json_variant_result(entry):
    """Scenario result of one gateway variant."""
    data = entry["json"]
    stats = data["stats"]
    median = stats["median_run"]["result"]
    result = {
        "variant": entry["gateway"],
        "status": "failed" if entry.get("execution_failed") else "complete",
        "throughput": {
            "medianRps": stats["median_rps"],
            "bestRps": stats["best_rps"],
            "worstRps": stats["worst_rps"],
            "cvPercent": stats["cv_pct"],
            "runsRps": data["runs_rps"],
        },
    }
    latency = json_latency(median["summary"])
    if latency:
        result["latencyMs"] = latency
    result["requests"] = data["requests"]
    peak_rss_kb = median.get("memory", {}).get("peak_rss_kb")
    if peak_rss_kb:
        result["memory"] = {"peakRssKiB": int(peak_rss_kb)}
    cpu = json_cpu(median)
    if cpu:
        result["cpu"] = cpu
    return result


def load_json_env(var_name, default):
    raw = os.environ.get(var_name, "").strip()
    if not raw:
        return default
    try:
        return json.loads(raw)
    except json.JSONDecodeError as exc:
        print(f"Warning: failed to parse {var_name}: {exc}")
        return default


def json_run_info():
    """Run info from the GitHub Actions environment."""
    server = os.environ.get("GITHUB_SERVER_URL", "https://github.com")
    repository = os.environ.get("GITHUB_REPOSITORY", "")
    run_id = os.environ.get("GITHUB_RUN_ID", "")
    run = {
        "number": int(os.environ["GITHUB_RUN_NUMBER"]),
        "startedAt": os.environ.get("RUN_STARTED_AT", "").strip() or None,
        "completedAt": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "sourceCommit": os.environ["GITHUB_SHA"],
        "event": os.environ["GITHUB_EVENT_NAME"],
    }
    if repository and run_id:
        run["url"] = f"{server}/{repository}/actions/runs/{run_id}"
    inputs = load_json_env("RUN_INPUTS_JSON", None)
    if isinstance(inputs, dict) and inputs:
        run["inputs"] = inputs
    return {
        key: run[key]
        for key in ("number", "url", "startedAt", "completedAt", "sourceCommit", "event", "inputs")
        if key == "startedAt" or run.get(key) is not None
    }


def most_common_value(results, field):
    values = Counter(
        value
        for r in results
        if (value := r["metadata"].get(field)) is not None and value != ""
    )
    return values.most_common(1)[0][0] if values else None


def json_scenario_description(mode, config):
    """The markdown scenario sentence as plain text."""
    if "virtualUsers" not in config or "durationSeconds" not in config:
        return ""
    return SCENARIOS[mode][1].replace("**", "").format(
        vus=config["virtualUsers"], duration=f"{config['durationSeconds']}s", delay=config["subgraphDelayMs"],
    )


def planned_duration_seconds(mode, summary):
    """Planned load duration from k6.js's summary; None if not reliably known."""
    if "plannedDurationSeconds" in summary:
        return int(summary["plannedDurationSeconds"])
    # Older burst summaries report the unused BENCH_OVER_TIME, not the stage sum.
    if mode == "burst":
        return None
    try:
        return int(str(summary["duration"]).rstrip("s"))
    except (KeyError, ValueError):
        return None


def json_scenario_config(mode, mode_results):
    """Scenario config as the runs executed it."""
    subgraph_delay_ms = most_common_value(mode_results, "subgraph_delay_ms")
    if subgraph_delay_ms is None:
        # Runs created before this metadata field existed use the scenario default.
        subgraph_delay_ms = SUBGRAPH_DELAY_MS.get(mode, 0)
    config = {
        # benchmark.sh always runs a single warmup.
        "warmupRuns": 1,
        "subgraphDelayMs": int(subgraph_delay_ms),
    }
    if not mode_results:
        return config
    summaries = [r["summary"] for r in mode_results]
    planned = Counter(planned_duration_seconds(mode, s) for s in summaries)
    planned.pop(None, None)
    if planned:
        config["durationSeconds"] = planned.most_common(1)[0][0]
    peaks = [s.get("metrics", {}).get("vus_max", {}).get("values", {}).get("max") for s in summaries]
    peaks = [int(p) for p in peaks if p]
    if peaks:
        config["virtualUsers"] = max(peaks)
    runs_per_gateway = Counter(Counter(get_gateway_name(r["metadata"]) for r in mode_results).values())
    config["measuredRuns"] = runs_per_gateway.most_common(1)[0][0]
    order = ("durationSeconds", "virtualUsers", "subgraphDelayMs", "warmupRuns", "measuredRuns")
    return {key: config[key] for key in order if key in config}


def generate_results_json(results, expected_by_mode):
    """Build the results.json document."""
    meta = load_report_metadata()
    catalog = meta.report_gateway_catalog()
    variant_to_product = {v["id"]: p["id"] for p in catalog for v in p["variants"]}
    job_results = load_json_env("SCENARIO_RESULTS_JSON", {})

    def mode_of(r):
        mode = r["metadata"].get("mode")
        return "burst" if mode == "ramping" else mode

    in_run = {e["gateway"] for entries in expected_by_mode.values() for e in entries}
    in_run |= {get_gateway_name(r["metadata"]) for r in results}
    unknown = sorted(in_run - variant_to_product.keys())
    if unknown:
        print(f"Error: no variant in k6/benchmark-config.json for: {', '.join(unknown)}")
        sys.exit(1)

    versions, runtime_versions = {}, {}
    for r in results:
        name = get_gateway_name(r["metadata"])
        versions[name] = versions.get(name) or json_version(get_version(r["metadata"]))
        runtime_versions[name] = runtime_versions.get(name) or r["metadata"].get("runtime_version") or None

    scenarios = []
    for mode in JSON_MODES:
        mode_results = [r for r in results if mode_of(r) == mode]
        job_result = job_results.get(mode)
        if job_result == "skipped" or (job_result is None and not mode_results):
            continue
        entries = []
        if mode_results:
            print(f"\nSelecting median runs for mode '{mode}':")
            entries = sort_entries(select_median_runs(mode_results))

        by_variant = {}
        for entry in entries:
            by_variant[entry["gateway"]] = json_variant_result(entry)
        for expected in expected_by_mode.get(mode, []):
            by_variant.setdefault(
                expected["gateway"],
                {"variant": expected["gateway"], "status": "failed"},
            )

        grouped = defaultdict(list)
        for product in catalog:
            for variant in product["variants"]:
                if variant["id"] in by_variant:
                    grouped[product["id"]].append(by_variant[variant["id"]])

        config = json_scenario_config(mode, mode_results)
        cpu_allocation = json_cpu_allocation(mode_results)
        if cpu_allocation:
            config["cpuAllocation"] = cpu_allocation
        scenario = {"id": mode, "name": SCENARIOS[mode][0]}
        description = json_scenario_description(mode, config)
        if description:
            scenario["description"] = description
        scenario.update({
            "config": config,
            "results": [
                {"gateway": product["id"], "variants": grouped[product["id"]]}
                for product in catalog
                if grouped.get(product["id"])
            ],
        })
        scenarios.append(scenario)

    gateways = []
    for product in catalog:
        variants = []
        for v in product["variants"]:
            if v["id"] not in in_run:
                continue
            variant = {"id": v["id"], "version": versions.get(v["id"]), **{k: v[k] for k in v if k != "id"}}
            runtime_version = runtime_versions.get(v["id"])
            if runtime_version and "runtime" in variant["platform"]:
                variant["platform"] = {
                    **variant["platform"],
                    "runtime": {**variant["platform"]["runtime"], "version": runtime_version},
                }
            variants.append(variant)
        if variants:
            gateways.append({**product, "variants": variants})

    hardware = json_hardware(results)
    if hardware is None:
        print("Error: no benchmark hardware metadata found in the result artifacts.")
        sys.exit(1)

    subgraphs = meta.SUBGRAPHS
    framework = dict(subgraphs["framework"])
    platform = meta.report_platform(subgraphs["language"], subgraphs.get("runtime"))
    hotchocolate = most_common_value(results, "subgraphs_hotchocolate_version")
    if hotchocolate:
        framework["version"] = hotchocolate
    subgraph_runtime = most_common_value(results, "subgraphs_runtime_version")
    if subgraph_runtime:
        platform["runtime"]["version"] = subgraph_runtime
    return {
        "run": json_run_info(),
        "hardware": hardware,
        "subgraphs": {"framework": framework, "platform": platform},
        "gateways": gateways,
        "scenarios": scenarios,
    }


def main():
    args = [a for a in sys.argv[1:] if a != "--json"]
    write_json = len(args) != len(sys.argv) - 1
    if len(args) < 1:
        print("Usage: generate-results.py [--json] <artifacts-dir> [output-dir]")
        sys.exit(1)

    artifacts_dir = args[0]
    output_dir = args[1] if len(args) > 1 else "."
    expected_by_mode = load_expected_gateways_by_mode()
    not_run_by_mode = load_not_run_gateways_by_mode()
    requested_modes = {
        mode.strip() for mode in os.environ.get("RESULT_MODES", "").split(",")
        if mode.strip()
    }

    if requested_modes:
        print(f"Generating only requested modes: {', '.join(sorted(requested_modes))}")

    print(f"Scanning {artifacts_dir} for results...")
    results = find_results(artifacts_dir)

    if not results:
        if not has_any_expected_gateways(expected_by_mode):
            print("No results found!")
            sys.exit(1)
        print("No result artifacts found. Proceeding with expected gateway matrix only.")
    else:
        print(f"Found {len(results)} result(s)")

    os.makedirs(output_dir, exist_ok=True)

    if write_json:
        document = generate_results_json(results, expected_by_mode)
        output_path = os.path.join(output_dir, "results.json")
        # Rename so a crash never leaves a partial results.json.
        with open(output_path + ".tmp", "w", encoding="utf-8") as f:
            json.dump(document, f, indent=2, ensure_ascii=False)
            f.write("\n")
        os.replace(output_path + ".tmp", output_path)
        print(f"Generated {output_path}")
        return

    for mode, filename in [
        ("constant", "RESULTS_CONSTANT.md"),
        ("ramping", "RESULTS_BURST.md"),       # legacy metadata value
        ("burst", "RESULTS_BURST.md"),
        ("constant-latency", "RESULTS_CONSTANT_LATENCY.md"),
    ]:
        if requested_modes and mode not in requested_modes:
            continue
        md = generate_markdown(mode, results, expected_by_mode, not_run_by_mode)
        if md:
            output_path = os.path.join(output_dir, filename)
            with open(output_path, "w") as f:
                f.write(md + "\n")
            print(f"Generated {output_path}")
        else:
            print(f"No results for mode '{mode}', skipping {filename}")


if __name__ == "__main__":
    main()
