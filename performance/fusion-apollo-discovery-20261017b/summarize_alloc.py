#!/usr/bin/env python3
"""Summarize allocation ticks from the unchanged TraceAnalysis helper."""
import collections
import json
import sys

trace = json.load(open(sys.argv[1]))
types = collections.defaultdict(lambda: {"sampled_bytes": 0, "ticks": 0})
groups = collections.defaultdict(lambda: {"sampled_bytes": 0, "ticks": 0})
for allocation in trace["allocationEvents"]:
    type_name = allocation["TypeName"]
    chain = allocation["chain"]
    targets = [types[type_name]]
    if "FetchResultStore.BuildRepresentationValue(" in chain and type_name in [
            "HotChocolate.Text.Json.JsonWriter", "VariableDedupTable",
            "HotChocolate.Buffers.ChunkedArrayWriter"]:
        targets.append(groups["representation_scratch_objects"])
    if "RepresentationValue.ToVariableValues()" in chain:
        targets.append(groups["representation_error_variables"])
    if "RepresentationPathAccumulator.ApplyTo(" in chain:
        targets.append(groups["duplicate_representation_paths"])
    if type_name == "HotChocolate.Fusion.Execution.EntityResultPath[]":
        targets.append(groups["immutable_entity_paths"])
    for target in targets:
        target["sampled_bytes"] += allocation["bytes"]
        target["ticks"] += 1

def ordered(values):
    return [{"name": key, **value,
             "sampled_byte_percent": value["sampled_bytes"] / trace["sampledBytes"] * 100}
            for key, value in sorted(values.items(), key=lambda item: -item[1]["sampled_bytes"])]

json.dump({
    "source": sys.argv[1],
    **{key: trace[key] for key in ["targetPid", "traceStartUtc", "traceEndUtc", "reportedEventsLost", "sampledBytes", "events"]},
    "allocation_ticks": len(trace["allocationEvents"]),
    "first_allocation_tick": min(event["utc"] for event in trace["allocationEvents"]),
    "last_allocation_tick": max(event["utc"] for event in trace["allocationEvents"]),
    "legacy_amount_fallback_ticks": sum(event["usedLegacyAmountFallback"] for event in trace["allocationEvents"]),
    "types": ordered(types), "groups": ordered(groups),
    "caller_groups": trace["frames"],
    "limitation": trace["limitation"]
}, sys.stdout, indent=2)
