"use client";

import { useRef } from "react";

import { TYPE } from "../../../../brand";
import { anim, useCycle, useElementMotion } from "../../hooks";
import { MC } from "../../palette";
import {
  PHASE_LABELS,
  PLAN_STEPS,
  TIMING,
  TOTAL_MS,
  TRACE_CLIENTS,
} from "./data";
import type { TraceSpec } from "./data";
import { Chip, ClientTabs, Plate, Rail } from "./parts";

/**
 * Hero visual (prototype v13): the query plan trace.
 *
 * Three console plates read left to right. The request plate types the
 * operation the selected client sends - the four client tabs above the plates
 * decide which one. The middle plate is the gateway, labelled Fusion: it holds
 * the plan Fusion built for that operation, one step per subgraph with the
 * subgraph's language and federation specification, and the steps light up in
 * execution order - the root fetch, then the three that run in parallel, then
 * the follow-up. The response plate receives one JSON fragment per subgraph as
 * its step finishes and merges them, line by line, into the single response
 * document with its timing bar.
 *
 * Everything is DOM text at its declared px size, so the 11px label floor holds
 * at any viewport: nothing is scaled to fit, the narrow layout drops panels,
 * headings and punctuation instead of shrinking the lettering. Below lg the
 * request plate is gone, the three phase headings are gone (the shared rail
 * spine still shows the fan-out), the plan rows put the subgraph and its two
 * chips on one wrapping line, and the response plate is the five merged lines -
 * still revealed one per phase, so the merge stays animated - plus the timing
 * bar, without the two brace lines. Row content width at 375 is 274px (320 plate
 * - 2 border - 24 px-3 - 20 rail), and the shortest row is wider than that, so
 * the chips do wrap to a second line under every row. Budget at 375x667 with the
 * 1.25 line height this column sets below lg: pt 12 + tabs ~53.5 + 12 + gateway
 * plate ~316 (34.5 header + 24 padding + 8 group gaps + rows 3x43.25 + 2x59 with
 * their source line + 2 border) + 12 + response plate ~147.5 (24 padding +
 * merged box 5x13.75 + 14 + mt-2 8 + timing 30.75 + 2 border) = ~553 against the
 * 587 the hero's min-h-[88svh] gives, so the whole merged document and the
 * timing bar sit inside the band.
 */

/** Steps 0-3 type the query, 4-6 execute the plan, 7-8 merge, 9 rests. */
const STEPS = 10;
const STEP_MS = 820;
const REST_STEP = STEPS - 1;
const TYPING_STEPS = 4;
/** Phase index once every plan step has returned. */
const DONE_PHASE = 3;
/** First step on which the merged response document is complete. */
const MERGE_STEP = TYPING_STEPS + 4;

const KEYFRAMES = `
@keyframes mc-qpt-caret { 0%, 49% { opacity: 1; } 50%, 100% { opacity: 0.1; } }
@keyframes mc-qpt-flow {
  0% { transform: translateX(0); opacity: 0; }
  25% { opacity: 1; }
  100% { transform: translateX(11px); opacity: 0; }
}
`;

const GRID = `linear-gradient(to right, ${MC.grid} 1px, transparent 1px), linear-gradient(to bottom, ${MC.grid} 1px, transparent 1px)`;
const GLOW = `radial-gradient(62% 62% at 68% 50%, color-mix(in srgb, ${MC.phosphor} 12%, transparent) 0%, transparent 72%)`;

const specTone = (spec: TraceSpec) =>
  spec === "Apollo Federation" ? MC.signal : MC.phosphor;

/** Phase the plan is on: -1 while the query is still being typed. */
const phaseOf = (step: number) =>
  step < TYPING_STEPS ? -1 : Math.min(step - TYPING_STEPS, DONE_PHASE);

/** Plan steps in execution order, grouped into the phases that share a rail. */
const PHASES = PHASE_LABELS.map((label, phase) => ({
  label,
  phase,
  steps: PLAN_STEPS.filter((planStep) => planStep.phase === phase),
}));

export default function QueryPlanTrace() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);
  const step = useCycle(running, STEPS, STEP_MS, REST_STEP);
  const clientIndex = useCycle(
    running,
    TRACE_CLIENTS.length,
    STEPS * STEP_MS,
    0,
  );

  const client = TRACE_CLIENTS[clientIndex];
  const phase = phaseOf(step);
  const merged = step >= MERGE_STEP;
  const typed =
    step >= TYPING_STEPS
      ? client.query.length
      : Math.max(
          2,
          Math.round(((step + 1) / TYPING_STEPS) * client.query.length),
        );

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <style>{KEYFRAMES}</style>
      <div className="absolute inset-0" style={{ background: MC.bg }} />
      <div
        className="absolute inset-0"
        style={{ backgroundImage: GRID, backgroundSize: "48px 48px" }}
      />
      <div className="absolute inset-0" style={{ background: GLOW }} />

      <div className="absolute inset-0 flex items-start justify-center overflow-hidden px-4 pt-3 lg:items-center lg:justify-end lg:pt-0 lg:pr-8 xl:pr-14">
        <div className="flex flex-col gap-3 leading-tight lg:leading-[1.6]">
          <ClientTabs activeIndex={clientIndex} />

          <div className="flex flex-col gap-3 lg:flex-row lg:items-stretch">
            <Plate
              title="Request"
              caption={client.label}
              accent={MC.signal}
              className="hidden w-[330px] xl:flex"
            >
              <div className="flex flex-col gap-0.5">
                {client.query.slice(0, typed).map((line, i) => (
                  <span
                    key={`${clientIndex}-${i}`}
                    style={{
                      color: i === 0 ? MC.signal : MC.ink,
                      fontFamily: MC.mono,
                      fontSize: TYPE.caption,
                      whiteSpace: "pre",
                    }}
                  >
                    {line}
                    {i === typed - 1 && (
                      <span
                        className="ml-px inline-block h-[13px] w-[7px] translate-y-[2px]"
                        style={{
                          background: MC.phosphor,
                          animation: anim(
                            running,
                            "mc-qpt-caret 1000ms steps(1) infinite",
                          ),
                        }}
                      />
                    )}
                  </span>
                ))}
              </div>
            </Plate>

            <Plate
              title="Fusion"
              caption="Gateway query plan"
              accent={MC.phosphor}
              className="w-[320px] sm:w-[344px] lg:w-[364px]"
            >
              <div className="flex flex-col gap-1">
                {PHASES.map((group) => (
                  <div key={group.label} className="flex flex-col">
                    <span
                      className="hidden pl-5 lg:block"
                      style={{
                        color: phase >= group.phase ? MC.phosphor : MC.dim,
                        fontFamily: MC.mono,
                        fontSize: TYPE.label,
                        letterSpacing: "0.18em",
                        transition: "color 400ms",
                      }}
                    >
                      {group.steps.length > 1
                        ? `${group.label} · ${group.steps.length} IN PARALLEL`
                        : group.label}
                    </span>

                    {group.steps.map((planStep, i) => {
                      const lit = phase >= group.phase;
                      const active = phase === group.phase;
                      return (
                        <div
                          key={planStep.subgraph}
                          className="flex items-stretch"
                          style={{
                            opacity: lit ? 1 : 0.45,
                            transition: "opacity 400ms",
                          }}
                        >
                          <Rail
                            lit={lit}
                            active={active}
                            spineAbove
                            spineBelow={i < group.steps.length - 1}
                            running={running}
                          />
                          <div className="flex flex-col gap-0.5 py-1">
                            <div className="flex flex-wrap items-center gap-x-2 gap-y-0.5 lg:flex-col lg:items-start">
                              <span
                                style={{
                                  color: lit ? MC.ink : MC.dim,
                                  fontFamily: MC.mono,
                                  fontSize: TYPE.caption,
                                  transition: "color 400ms",
                                }}
                              >
                                {`${planStep.subgraph} · ${planStep.selection}`}
                              </span>
                              <span className="flex flex-wrap gap-1">
                                <Chip
                                  label={planStep.language}
                                  tone={MC.amber}
                                  lit={lit}
                                />
                                <Chip
                                  label={planStep.spec}
                                  tone={specTone(planStep.spec)}
                                  lit={lit}
                                />
                              </span>
                            </div>
                            {planStep.source && (
                              <span
                                style={{
                                  color: MC.dim,
                                  fontFamily: MC.mono,
                                  fontSize: TYPE.label,
                                  letterSpacing: "0.1em",
                                }}
                              >
                                {`└ ${planStep.source.name} · ${planStep.source.kind} · ${planStep.source.language}`}
                              </span>
                            )}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                ))}
              </div>
            </Plate>

            <Plate
              title="Response"
              caption={merged ? "1 document" : `${PLAN_STEPS.length} fragments`}
              accent={merged ? MC.phosphor : MC.dim}
              className="w-[320px] sm:w-[344px] lg:w-[352px]"
              headerClassName="hidden lg:flex"
            >
              <div className="hidden flex-col lg:flex">
                {PLAN_STEPS.map((planStep, i) => {
                  const back = phase > planStep.phase;
                  return (
                    <div
                      key={planStep.subgraph}
                      className="flex items-stretch"
                      style={{
                        opacity: back ? 1 : 0.35,
                        transform: back ? "none" : "translateX(-8px)",
                        transition: "opacity 400ms, transform 400ms",
                      }}
                    >
                      <Rail
                        lit={back}
                        active={phase === planStep.phase}
                        spineAbove={i > 0}
                        spineBelow
                        running={running}
                      />
                      <span
                        className="truncate py-[3px]"
                        style={{
                          color: back ? MC.ink : MC.dim,
                          fontFamily: MC.mono,
                          fontSize: TYPE.label,
                          transition: "color 400ms",
                        }}
                      >
                        {planStep.fragment}
                      </span>
                    </div>
                  );
                })}
              </div>

              <div
                className="mt-2 rounded-md border px-2 py-1.5"
                style={{
                  borderColor: merged
                    ? `color-mix(in srgb, ${MC.phosphor} 55%, transparent)`
                    : MC.panelEdge,
                  background: `color-mix(in srgb, ${MC.phosphor} ${merged ? 8 : 0}%, transparent)`,
                  transition: "border-color 400ms, background 400ms",
                }}
              >
                <span
                  className="hidden lg:block"
                  style={{
                    color: MC.dim,
                    fontFamily: MC.mono,
                    fontSize: TYPE.label,
                    whiteSpace: "pre",
                  }}
                >
                  {'{ "data": { "product": {'}
                </span>
                {PLAN_STEPS.map((planStep) => (
                  <span
                    key={planStep.subgraph}
                    className="block truncate"
                    style={{
                      color: MC.ink,
                      fontFamily: MC.mono,
                      fontSize: TYPE.label,
                      whiteSpace: "pre",
                      opacity: phase > planStep.phase ? 1 : 0,
                      transition: "opacity 400ms",
                    }}
                  >
                    {planStep.merged}
                  </span>
                ))}
                <span
                  className="hidden lg:block"
                  style={{
                    color: MC.dim,
                    fontFamily: MC.mono,
                    fontSize: TYPE.label,
                    whiteSpace: "pre",
                  }}
                >
                  {"} } }"}
                </span>
              </div>

              <div className="mt-2 flex items-end gap-px">
                {TIMING.map((segment) => {
                  const filled =
                    segment.phase === DONE_PHASE
                      ? merged
                      : phase > segment.phase;
                  return (
                    <div
                      key={segment.label}
                      className="flex flex-col gap-1"
                      style={{ flexGrow: segment.ms, flexBasis: 0 }}
                    >
                      <span
                        className="truncate"
                        style={{
                          color: filled ? MC.phosphor : MC.dim,
                          fontFamily: MC.mono,
                          fontSize: TYPE.label,
                          letterSpacing: "0.08em",
                          transition: "color 400ms",
                        }}
                      >
                        {segment.label}
                      </span>
                      <span
                        className="h-[5px] rounded-[2px]"
                        style={{
                          background: filled
                            ? MC.phosphor
                            : `color-mix(in srgb, ${MC.line} 70%, transparent)`,
                          transition: "background 400ms",
                        }}
                      />
                    </div>
                  );
                })}
                <span
                  className="pl-2"
                  style={{
                    color: MC.ink,
                    fontFamily: MC.mono,
                    fontSize: TYPE.label,
                    letterSpacing: "0.1em",
                  }}
                >
                  {`${TOTAL_MS} ms`}
                </span>
              </div>
            </Plate>
          </div>
        </div>
      </div>
    </div>
  );
}
