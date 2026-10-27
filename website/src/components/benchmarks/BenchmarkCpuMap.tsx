import { CPU_GROUPS, cpuMap } from "@/src/components/benchmarks/benchmarkView";
import { SUBSECTION_HEADING } from "@/src/components/benchmarks/benchmarkHeadings";
import type { BenchmarkReport } from "@/src/helpers/benchmarkReport";

interface BenchmarkCpuMapProps {
  readonly id: string;
  readonly report: BenchmarkReport;
  readonly className?: string;
}

/** The hardware threads of the test machine by physical core, grouped and colored by the process group each one is pinned to. */
export function BenchmarkCpuMap({
  id,
  report,
  className = "",
}: BenchmarkCpuMapProps) {
  const groups = cpuMap(report);
  if (!groups) {
    return null;
  }

  return (
    <div className={className}>
      <h3 id={id} className={SUBSECTION_HEADING}>
        CPU pinning
      </h3>
      <ul
        aria-labelledby={id}
        className="mt-3 grid gap-1.5"
        style={{
          gridTemplateColumns: `repeat(${report.hardware.physicalCores}, minmax(0, 1fr))`,
        }}
      >
        {groups.map(({ group, thread, core, row }) => {
          const info = CPU_GROUPS.find((item) => item.id === group);
          return (
            <li
              key={thread}
              className={`flex h-8 items-center justify-center rounded text-xs tabular-nums ${
                info
                  ? "text-cc-white font-semibold"
                  : "border-cc-card-border text-cc-ink-dim border"
              }`}
              style={{
                backgroundColor: info?.color,
                gridColumn: core + 1,
                gridRow: row + 1,
              }}
            >
              <span className="sr-only">
                Hardware thread {thread}, physical core {core}: {info?.name}
              </span>
            </li>
          );
        })}
      </ul>
      <ul className="text-caption text-cc-prose mt-3 flex flex-wrap gap-x-5 gap-y-1">
        {CPU_GROUPS.map((group) => (
          <li key={group.id} className="flex items-center gap-2">
            <span
              aria-hidden="true"
              className="size-2.5 rounded-sm"
              style={{ backgroundColor: group.color }}
            />
            {group.name}
          </li>
        ))}
      </ul>
      <p className="text-caption text-cc-prose mt-3">
        Processes are pinned with <code>taskset</code>; all other processes are
        moved onto core 0. Kernel threads and interrupts can still run on any
        core.
      </p>
    </div>
  );
}
