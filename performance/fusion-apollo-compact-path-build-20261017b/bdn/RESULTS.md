# C9 compact-path construction micro result

**PASS_FOR_VU, not an HTTP CPU or implementation acceptance claim.** Both duplicate96-to-one candidate launches have lower mean elapsed time than both adjacent controls. The stable early controls support a 1.24–1.33 microsecond saving (2.2–2.4%); the second candidate improves 0.568 microseconds (1.0%) against its preceding control. Allocations are near neutral, as predicted. Root accepted the independent review and queued bounded real-load validation; no additional micro run is requested.

All five fresh launches completed from 17:49:38.183559 through 17:50:35.624882 UTC. Each retained all three cases and twelve actual observations of 1,000 completed operations, for 180 elapsed observations and fifteen native MemoryDiagnoser records. Native denominators are exactly 1,000. Runtime is the established .NET10.0.12 default server-GC policy, with identical common-host files and only the exact measured Execution DLL/PDB overlaid. Commands, sources and hashes are in METHOD.md and manifest.json; full raw/log/identity/cleanup records are in results/.

| Case | B1 mean (us) | C9 first mean (us) | B2 mean (us) | C9 second mean (us) | B3 mean (us) |
|---|---:|---:|---:|---:|---:|
| ordinary | 11.826685 | 11.793785 | 12.202891 | 11.683993 | 12.049846 |
| batch9 | 35.915004 | 34.899464 | 34.610215 | 33.700188 | 34.967100 |
| duplicate96-to-one | 56.126103 | 54.887987 | 56.219103 | 55.650821 | 59.689788 |

Duplicate96-to-one candidate comparisons against preceding/following controls are -2.21%/-2.37% and -1.01%/-6.77%. The final baseline is noisy (standard deviation4.219us, maximum72.410us), so its 6.77% contrast is not the typical gain. Ordinary also improves by0.28–4.25%, indicating broad elapsed-time shifts rather than attributing every difference to the changed path builder. Batch is mixed in the first arm (-2.83%/+0.84%) and lower in the second (-2.63%/-3.62%), without a repeated adverse pattern. All observations and early/late block summaries remain in analysis.json; none were trimmed.

Native allocated bytes per operation for ordinary are25687/25680/25687/25680/25712; batch33534/33534/33528/33528/33560; duplicate29360/29367/29352/29367/29392 in launch order. These are process-wide diagnostic totals, rounded after division by1000, and include background allocation. There is no allocation-saving claim. Candidate duplicate diagnostics report Gen0/1/2 counts3/1/1 versus baseline5/0/0; co-occurrence does not establish a timing or allocation cause. The exact-version MemoryDiagnoser source audit is linked in METHOD.md.

Correctness evidence is151 focused passing tests, with the four new shape oracles also passing against the exact pinned baseline engine. The independent source review found the same root/reference/index and64-visited-row boundary behavior, with existing builder/pool policy unchanged. The warm original-query profile attributes1.706% exclusive sampled thread time to CreateCompactPath; sampling gives time, not invocation counts or promised total speedup.

Preflight initially stopped before measurement because idle native MSBuild/VBCSCompiler servers existed. Exact process identities and one-second CPU deltas showed all sixteen idle; no unrelated servers were terminated. During the campaign, one preserved compiler process accumulated18CPU ticks while the other fifteen remained unchanged. This background activity is retained as a limitation, not an established cause or automatic invalidation. Owner cleanup found all six exact target/parent identities absent; independent cleanup verified the free lock at17:51:23.308776UTC. The independent execution_hotpaths review is retained alongside raw results.

The bounded conclusion is a repeatable affected-case elapsed improvement with neutral allocation and no repeated unaffected-case adverse pattern. Temporal drift and the noisy final control constrain its size. Real-load gateway CPU per successful request, latency tails, ordinary controls and headroom must establish practical benefit before a minimal implementation PR can qualify.
