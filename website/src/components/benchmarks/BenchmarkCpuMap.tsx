import { CPU_GROUPS, cpuMap } from "@/src/components/benchmarks/benchmarkView";
import type { BenchmarkReport } from "@/src/helpers/benchmarkReport";

interface BenchmarkCpuMapProps {
  readonly id: string;
  readonly report: BenchmarkReport;
  readonly className?: string;
}

/** The logical CPUs of the test machine in two rows, grouped and colored by the process group each one is pinned to. */
export function BenchmarkCpuMap({
  id,
  report,
  className = "",
}: BenchmarkCpuMapProps) {
  const groups = cpuMap(report);
  if (!groups) {
    return null;
  }
  const columns = Math.ceil(groups.length / 2);
  const order = [
    undefined,
    ...CPU_GROUPS.map((group) => group.id).toReversed(),
  ];
  const cells = groups
    .map((group, cpu) => ({ group, cpu }))
    .toSorted((a, b) => order.indexOf(a.group) - order.indexOf(b.group));

  return (
    <div className={className}>
      <h3 id={id} className="text-cc-heading text-sm font-semibold">
        CPU pinning
      </h3>
      <ul
        aria-labelledby={id}
        className="mt-3 grid gap-1.5"
        style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` }}
      >
        {cells.map(({ group, cpu }) => {
          const info = CPU_GROUPS.find((item) => item.id === group);
          return (
            <li
              key={cpu}
              className={`flex h-8 items-center justify-center rounded text-xs tabular-nums ${
                info
                  ? "text-cc-white font-semibold"
                  : "border-cc-card-border text-cc-ink-dim border"
              }`}
              style={info ? { backgroundColor: info.color } : undefined}
            >
              <span className="sr-only">
                CPU {cpu}: {info?.name ?? "not assigned"}
              </span>
            </li>
          );
        })}
      </ul>
      <ul className="text-caption text-cc-prose mt-3 flex flex-wrap gap-x-5 gap-y-1">
        {order.map((id) => {
          const group = CPU_GROUPS.find((item) => item.id === id);
          const count = groups.filter((item) => item === id).length;
          return count === 0 ? null : (
            <li key={id ?? "unassigned"} className="flex items-center gap-2">
              <span
                aria-hidden="true"
                className={`size-2.5 rounded-sm ${group ? "" : "border-cc-card-border border"}`}
                style={group ? { backgroundColor: group.color } : undefined}
              />
              {group?.name ?? "Not assigned"}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
