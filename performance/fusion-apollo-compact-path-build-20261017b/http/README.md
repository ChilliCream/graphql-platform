# Independent survivor HTTP validation preparation

This reuses the published baseline host, source fixtures, original k6 driver and corrected campaign/analysis primitives. The prior interrupted C2/C3 observations remain separate historical evidence; this new cohort compares independently micro-qualified engines with fresh matched controls. Candidate engines are never stacked. No HTTP execution is authorized by preparation.

Current zero-delay order: B,C9,C7,C10,B,C10,C7,C9,B. Every launch performs complete response preflight, original warmup30s, unchanged original TestQuery30s, entities920s and corrected ordinary fixed5000/s20s with200preallocated/maxVUs. Native/summary counts must agree, setup must be exactly100 per normal phase, and drops/errors must be zero. Nominal100000 is descriptive; actual scheduled/completed counts and inferred first/last iteration starts are reported. Source/gateway/generator/observer affinity, unique Authorization, minthreads256, connection cap256, the benchmark host's explicit DisableDefaultSecurity policy and disabled logging remain unchanged; this is not production-default security. Runtime identities and every frozen file are checked.

A separately prepared delayed stage uses the same independent order and four-millisecond source delay: originalwarm30s, original30s, exact original ramping500VU burst60s, then20s observed idle. It requires its own root grant after the zero-delay result gate. Preparation or completion of stage one never starts stage two automatically. No new retained-heap capture is proposed.

All comparisons include both adjacent controls, full control envelope/time trend, gateway CPU per native successful request, source/generator/observer headroom, response and sparse-tail clustering, native count consistency and runtime counter interval limitations. BDN elapsed savings are not a gateway CPU result. Preserve C10 known128 second-arm4.1–4.4% adverse micro control, C9 ordinary time shifts and noisy last baseline, and unknown exact real-response predicate coverage. Warm caller evidence establishes C7/C10 route presence, not affected-response counts.

The final work bound is computed from the frozen launch count:160seconds per zero-delay launch or200seconds per delayed launch, plus60seconds campaign margin and120seconds reserved for exact-owned cleanup. With the current nine launches this is25minutes work plus2minutes cleanup for stage one, and31minutes work plus2minutes cleanup for stage two. No automatic retry/resume. Use distinct new output paths and explicit root-grant text. Preparation only:

```sh
python3 campaign.py prepare --survivors C9,C7,C10 --stage zero --plan /workspaces/platform-1/perf/fusion-apollo-survivor-http-20261017b/zero-plan.json
```

Execution only after the exact frozen plan/runner review, all peers quiet, independent free-lock/load preflight and root grant:

```sh
flock -x -o /workspaces/platform-1/perf/fusion-common-path.lock taskset -c 0,8 python3 campaign.py execute --plan <reviewed-plan> --output <new-output> --root-grant <exact-grant>
```

The pinned baseline source remains43a8dca2 and baseline evidence ef9fc1159a5a7a980754ebd42b511030320482cd. Source adaptation begins from b0b775cffe53367f9b7ee6637308a50f148fe3ed/performance/fusion-apollo-duplicate-paths-20261017b/http, retaining vendor originals byte-for-byte. Freeze changes only explicit independent survivor selection, combined matching conditioning and removal of old retained-input dependencies. The analysis adds inferred last-iteration-start reporting from existing native points.

Required original scenarios versus investigation settings:
- Original constant mode: unchanged TestQuery and native closed50-VU load at zero source delay. The benchmark default duration is120s; this predeclared matched investigation uses30s measured phases.
- Original constant-latency mode: benchmark.sh maps this to the same50-VU constant driver with4ms source delay. It is covered by the separately conditional delayed stage, also30s measured here.
- Original burst/ramping mode: unchanged50→500→50VU schedule over10/40/10s, including native5s graceful ramp-down/stop policy. The60s stage remains unchanged.
- Investigation conditioning/controls:30s original-query warmup,20s entities9,20s ordinary fixed5000/s with200VUs, and20s post-burst idle observation. These supplement the original modes and do not establish throughput capacity.

First-baseline setup includes a bounded direct replay of the seven immutable original source requests, with original default/alias Accept policy, at most5s and2MiB per response. It records whitelisted response framing/type headers, protocol version, byte length and hash, never sensitive header values. It precedes30s conditioning. urllib transport differs from the gateway client, so capture predicates remain protocol-qualified; physical arena adjacency is unobserved. No production instrumentation or separate VU campaign is introduced.
