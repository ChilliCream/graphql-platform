"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { Pane, Spinner } from "./Chrome";
import { TERM } from "./palette";

/**
 * "What is Fusion?": a gateway log tail. One query arrives from one client at
 * one endpoint, the distributed executor prints the plan, four fetches go out
 * to four subgraphs at once and come back one by one, and the tail closes with
 * the four results merged into a single response.
 *
 * Rest state: the whole request already logged, every fetch resolved with its
 * timing and the merged response line printed.
 */

const DUR = 12;

const appear = (name: string, at: number) => `
.${name} { animation: ${name} ${DUR}s linear infinite; }
@keyframes ${name} { 0%, ${at}% { opacity: 0; } ${at + 0.8}%, 100% { opacity: 1; } }`;

const phase = (name: string, from: number, to: number) => `
.${name} { animation: ${name} ${DUR}s linear infinite; }
@keyframes ${name} {
  0%, ${from}% { opacity: 0; }
  ${from + 0.8}%, ${to}% { opacity: 1; }
  ${to + 0.8}%, 100% { opacity: 0; }
}`;

interface Fetch {
  readonly stamp: string;
  readonly file: string;
  readonly fields: string;
  readonly took: string;
  /** Percentage of the loop at which the fetch comes back. */
  readonly back: number;
}

const FETCHES: readonly Fetch[] = [
  {
    stamp: "12:04:01.121",
    file: "catalog.ts",
    fields: "name, price",
    took: "6ms",
    back: 44,
  },
  {
    stamp: "12:04:01.121",
    file: "billing.java",
    fields: "taxRate",
    took: "9ms",
    back: 52,
  },
  {
    stamp: "12:04:01.122",
    file: "ordering.go",
    fields: "availability",
    took: "7ms",
    back: 48,
  },
  {
    stamp: "12:04:01.122",
    file: "shipping.rb",
    fields: "deliveryEstimate",
    took: "5ms",
    back: 40,
  },
];

const SENT_AT = 22;
const MERGE_AT = 58;

const CSS = `
${appear("ttl-in", 6)}
${appear("ttl-plan", 14)}
${FETCHES.map((_, i) => appear(`ttl-s${i}`, SENT_AT + i * 2)).join("\n")}
${FETCHES.map((f, i) => phase(`ttl-w${i}`, SENT_AT + i * 2 + 1, f.back)).join("\n")}
${FETCHES.map((f, i) => appear(`ttl-r${i}`, f.back + 1)).join("\n")}
${appear("ttl-merge", MERGE_AT)}
${appear("ttl-out", MERGE_AT + 4)}
`;

export function FanOutLog() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <Pane title="tail -f gateway.log" meta="one endpoint" run={run}>
      <style>{CSS}</style>

      <div className="ttl-in">
        <span style={{ color: TERM.dim }}>{"12:04:01.118  "}</span>
        <span style={{ color: TERM.prompt }}>{"← "}</span>
        <span>{"query ProductPage".padEnd(22)}</span>
        <span style={{ color: TERM.dim }}>client web</span>
      </div>

      <div className="ttl-plan">
        <span style={{ color: TERM.dim }}>{"12:04:01.119  "}</span>
        <span>{"plan  "}</span>
        <span style={{ color: TERM.dim }}>
          4 fetches · 2 stages · 5 subgraphs
        </span>
      </div>

      <div style={{ marginTop: "0.5em" }}>
        {FETCHES.map((f, i) => (
          <div key={f.file} className={`ttl-s${i}`}>
            <span style={{ color: TERM.dim }}>{`${f.stamp}  `}</span>
            <span style={{ color: TERM.prompt }}>{"→ "}</span>
            <span>{f.file.padEnd(14)}</span>
            <span style={{ color: TERM.dim }}>{f.fields.padEnd(19)}</span>
            <span className={`ttl-w${i}`} style={{ opacity: 0 }}>
              <Spinner />
            </span>
            <span className={`ttl-r${i}`} style={{ color: TERM.ok }}>
              {f.took}
            </span>
          </div>
        ))}
      </div>

      <div className="ttl-merge" style={{ marginTop: "0.5em" }}>
        <span style={{ color: TERM.dim }}>{"12:04:01.138  "}</span>
        <span>merge 4 results → 1 response</span>
      </div>

      <div className="ttl-out">
        <span style={{ color: TERM.dim }}>{"12:04:01.139  "}</span>
        <span style={{ color: TERM.prompt }}>{"← "}</span>
        <span style={{ color: TERM.ok }}>200 OK</span>
        <span style={{ color: TERM.dim }}>
          {"  21ms  1 query in, 1 response out"}
        </span>
      </div>
    </Pane>
  );
}
