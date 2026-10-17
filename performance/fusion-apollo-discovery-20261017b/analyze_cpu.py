#!/usr/bin/env python3
"""Summarize this investigation's evented Speedscope thread-time export."""
import collections
import json
import sys

document = json.load(open(sys.argv[1]))
frames = [frame["name"] for frame in document["shared"]["frames"]]
idle_patterns = [
    "LowLevelLifoSemaphore.Wait", "WaitHandle.WaitOne", "Monitor.Wait", "ManualResetEventSlim.Wait",
    "GC.RunFinalizers", "PollGCWorker", "Interop+Sys.Read(",
    "WaitForSocketEvents", "SocketAsyncEngine.EventLoop", "Thread.Sleep",
    "WaitForNextTick", "WaitForEvent", "PosixSignalRegistration.WaitForSignal",
]
pseudo = {"CPU_TIME", "UNMANAGED_CODE_TIME", "(Non-Activities)", "Threads", "?!?"}
groups = {
    "representation_build": ["FetchResultStore.BuildRepresentationValue("],
    "representation_shape": ["FetchResultStore.TryWriteShapeLevel("],
    "representation_error_variables": ["RepresentationValue.ToVariableValues("],
    "representation_complete": ["FetchResultStore.CompleteRepresentationVariableValue("],
    "representation_dedup": ["FetchResultStore.TryDeduplicateRepresentation("],
    "dedup_table_clear": ["VariableDedupTable.RentClearedTable("],
    "representation_duplicate_paths": ["RepresentationPathAccumulator.ApplyTo("],
    "alias_body": ["AliasBatchedRequestBody."],
    "alias_split": ["AliasBatchResponseSplitter.", "HttpSourceSchemaClient.SplitResponse("],
    "value_completion": ["ValueCompletion."],
    "source_parse": ["SourceResultDocument.Parse("],
    "response_write": ["CompositeResultDocument.Write", "JsonResultFormatter.FormatInternalAsync"],
}
exclusive = collections.Counter()
inclusive = collections.Counter()
group_totals = collections.Counter()
managed_group_totals = collections.Counter()
leaf_kinds = collections.Counter()
wait_totals = collections.Counter()
stack_totals = collections.Counter()
total = kept = unknown = 0.0
profile_bounds = []
for profile in document["profiles"]:
    assert profile["type"] == "evented" and profile["unit"] == "milliseconds"
    profile_bounds.append({k: profile[k] for k in ["name", "startValue", "endValue"]})
    stack = []
    previous = profile["startValue"]
    for event in profile["events"]:
        duration = event["at"] - previous
        assert duration >= 0
        if duration > 0 and stack:
            names = [frames[i] for i in stack]
            total += duration
            waits = [pattern for pattern in idle_patterns if any(pattern in name for name in names)]
            if waits:
                wait_totals[waits[0]] += duration
            else:
                kept += duration
                kind = names[-1] if names[-1] in pseudo else "non_synthetic_leaf"
                leaf_kinds[kind] += duration
                real = [name for name in names if name not in pseudo
                        and not name.startswith("Thread (")
                        and not name.startswith("Process64 ")]
                leaf = real[-1] if real else "unresolved native-only stack"
                if not real:
                    unknown += duration
                exclusive[leaf] += duration
                for name in set(real):
                    inclusive[name] += duration
                for key, patterns in groups.items():
                    if any(pattern in name for pattern in patterns for name in names):
                        group_totals[key] += duration
                        if kind == "CPU_TIME":
                            managed_group_totals[key] += duration
                stack_totals[tuple(real)] += duration
        if event["type"] == "O":
            stack.append(event["frame"])
        else:
            assert stack.pop() == event["frame"]
        previous = event["at"]
    assert not stack

def rows(counter, limit=None):
    return [{"name": k, "sampled_thread_ms": v, "percent_filtered": v / kept * 100}
            for k, v in counter.most_common(limit)]

json.dump({
    "source": sys.argv[1], "profile_bounds": profile_bounds,
    "total_sampled_thread_ms": total, "filtered_sampled_thread_ms": kept,
    "unresolved_native_only_ms": unknown,
    "excluded_wait_ms": dict(wait_totals), "wait_patterns": idle_patterns,
    "synthetic_leaf_ms": dict(leaf_kinds),
    "managed_sample_groups": rows(managed_group_totals),
    "groups": rows(group_totals), "exclusive": rows(exclusive),
    "inclusive": rows(inclusive),
    "largest_stacks": [{"frames": list(k), "sampled_thread_ms": v}
                       for k, v in stack_totals.most_common(100)],
    "limitation": "EventPipe sampled thread time is not process CPU. Known idle/wait stacks are excluded explicitly; remaining time can include native/GC or unidentified waits. Inclusive categories overlap. Durations and percentages are diagnostic attribution, not invocation counts or exact CPU savings."
}, sys.stdout, indent=2)
