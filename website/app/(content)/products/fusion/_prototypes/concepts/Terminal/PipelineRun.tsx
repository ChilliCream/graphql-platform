"use client";

import { useReducedMotionPreference, useSceneActive } from "../../Primitives";
import { Pane, Spinner } from "./Chrome";
import { SUBGRAPHS, TERM } from "./palette";

/**
 * "Any GraphQL server, no plugin": the composition step as CI output. Every
 * subgraph is an ordinary GraphQL server in its own language - the extension
 * is the only thing that changes and nothing is installed next to it - and the
 * one build step is composition, which walks the source schemas, hits a type
 * conflict between two of them and exits non-zero. The pipeline stops; the
 * running gateway is never touched.
 *
 * Rest state: the finished failing run, both errors printed and the non-zero
 * exit line at the bottom.
 */

const DUR = 13;

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

/** The subgraph whose source schema disagrees with `catalog.ts`. */
const CONFLICTING = "accounts.cs";

const START_AT = 12;
const STEP = 7;
const FAIL_AT = START_AT + SUBGRAPHS.length * STEP;

const ERRORS = [
  { line: "E_TYPE_CONFLICT   Product.price", tone: TERM.err },
  { line: "    catalog.ts     price: Float!", tone: TERM.dim },
  { line: "    accounts.cs    price: String!", tone: TERM.dim },
  { line: "E_FIELD_MISSING   Order.total not resolvable", tone: TERM.err },
];

const CSS = `
${appear("ttp-head", 6)}
${SUBGRAPHS.map((_, i) => appear(`ttp-s${i}`, START_AT + i * STEP)).join("\n")}
${SUBGRAPHS.map((_, i) => phase(`ttp-w${i}`, START_AT + i * STEP + 0.5, START_AT + (i + 1) * STEP - 2)).join("\n")}
${SUBGRAPHS.map((_, i) => appear(`ttp-m${i}`, START_AT + (i + 1) * STEP - 1.5)).join("\n")}
${appear("ttp-fail", FAIL_AT + 1)}
${ERRORS.map((_, i) => appear(`ttp-e${i}`, FAIL_AT + 3 + i * 2)).join("\n")}
.ttp-exit { animation: ttp-exit ${DUR}s linear infinite; }
@keyframes ttp-exit {
  0%, ${FAIL_AT + 12}% { opacity: 0; }
  ${FAIL_AT + 12.6}%, ${FAIL_AT + 15}% { opacity: 1; }
  ${FAIL_AT + 16}% { opacity: 0.25; }
  ${FAIL_AT + 17}%, 100% { opacity: 1; }
}
`;

export function PipelineRun() {
  const active = useSceneActive();
  const reduced = useReducedMotionPreference();
  const run = active && !reduced;

  return (
    <Pane title="ci: fusion compose ./sources" meta="build step" run={run}>
      <style>{CSS}</style>

      <div className="ttp-head" style={{ color: TERM.dim }}>
        {"checking 5 source schemas · 0 gateway packages installed"}
      </div>

      <div style={{ marginTop: "0.5em" }}>
        {SUBGRAPHS.map((s, i) => {
          const bad = s.file === CONFLICTING;
          return (
            <div key={s.file} className={`ttp-s${i}`}>
              <span className={`ttp-w${i}`} style={{ opacity: 0 }}>
                <Spinner />
              </span>
              <span
                className={`ttp-m${i}`}
                style={{ color: bad ? TERM.err : TERM.ok }}
              >
                {bad ? "✖" : "✔"}
              </span>
              <span>{` ${s.file.padEnd(14)}`}</span>
              <span style={{ color: TERM.dim }}>{s.language.padEnd(8)}</span>
              <span style={{ color: bad ? TERM.err : TERM.dim }}>
                {bad ? "conflicts with catalog.ts" : "ordinary graphql server"}
              </span>
            </div>
          );
        })}
      </div>

      <div className="ttp-fail" style={{ marginTop: "0.5em" }}>
        <span style={{ color: TERM.err }}>{"✖ composition failed"}</span>
        <span style={{ color: TERM.dim }}>{" · 2 errors"}</span>
      </div>

      {ERRORS.map((e, i) => (
        <div key={e.line} className={`ttp-e${i}`} style={{ color: e.tone }}>
          {`  ${e.line}`}
        </div>
      ))}

      <div className="ttp-exit" style={{ color: TERM.err, marginTop: "0.5em" }}>
        {"exit 1 · pipeline stopped · gateway untouched"}
      </div>
    </Pane>
  );
}
