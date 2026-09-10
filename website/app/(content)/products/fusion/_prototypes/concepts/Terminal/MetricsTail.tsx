"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { Pane } from "./Chrome";
import { SUBGRAPHS, TERM } from "./palette";

/**
 * Nitro band: the Fusion dashboard as a watch command. One row for the gateway
 * and one for every subgraph behind it, each with latency, throughput and
 * error rate, and a sparkline drawn out of block characters - the only moving
 * picture in this concept that is still nothing but text.
 *
 * Rest state: the tail frozen on one frame, every row and sparkline readable.
 */

const COLUMNS = 10;

const CSS = `
.ttm-a { animation: ttm-a 1.9s steps(1, end) infinite; }
.ttm-b { animation: ttm-b 1.9s steps(1, end) infinite; }
@keyframes ttm-a {
  0%, 49.9% { opacity: 1; }
  50%, 100% { opacity: 0; }
}
@keyframes ttm-b {
  0%, 49.9% { opacity: 0; }
  50%, 100% { opacity: 1; }
}
`;

const BARS = "▁▂▃▄▅▆▇";

/** Deterministic so the server and the client render the same first frame. */
const bar = (row: number, column: number, frame: number) =>
  BARS[(row * 3 + column * 5 + frame * 2 + ((row * column) % 4)) % BARS.length];

interface SparkProps {
  readonly row: number;
}

function Spark({ row }: SparkProps) {
  return (
    <span style={{ color: TERM.prompt }}>
      {Array.from({ length: COLUMNS }, (_, c) => (
        <span
          key={c}
          className="relative inline-block"
          style={{ width: "1ch", height: "1em" }}
        >
          <span
            className="ttm-a absolute inset-0"
            style={{ animationDelay: `${(c * 0.11 + row * 0.07).toFixed(2)}s` }}
          >
            {bar(row, c, 0)}
          </span>
          <span
            className="ttm-b absolute inset-0"
            style={{
              animationDelay: `${(c * 0.11 + row * 0.07).toFixed(2)}s`,
              opacity: 0,
            }}
          >
            {bar(row, c, 1)}
          </span>
        </span>
      ))}
    </span>
  );
}

interface MetricRow {
  readonly label: string;
  readonly p95: string;
  readonly rps: string;
  readonly err: string;
  readonly hot: boolean;
}

const ROWS: readonly MetricRow[] = [
  { label: "gateway", p95: "32ms", rps: "4.1k", err: "0.02%", hot: false },
  ...SUBGRAPHS.map((s, i) => ({
    label: s.file,
    p95: ["11ms", "24ms", "17ms", "9ms", "13ms"][i],
    rps: ["2.6k", "1.1k", "1.9k", "0.7k", "1.4k"][i],
    err: ["0.00%", "0.31%", "0.01%", "0.00%", "0.02%"][i],
    hot: i === 1,
  })),
];

export function MetricsTail() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <Pane title="nitro watch --fusion" meta="live" run={run}>
      <style>{CSS}</style>

      <div style={{ color: TERM.dim }}>
        {"TARGET".padEnd(16) +
          "P95".padStart(5) +
          "RPS".padStart(8) +
          "ERRORS".padStart(8) +
          "  LAST 60s"}
      </div>

      {ROWS.map((row, i) => (
        <div key={row.label} style={{ marginTop: i === 1 ? "0.4em" : 0 }}>
          <span style={{ color: i === 0 ? TERM.text : TERM.dim }}>
            {row.label.padEnd(16)}
          </span>
          <span>{row.p95.padStart(5)}</span>
          <span style={{ color: TERM.dim }}>{row.rps.padStart(8)}</span>
          <span style={{ color: row.hot ? TERM.warn : TERM.ok }}>
            {row.err.padStart(8)}
          </span>
          <span>{"  "}</span>
          <Spark row={i} />
        </div>
      ))}
    </Pane>
  );
}
