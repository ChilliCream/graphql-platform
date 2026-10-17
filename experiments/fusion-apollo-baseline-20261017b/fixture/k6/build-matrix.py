#!/usr/bin/env python3
"""Build the benchmark gateway/subgraph matrices.

All benchmarks run against the HotChocolate subgraphs only; each
gateway therefore contributes exactly one matrix entry. The benchmark machines
have no .NET installed, so the subgraphs-net prebuilt artifacts carry their
own bundled runtime (see each subgraphs-net/build.sh).

Reads the gateway selection from the SELECTED_GATEWAY environment variable
("all" or a gateway base name such as "fusion" or "fusion-nightly-net11") and
an optional exact HotChocolate pin from FUSION_VERSION (empty means resolve
the latest per channel; applies to fusion-family gateways only) and writes
the matrices to the file named by GITHUB_OUTPUT (or stdout when
GITHUB_OUTPUT is unset, for local inspection).
"""

import json
import os
import re
import sys
import urllib.request

SUBGRAPHS_DIR = "subgraphs-net"
SUBGRAPH_TECH = ".net"

CONFIG_PATH = os.path.join(os.path.dirname(os.path.abspath(__file__)), "benchmark-config.json")
with open(CONFIG_PATH, encoding="utf-8") as _fh:
    CONFIG = json.load(_fh)

GATEWAYS = CONFIG["gateways"]
SUBGRAPHS = CONFIG["subgraphs"]

VARIANTS = [variant for product in GATEWAYS for variant in product["variants"]]
VARIANTS_BY_ID = {variant["id"]: variant for variant in VARIANTS}
PRODUCT_BY_VARIANT = {v["id"]: product["id"] for product in GATEWAYS for v in product["variants"]}

# Gateways whose HotChocolate packages install.sh resolves: an explicit
# FUSION_VERSION pins all of these; every other gateway ignores it.
FUSION_BASE_NAMES = tuple(v["id"] for v in VARIANTS if PRODUCT_BY_VARIANT[v["id"]] == "fusion")

# Fusion variants running preview HotChocolate packages.
NIGHTLY_BASE_NAMES = tuple(v for v in FUSION_BASE_NAMES if VARIANTS_BY_ID[v].get("channel") == "preview")


def report_platform(language, runtime=None, features=()):
    platform = {"language": {"name": language}}
    if runtime:
        platform["runtime"] = {"name": runtime}
    if features:
        platform["features"] = [dict(f) for f in features]
    return platform


def report_gateway_catalog():
    catalog = []
    for product in GATEWAYS:
        variants = []
        for gateway in product["variants"]:
            variant = {"id": gateway["id"]}
            if gateway.get("label"):
                variant["label"] = gateway["label"]
            if "preview" in gateway:
                variant["preview"] = gateway["preview"]
            variant.update({
                "composition": gateway["path"].split("/", 1)[0],
                "platform": report_platform(
                    gateway["language"],
                    gateway.get("runtime"),
                    gateway.get("platformFeatures", ()),
                ),
            })
            if gateway.get("gatewayFeatures"):
                variant["features"] = [dict(f) for f in gateway["gatewayFeatures"]]
            variants.append(variant)
        catalog.append({
            "id": product["id"],
            "name": product["name"],
            "vendor": product["vendor"],
            "repositoryUrl": product["repositoryUrl"],
            "variants": variants,
        })
    return catalog


SUBGRAPH_ARTIFACT_BY_CATEGORY = {
    "apollo-federation": "prebuilt-apollo-federation-subgraphs",
    "composite-schema": "prebuilt-composite-schema-subgraphs",
}


# Numeric X.Y.Z with an optional prerelease suffix of nonempty dot-separated
# identifiers (e.g. "16.6.8", "16.7.0-p.27"). Mirrored by the
# BENCH_GATEWAY_VERSION check in both fusion install.sh scripts — keep the
# two in sync.
FUSION_VERSION_PATTERN = re.compile(r"^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$")


def validate_fusion_version(raw):
    """Normalize the FUSION_VERSION env value, failing fast on garbage."""
    version = (raw or "").strip()
    if not version:
        return ""
    if not FUSION_VERSION_PATTERN.match(version):
        print(
            f"Error: invalid FUSION_VERSION={raw!r} "
            "(expected numeric X.Y.Z with optional prerelease suffix, "
            "e.g. '16.6.8' or '16.7.0-p.27')",
            file=sys.stderr,
        )
        sys.exit(1)
    return version


def _parse_core(core):
    nums = []
    for part in core.split("."):
        if not part.isdigit():
            return None
        nums.append(int(part))
    return tuple(nums)


def _prerelease_key(pre):
    # SemVer 2.0: numeric identifiers rank below alphanumeric and compare numerically.
    key = []
    for ident in pre.split("."):
        if ident.isdigit():
            key.append((0, int(ident), ""))
        else:
            key.append((1, 0, ident))
    return tuple(key)


def resolve_hotchocolate_channels(versions):
    # Return (preview_ahead, latest_stable, latest_preview). preview_ahead is
    # True only when the newest prerelease is on a higher core (X.Y.Z) line than
    # the newest stable release (an in-flight preview ahead of what has shipped).
    stable = []
    preview = []
    for raw in versions:
        core, sep, pre = raw.partition("-")
        core_tuple = _parse_core(core)
        if core_tuple is None:
            continue
        if sep:
            preview.append((core_tuple, _prerelease_key(pre), raw))
        else:
            stable.append((core_tuple, raw))
    latest_stable = max(stable, key=lambda x: x[0]) if stable else None
    latest_preview = max(preview, key=lambda x: (x[0], x[1])) if preview else None
    stable_str = latest_stable[1] if latest_stable else None
    preview_str = latest_preview[2] if latest_preview else None
    if latest_preview is None:
        ahead = False
    elif latest_stable is None:
        ahead = True
    else:
        ahead = latest_preview[0] > latest_stable[0]
    return ahead, stable_str, preview_str


def hotchocolate_preview_is_ahead():
    # Query NuGet for the HotChocolate version feed. On any failure return
    # (False, None, None) so fusion-nightly is skipped unless explicitly asked for.
    url = "https://api.nuget.org/v3-flatcontainer/hotchocolate.aspnetcore/index.json"
    try:
        with urllib.request.urlopen(url, timeout=30) as resp:
            versions = json.load(resp).get("versions", [])
            return resolve_hotchocolate_channels(versions)
    except Exception as exc:  # noqa: BLE001
        print(
            f"Warning: could not query NuGet for HotChocolate preview status ({exc}); "
            "fusion-nightly will be skipped unless explicitly selected."
        )
        return False, None, None


def build_gateway_entries(fusion_version=""):
    """Expand each base gateway into its single HotChocolate-subgraphs matrix entry.

    An explicit fusion_version pins the fusion-family entries: they carry a
    "version" field and the version in their display name. Non-fusion entries
    ignore it, and an empty version leaves every entry exactly as before.
    """
    entries = []
    for variant in VARIANTS:
        name = variant["id"]
        category = variant["path"].split("/", 1)[0]
        entry = {
            "path": variant["path"],
            "name": name,
            "base_name": name,
            "channel": variant.get("channel", "stable"),
            "dotnet": variant.get("dotnet", "10"),
            "subgraphs": SUBGRAPHS_DIR,
            "subgraph_tech": SUBGRAPH_TECH,
            "display_name": f"{name} (HotChocolate subgraphs)",
            "subgraphs_artifact": SUBGRAPH_ARTIFACT_BY_CATEGORY[category],
            "gateway_artifact": f"prebuilt-gateway-{name}",
        }
        if fusion_version and name in FUSION_BASE_NAMES:
            entry["version"] = fusion_version
            entry["display_name"] = f"{name} {fusion_version} (HotChocolate subgraphs)"
        entries.append(entry)
    return entries


def is_excluded(entry, mode):
    # Per-mode exclusions: gateway/subgraph combinations that are intentionally
    # not benchmarked in a given mode. Excluded combinations are removed from the
    # run matrix but still surface in that mode's results table marked "not run"
    # (via NOT_RUN_GATEWAYS_JSON -> generate-results.py), so nothing is silently
    # dropped.
    #
    # No combination is excluded at the moment.
    return False


def split_for_mode(entries, mode):
    run = [e for e in entries if not is_excluded(e, mode)]
    not_run = [e for e in entries if is_excluded(e, mode)]
    return run, not_run


def select_gateways(selected, preview_check=hotchocolate_preview_is_ahead,
                   fusion_version=""):
    """Select matrix entries for a gateway selection string."""
    gateways = build_gateway_entries(fusion_version)

    if selected == "all":
        chosen = gateways
    else:
        chosen = [entry for entry in gateways if entry["base_name"] == selected]

    # Skip the fusion-nightly (preview) variants — both the .NET 10 and the
    # .NET 11 build — unless a HotChocolate prerelease newer than the latest
    # stable exists; otherwise "nightly" would benchmark a build older than (or
    # equal to) the stable run. An explicit fusion-nightly* selection — or an
    # explicit FUSION_VERSION pin — is always honored.
    nightly_in_chosen = any(e["base_name"] in NIGHTLY_BASE_NAMES for e in chosen)
    nightly_explicit = selected in NIGHTLY_BASE_NAMES
    if nightly_in_chosen and not nightly_explicit and not fusion_version:
        preview_ahead, latest_stable_str, latest_preview_str = preview_check()
        if preview_ahead:
            print(
                f"Including fusion-nightly variants: preview {latest_preview_str} is "
                f"ahead of stable {latest_stable_str}."
            )
        else:
            chosen = [e for e in chosen if e["base_name"] not in NIGHTLY_BASE_NAMES]
            print(
                "Skipping fusion-nightly variants: newest prerelease "
                f"({latest_preview_str or 'none'}) is not ahead of latest stable "
                f"({latest_stable_str or 'none'})."
            )
    elif nightly_in_chosen and fusion_version:
        print(
            f"Including fusion-nightly variants: explicit FUSION_VERSION "
            f"{fusion_version} pins the HotChocolate packages."
        )

    if not chosen:
        print(f"Error: no gateway entries match selection {selected!r}", file=sys.stderr)
        sys.exit(1)

    return chosen


def build_matrices(selected, preview_check=hotchocolate_preview_is_ahead,
                   fusion_version=""):
    """Compute all workflow matrices for a gateway selection string."""
    fusion_version = validate_fusion_version(fusion_version)
    chosen = select_gateways(selected, preview_check, fusion_version)

    chosen_constant, not_run_constant = split_for_mode(chosen, "constant")
    chosen_burst, not_run_burst = split_for_mode(chosen, "burst")

    builds_by_artifact = {}
    for entry in chosen:
        artifact = entry["gateway_artifact"]
        if artifact in builds_by_artifact:
            continue
        build = {
            "path": entry["path"],
            "artifact": artifact,
            "channel": entry["channel"],
            "dotnet": entry["dotnet"],
        }
        if entry.get("version"):
            build["version"] = entry["version"]
        builds_by_artifact[artifact] = build

    subgraphs_by_artifact = {}
    for entry in chosen:
        artifact = entry["subgraphs_artifact"]
        if artifact in subgraphs_by_artifact:
            continue
        category = entry["path"].split("/", 1)[0]
        subgraphs_by_artifact[artifact] = {
            "path": f"{category}/{entry['subgraphs']}",
            "artifact": artifact,
        }

    return {
        "gateways": chosen,
        "gateways_constant": chosen_constant,
        "not_run_constant": not_run_constant,
        "gateways_burst": chosen_burst,
        "not_run_burst": not_run_burst,
        "gateway_builds": list(builds_by_artifact.values()),
        "subgraph_builds": list(subgraphs_by_artifact.values()),
    }


def main():

    selected = os.environ.get("SELECTED_GATEWAY", "all")
    fusion_version = validate_fusion_version(os.environ.get("FUSION_VERSION", ""))
    matrices = build_matrices(selected, fusion_version=fusion_version)

    output_path = os.environ.get("GITHUB_OUTPUT", "")
    output_lines = [
        f"gateways={json.dumps(matrices['gateways'], separators=(',', ':'))}",
        f"gateways_constant={json.dumps(matrices['gateways_constant'], separators=(',', ':'))}",
        f"not_run_constant={json.dumps(matrices['not_run_constant'], separators=(',', ':'))}",
        f"gateways_burst={json.dumps(matrices['gateways_burst'], separators=(',', ':'))}",
        f"not_run_burst={json.dumps(matrices['not_run_burst'], separators=(',', ':'))}",
        f"gateway_builds={json.dumps(matrices['gateway_builds'], separators=(',', ':'))}",
        f"subgraph_builds={json.dumps(matrices['subgraph_builds'], separators=(',', ':'))}",
    ]
    if output_path:
        with open(output_path, "a", encoding="utf-8") as fh:
            for line in output_lines:
                fh.write(line + "\n")
    else:
        for line in output_lines:
            print(line)

    print("Selected gateway entries:")
    for entry in matrices["gateways"]:
        print(f" - {entry['base_name']} [{entry['subgraph_tech']}]")
    if fusion_version:
        print(
            f"Pinned HotChocolate version for fusion-family gateways: {fusion_version}"
        )

    print("Subgraph builds:")
    for sub in matrices["subgraph_builds"]:
        print(f" - {sub['path']} ({sub['artifact']})")


if __name__ == "__main__":
    main()
