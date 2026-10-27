import type { BenchmarkScenario } from "@/src/helpers/benchmarkReport";

interface BenchmarkLoadShapeProps {
  readonly scenario: BenchmarkScenario;
  readonly className?: string;
}

const BASE_USERS = 50;

/** Returns the virtual users over time of a scenario as [seconds, users] points. */
function loadPoints(scenario: BenchmarkScenario): readonly [number, number][] {
  const duration = scenario.config.durationSeconds ?? 120;
  const users = scenario.config.virtualUsers ?? BASE_USERS;
  if (scenario.id === "burst") {
    return [
      [0, BASE_USERS],
      [10, BASE_USERS],
      [duration - 10, users],
      [duration, BASE_USERS],
    ];
  }
  return [
    [0, users],
    [duration, users],
  ];
}

/** A small drawing of a scenario's virtual users over time, scaled to that scenario's own peak and duration. */
export function BenchmarkLoadShape({
  scenario,
  className = "",
}: BenchmarkLoadShapeProps) {
  const points = loadPoints(scenario);
  const duration = points.at(-1)![0];
  const peak = Math.max(...points.map(([, users]) => users));
  const x = (seconds: number) => (seconds / duration) * 100;
  const y = (users: number) => 100 - (users / peak) * 80;
  const line = points.map(([s, u]) => `${x(s)},${y(u)}`).join(" ");

  return (
    <span className={`block ${className}`} aria-hidden="true">
      <span className="border-cc-ink-dim/40 relative block h-10 border-b">
        <svg
          className="absolute inset-0 size-full overflow-visible"
          viewBox="0 0 100 100"
          preserveAspectRatio="none"
        >
          <polygon
            points={`0,100 ${line} 100,100`}
            className="fill-current opacity-10"
          />
          <polyline
            points={line}
            fill="none"
            stroke="currentColor"
            strokeWidth={2}
            strokeLinejoin="round"
            strokeLinecap="round"
            vectorEffect="non-scaling-stroke"
          />
        </svg>
      </span>
      <span className="text-cc-ink-dim mt-1 flex justify-between text-[11px] leading-4 tabular-nums">
        <span>
          {peak === BASE_USERS || scenario.id !== "burst"
            ? `${peak} virtual users`
            : `${BASE_USERS} to ${peak} virtual users`}
        </span>
        <span>{duration} s</span>
      </span>
    </span>
  );
}
