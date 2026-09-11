"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { Pane } from "./Chrome";
import { TERM } from "./palette";

/**
 * "Composition protects the graph, Nitro protects your clients": the operation
 * registry as a table. A field is removed from one source schema, composition
 * still passes because the source schemas still agree with one another, and it
 * is the registry - every operation the registered clients actually publish -
 * that Nitro scans row by row to name the one client the change would break.
 *
 * Rest state: composition already green, every row scanned, all four verdicts
 * printed and the merge marked blocked.
 */

const DUR = 13;

const appear = (name: string, at: number) => `
.${name} { animation: ${name} ${DUR}s linear infinite; }
@keyframes ${name} { 0%, ${at}% { opacity: 0; } ${at + 0.8}%, 100% { opacity: 1; } }`;

/** The cursor bar Nitro drags down the registry while it checks a row. */
const scan = (name: string, at: number) => `
.${name} { animation: ${name} ${DUR}s linear infinite; }
@keyframes ${name} {
  0%, ${at}% { background: transparent; }
  ${at + 0.4}%, ${at + 5}% { background: ${TERM.scan}; }
  ${at + 5.6}%, 100% { background: transparent; }
}`;

type Verdict = "safe" | "risky" | "breaking";

interface Row {
  readonly client: string;
  readonly operation: string;
  readonly fields: string;
  readonly verdict: Verdict;
}

const ROWS: readonly Row[] = [
  { client: "web", operation: "CatalogPage", fields: "12", verdict: "safe" },
  {
    client: "mobile",
    operation: "OrderTracking",
    fields: "7",
    verdict: "breaking",
  },
  {
    client: "partner",
    operation: "BillingExport",
    fields: "19",
    verdict: "safe",
  },
  {
    client: "agent",
    operation: "ShippingLookup",
    fields: "5",
    verdict: "risky",
  },
];

const VERDICT_COLOR: Record<Verdict, string> = {
  safe: TERM.ok,
  risky: TERM.warn,
  breaking: TERM.err,
};

const SCAN_AT = 30;
const SCAN_STEP = 9;
const SUMMARY_AT = SCAN_AT + ROWS.length * SCAN_STEP + 2;

const CSS = `
${appear("ttr-change", 6)}
${appear("ttr-compose", 14)}
${appear("ttr-head", 24)}
${ROWS.map((_, i) => scan(`ttr-x${i}`, SCAN_AT + i * SCAN_STEP)).join("\n")}
${ROWS.map((_, i) => appear(`ttr-v${i}`, SCAN_AT + i * SCAN_STEP + 4)).join("\n")}
${appear("ttr-sum", SUMMARY_AT)}
`;

export function RegistryTable() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <Pane
      title="nitro schema check --against registry"
      meta="4 clients"
      run={run}
    >
      <style>{CSS}</style>

      <div className="ttr-change">
        <span style={{ color: TERM.dim }}>{"change   "}</span>
        <span>{"Ordering.deliveryEstimate removed"}</span>
      </div>

      <div className="ttr-compose">
        <span style={{ color: TERM.dim }}>{"compose  "}</span>
        <span style={{ color: TERM.ok }}>{"pass"}</span>
        <span style={{ color: TERM.dim }}>
          {" · source schemas still agree"}
        </span>
      </div>

      <div
        className="ttr-head"
        style={{
          borderBottom: `1px solid ${TERM.rule}`,
          color: TERM.dim,
          marginTop: "0.7em",
          paddingBottom: "0.2em",
        }}
      >
        {"CLIENT   PUBLISHED OPERATION   FIELDS  VERDICT"}
      </div>

      {ROWS.map((row, i) => (
        <div key={row.client} className={`ttr-x${i}`}>
          <span>{row.client.padEnd(9)}</span>
          <span>{row.operation.padEnd(21)}</span>
          <span style={{ color: TERM.dim }}>{row.fields.padStart(4)}</span>
          <span>{"    "}</span>
          <span
            className={`ttr-v${i}`}
            style={{ color: VERDICT_COLOR[row.verdict] }}
          >
            {row.verdict}
          </span>
        </div>
      ))}

      <div
        className="ttr-sum"
        style={{
          borderTop: `1px solid ${TERM.rule}`,
          marginTop: "0.7em",
          paddingTop: "0.4em",
        }}
      >
        <span style={{ color: TERM.err }}>{"1 breaking"}</span>
        <span style={{ color: TERM.dim }}>{" · 1 risky · "}</span>
        <span>{"merge blocked before deploy"}</span>
      </div>
    </Pane>
  );
}
