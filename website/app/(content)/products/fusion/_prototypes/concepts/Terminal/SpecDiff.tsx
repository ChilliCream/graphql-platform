"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { Pane } from "./Chrome";
import { SUBGRAPHS, TERM } from "./palette";

/**
 * "Both specifications, one gateway": a two-pane diff. The left file is
 * written to the GraphQL Federation specification, the right one to Apollo
 * Federation, and the directive at the top of each file is the only thing that
 * differs. Line by line both files are pulled into the one composite schema
 * printed underneath, where two sources that are not GraphQL servers at all -
 * an OpenAPI document and a gRPC definition - add their fields on the same
 * terms.
 *
 * Rest state: both files bright, the composite schema fully printed with the
 * origin of every field in the right-hand column.
 */

const DUR = 14;

const appear = (name: string, at: number) => `
.${name} { animation: ${name} ${DUR}s linear infinite; }
@keyframes ${name} { 0%, ${at}% { opacity: 0; } ${at + 0.8}%, 100% { opacity: 1; } }`;

/** Dims a source line until the moment composition takes it. */
const pull = (name: string, at: number) => `
.${name} { animation: ${name} ${DUR}s linear infinite; }
@keyframes ${name} {
  0%, ${at}% { color: ${TERM.faint}; }
  ${at + 0.8}%, 100% { color: ${TERM.text}; }
}`;

const [CATALOG, BILLING] = SUBGRAPHS;

interface SourceLine {
  readonly text: string;
  /** Percentage of the loop at which composition takes this line. */
  readonly at: number;
}

const LEFT: readonly SourceLine[] = [
  { text: "type Product", at: 16 },
  { text: '  @key(fields: "id")', at: 16 },
  { text: "{", at: 16 },
  { text: "  id: ID!", at: 20 },
  { text: "  name: String!", at: 26 },
  { text: "  price: Float!", at: 32 },
  { text: "}", at: 32 },
];

const RIGHT: readonly SourceLine[] = [
  { text: "type Product", at: 16 },
  { text: '  @key(fields: "id")', at: 16 },
  { text: "{", at: 16 },
  { text: "  id: ID!", at: 20 },
  { text: "  taxRate: Float!", at: 38 },
  { text: "}", at: 38 },
];

interface CompositeLine {
  readonly field: string;
  readonly from: string;
  readonly at: number;
}

const COMPOSITE: readonly CompositeLine[] = [
  { field: "  id: ID!", from: "catalog.ts", at: 21 },
  { field: "  name: String!", from: "catalog.ts", at: 27 },
  { field: "  price: Float!", from: "catalog.ts", at: 33 },
  { field: "  taxRate: Float!", from: "billing.java", at: 39 },
  { field: "  refund: RefundRef", from: "payments.openapi.yaml", at: 47 },
  { field: "  stock: Int!", from: "inventory.proto", at: 55 },
];

const CSS = `
${LEFT.map((l, i) => pull(`ttd-l${i}`, l.at)).join("\n")}
${RIGHT.map((l, i) => pull(`ttd-r${i}`, l.at)).join("\n")}
${appear("ttd-open", 14)}
${COMPOSITE.map((l, i) => appear(`ttd-c${i}`, l.at)).join("\n")}
${appear("ttd-close", 60)}
${appear("ttd-note", 64)}
`;

interface FileColumnProps {
  readonly file: string;
  readonly directive: string;
  readonly lines: readonly SourceLine[];
  readonly prefix: string;
}

function FileColumn({ file, directive, lines, prefix }: FileColumnProps) {
  return (
    <div style={{ flex: "1 1 0", minWidth: 0, overflow: "hidden" }}>
      <div style={{ color: TERM.dim }}>{file}</div>
      <div style={{ color: TERM.spec }}>{directive}</div>
      <div style={{ color: TERM.rule }}>{"─".repeat(22)}</div>
      {lines.map((line, i) => (
        <div key={line.text} className={`${prefix}${i}`}>
          {line.text}
        </div>
      ))}
    </div>
  );
}

export function SpecDiff() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <Pane title="fusion compose --explain Product" meta="2 specs" run={run}>
      <style>{CSS}</style>

      <div className="flex gap-[2ch]">
        <FileColumn
          file={CATALOG.file}
          directive={CATALOG.directive}
          lines={LEFT}
          prefix="ttd-l"
        />
        <FileColumn
          file={BILLING.file}
          directive={BILLING.directive}
          lines={RIGHT}
          prefix="ttd-r"
        />
      </div>

      <div style={{ color: TERM.rule, marginTop: "0.6em" }}>
        {"───────────────── compose ─────────────────"}
      </div>

      <div className="ttd-open" style={{ marginTop: "0.4em" }}>
        <span style={{ color: TERM.dim }}>{"composite schema  "}</span>
        <span>{"type Product {"}</span>
      </div>

      {COMPOSITE.map((line, i) => (
        <div key={line.field} className={`ttd-c${i}`}>
          <span>{line.field.padEnd(22)}</span>
          <span style={{ color: TERM.dim }}>{line.from}</span>
        </div>
      ))}

      <div className="ttd-close">{"}"}</div>

      <div className="ttd-note" style={{ color: TERM.ok, marginTop: "0.4em" }}>
        {"✔ both specifications composed · one schema"}
      </div>
    </Pane>
  );
}
