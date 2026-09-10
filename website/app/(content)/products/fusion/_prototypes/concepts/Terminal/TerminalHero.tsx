"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { Caret, Pane, Spinner } from "./Chrome";
import { SOURCES, SUBGRAPHS, TERM } from "./palette";

/**
 * Hero: a live session that composes the graph. The operator types
 * `fusion compose`, the five subgraph files and the two non-GraphQL sources
 * are read in one at a time with their specification directive, a spinner runs
 * while composition works, and the run ends with one composite schema printed
 * as a box-drawn tree and a fresh prompt.
 *
 * Rest state: the finished session, spinner line gone, composite schema
 * printed, caret parked at the prompt.
 */

const DUR = 13;

/** Prints an element from `at`% of the loop onwards. */
const appear = (name: string, at: number) => `
.${name} { animation: ${name} ${DUR}s linear infinite; }
@keyframes ${name} { 0%, ${at}% { opacity: 0; } ${at + 0.8}%, 100% { opacity: 1; } }`;

/** Prints an element only between `from`% and `to`% of the loop. */
const phase = (name: string, from: number, to: number) => `
.${name} { animation: ${name} ${DUR}s linear infinite; }
@keyframes ${name} {
  0%, ${from}% { opacity: 0; }
  ${from + 0.8}%, ${to}% { opacity: 1; }
  ${to + 0.8}%, 100% { opacity: 0; }
}`;

const FILES = [
  ...SUBGRAPHS.map((s) => ({
    key: s.file,
    file: s.file,
    tag: s.directive,
    tagColor: TERM.spec,
  })),
  ...SOURCES.map((s) => ({
    key: s.file,
    file: s.file,
    tag: s.kind,
    tagColor: TERM.dim,
  })),
];

const COMMAND = "fusion compose ./sources";

const TREE = [
  "┌─ composite schema ──────────────────┐",
  "│ Query.product      → Catalog        │",
  "│ Product.taxRate    → Billing        │",
  "│ Product.available  → Ordering       │",
  "│ Product.delivery   → Shipping       │",
  "│ Product.payment    → Payments       │",
  "└─────────────────────────────────────┘",
];

const FILE_AT = 16;
const FILE_STEP = 3.4;
const SPIN_FROM = FILE_AT + FILES.length * FILE_STEP;
const DONE_AT = SPIN_FROM + 8;

const CSS = `
.tth-type {
  animation: tth-type ${DUR}s steps(${COMMAND.length}, end) infinite;
}
@keyframes tth-type {
  0% { width: 0ch; }
  11%, 100% { width: ${COMMAND.length}ch; }
}
${appear("tth-read", 12)}
${FILES.map((_, i) => appear(`tth-f${i}`, FILE_AT + i * FILE_STEP)).join("\n")}
${phase("tth-spin", SPIN_FROM, DONE_AT)}
${appear("tth-done", DONE_AT + 1)}
${TREE.map((_, i) => appear(`tth-t${i}`, DONE_AT + 2.4 + i * 1.1)).join("\n")}
${appear("tth-prompt", DONE_AT + 2.4 + TREE.length * 1.1)}
`;

export function TerminalHero() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <Pane
      title="graph@fusion:~/graph"
      meta={`${SUBGRAPHS.length} subgraphs · ${SOURCES.length} sources`}
      run={run}
    >
      <style>{CSS}</style>

      <div>
        <span style={{ color: TERM.prompt }}>{"$ "}</span>
        <span
          className="tth-type inline-block overflow-hidden align-bottom"
          style={{ width: `${COMMAND.length}ch` }}
        >
          {COMMAND}
        </span>
      </div>

      <div className="tth-read" style={{ color: TERM.dim, marginTop: "0.5em" }}>
        reading source schemas
      </div>

      {FILES.map((f, i) => (
        <div key={f.key} className={`tth-f${i}`}>
          {"  "}
          <span>{f.file.padEnd(24)}</span>
          <span style={{ color: f.tagColor }}>{f.tag.padEnd(22)}</span>
          <span style={{ color: TERM.ok }}>ok</span>
        </div>
      ))}

      <div
        className="tth-spin"
        style={{ marginTop: "0.5em", opacity: 0, color: TERM.dim }}
      >
        <Spinner />
        {" composing composite schema"}
      </div>

      <div className="tth-done" style={{ marginTop: "0.5em" }}>
        <span style={{ color: TERM.ok }}>✔ </span>
        <span>composition succeeded</span>
        <span style={{ color: TERM.dim }}>{" · 0 conflicts"}</span>
      </div>

      <div style={{ color: TERM.dim, marginTop: "0.5em" }}>
        {TREE.map((line, i) => (
          <div key={line} className={`tth-t${i}`}>
            {line}
          </div>
        ))}
      </div>

      <div className="tth-prompt" style={{ marginTop: "0.5em" }}>
        <span style={{ color: TERM.prompt }}>{"$ "}</span>
        <Caret />
      </div>
    </Pane>
  );
}
