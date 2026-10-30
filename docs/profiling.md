# Profiling

How to take CPU, allocation and memory snapshots of a .NET process that is running inside the dev container. The tools attach to a running process, so start the application and generate traffic the way you normally do.

## Tools

The dev container installs these tools, pinned in `.devcontainer/dockerfile`:

| Tool                                                                               | Version     | Location                                                                                                  |
| ---------------------------------------------------------------------------------- | ----------- | --------------------------------------------------------------------------------------------------------- |
| `dotnet-trace`, `dotnet-counters`, `dotnet-gcdump`, `dotnet-dump`, `dotnet-symbol` | 10.0.750501 | `~/.dotnet/tools`, dotnet global tools                                                                    |
| `dottrace`                                                                         | 2026.2.3.1  | `~/.dotnet/tools`, dotnet global tool `JetBrains.dotTrace.GlobalTools`                                    |
| `dotmemory`                                                                        | 2026.2.3.1  | `/opt/dotmemory` with the launcher in `/usr/local/bin`, unpacked from `JetBrains.dotMemory.Console.<rid>` |

`~/.dotnet/tools` is on `PATH` for every process in the container, including non-interactive shells. JetBrains publishes the dotMemory console profiler only as a per-architecture NuGet package, not as a dotnet tool, so it is unpacked instead of installed with `dotnet tool`.

The application runs in the same container as the tools, so no `/tmp` or PID namespace sharing is needed. The runtime creates its diagnostics socket under `$TMPDIR` (default `/tmp`). If you override `TMPDIR` for the application, run the tools with the same value.

## Output folder

Write everything to `perf/<label>-<git-short-sha>/` in the repository root. `perf/` is ignored by git.

```bash
OUT=perf/fusion-gateway-$(git rev-parse --short HEAD)
mkdir -p "$OUT"
```

The workspace folder is mounted from the host, so Rider on the host can open the `.dtp` and `.dmw` files straight from that folder.

## Find the process

```bash
dotnet-trace ps
```

Use the PID of the application process, not of `dotnet run` that launched it. For a web application, `lsof -nP -iTCP:<port> -sTCP:LISTEN -t` returns the same PID.

## CPU sampling trace

```bash
dotnet-trace collect -p <pid> --duration 00:00:00:30 --format speedscope -o "$OUT/cpu.nettrace"
dotnet-trace report "$OUT/cpu.nettrace" topN -n 25
dotnet-trace report "$OUT/cpu.nettrace" topN -n 25 --inclusive
```

This writes `cpu.nettrace` and `cpu.speedscope.json`. Open the speedscope file at <https://www.speedscope.app>, or analyze the nettrace with PerfView or `dotnet-trace report`.

## Allocation trace

```bash
dotnet-trace collect -p <pid> --profile gc-verbose --duration 00:00:00:15 -o "$OUT/alloc.nettrace"
```

`gc-verbose` records sampled allocation ticks with their call stacks and every GC. It is expensive, so keep the duration short. PerfView and the TraceEvent library read the `GCAllocationTick` events.

## Heap snapshots

```bash
dotnet-gcdump collect -p <pid> -o "$OUT/heap.gcdump"
dotnet-gcdump report "$OUT/heap.gcdump" > "$OUT/heap-report.txt"
```

The report lists live objects by type and size. Take two gcdumps a few minutes apart to find growth.

## Live counters

```bash
dotnet-counters monitor -p <pid> --counters System.Runtime,Microsoft.AspNetCore.Hosting,System.Net.Http
dotnet-counters collect -p <pid> --format json -o "$OUT/counters.json" --duration 00:00:00:20 --counters System.Runtime,Microsoft.AspNetCore.Hosting,System.Net.Http
```

`monitor` shows the values live in the terminal and has no file output. `collect` writes the same counters to a file, as JSON or CSV.

## dotTrace

`dottrace attach` supports the Sampling and Timeline profiling types. Tracing and Line-by-line are only available through `dottrace start`, which launches the process under the profiler.

Sampling, controlled through stdin. The profiler prints `##dotTrace["connected"]`, `##dotTrace["started"]` and `##dotTrace["snapshot-saved"]` as it goes, so send each command after the previous one was confirmed:

```bash
dottrace attach <pid> --profiling-type=Sampling --save-to="$OUT/dottrace-sampling.dtp" \
  --collect-data-from-start=off --service-input=stdin --service-output=on
##dotTrace["start"]
##dotTrace["get-snapshot"]
##dotTrace["disconnect"]
```

The same session from a script, waiting for the confirmations instead of typing:

```bash
LOG="$OUT/dottrace.log"; FIFO=$(mktemp -u); mkfifo "$FIFO"
dottrace attach <pid> --profiling-type=Sampling --save-to="$OUT/dottrace-sampling.dtp" --overwrite \
  --collect-data-from-start=off --service-input=stdin --service-output=on < "$FIFO" > "$LOG" 2>&1 &
exec 3>"$FIFO"
until grep -q '"connected"' "$LOG"; do sleep 1; done
printf '\n##dotTrace["start"]\r\n' >&3
sleep 30
printf '\n##dotTrace["get-snapshot"]\r\n' >&3
until grep -q '"snapshot-saved"' "$LOG"; do sleep 1; done
printf '\n##dotTrace["disconnect"]\r\n' >&3
wait; exec 3>&-; rm -f "$FIFO"
```

Tracing, for call counts, starts the application under the profiler instead of attaching. Point it at the application binary, not at `dotnet run`, so the profiled process is the application itself:

```bash
dottrace start --framework=NetCore --profiling-type=Tracing --save-to="$OUT/dottrace-tracing.dtp" \
  --collect-data-from-start=off --service-input=stdin --service-output=on \
  bin/Release/net10.0/<App>
```

A snapshot is the `.dtp` file plus its numbered `.dtp.0000` companions. Keep them together.

## dotMemory

One snapshot now:

```bash
dotmemory get-snapshot <pid> --save-to-dir="$OUT/dotmemory"
```

Attach and take a snapshot every 30 seconds, at most three, then detach:

```bash
dotmemory attach <pid> --trigger-timer=30s --trigger-max-snapshots=3 --timeout=2m --save-to-dir="$OUT/dotmemory"
```

`--trigger-mem-inc=50mb` and `--trigger-mem-limit=1gb` trigger on memory growth instead of time. The result is a `.dmw` workspace that holds all snapshots of the session.

## Dumps and symbols

`dotnet-dump collect -p <pid> -o "$OUT/full.dmp"` writes a full process dump for `dotnet-dump analyze`. `dotnet-symbol` downloads the runtime symbols a dump from another machine needs; dumps taken inside the container already resolve.

## Caveats

- Profile Release builds. Debug builds disable optimizations and distort both time and allocations.
- Sampling gives time but not call counts. Use dotTrace Tracing mode for call counts, which means starting the process with `dottrace start`.
- On Linux, EventPipe samples measure thread time, so idle thread pool frames such as `LowLevelLifoSemaphore.WaitForSignal`, `WaitHandle.WaitOneNoCheck`, `Monitor.Wait`, the finalizer thread in `GC.RunFinalizers`, `PollGCWorker` and a blocked file watcher in `Interop+Sys.Read` dominate the exclusive time. Ignore them and read the frames below them. The speedscope export puts a synthetic `CPU_TIME` or `UNMANAGED_CODE_TIME` leaf under every sample, and `?!?` stands for native frames without symbols, mostly GC threads. For the CPU a process really uses, read `dotnet.process.cpu.time` from `dotnet-counters`.
- Take the dotMemory snapshots last. In two sessions the profiled Fusion gateway crashed 45 seconds and two minutes after `dotmemory attach` had disconnected, under load, with a native fault that the runtime turned into a `createdump` minidump. Set `DOTNET_DbgEnableMiniDump=1` on the application if you want that dump, and restart the application after a dotMemory session before measuring anything else.
- `.dtp` and `.dmw` snapshots are for Rider on the host. Agents should use the `dotnet-trace` and `dotnet-gcdump` outputs, which are readable in the container.
- Attaching needs no special container privileges. It only requires that the application was not started with `DOTNET_EnableDiagnostics=0`.
