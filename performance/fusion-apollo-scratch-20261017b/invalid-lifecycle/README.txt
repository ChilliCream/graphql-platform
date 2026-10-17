INVALID NORMAL-LIFECYCLE MICRO CAMPAIGN

The first shared campaign completed ten timing launches and stopped when its
first baseline allocation launch detected a collection inside the no-GC
region. All original source, identities, observations and failure stderr are
retained. No production candidate was changed and no outcome from this
campaign qualifies as a candidate benefit or regression.

The fixture omitted MemoryArena.Seal before disposal. MemoryArena.cs states
that unsealed disposal deliberately abandons its pages and segment tables;
JsonMemory.BufferSize is 128KiB. Normal completed execution seals arenas before
returning their memory. This fixture therefore repeatedly generated unrelated
large-object garbage from setup. Baseline unique1 launch means ranged from
14.7 to 133.5 microseconds despite most observations lying between 6 and 22
microseconds. Some 128-request observations include roughly 280ms pauses.
These stalls are retained in arithmetic means, never trimmed.

The allocation failure only reports the collection guard exception. It does
not record the case, generation counters or latency-mode state. Its finally
block completed EndNoGCRegion without replacing the original exception. This
is insufficient to attribute the failure specifically to budget exhaustion
or to the arena omission. Corrected diagnostics retain the missing state.

Root and C3 owner independently confirmed the normal lifecycle defect from
source. A separately frozen corrected harness seals both arenas before any
store cleanup, retains complete affected Clean work in scope, and prints
allocation diagnostics outside scoped measurements. Candidate production
assemblies and all workload dimensions remain identical. A new bounded
campaign requires a fresh exclusive grant; this is correction of an identified
fixture error, not repetition of a valid result seeking a favorable outcome.
