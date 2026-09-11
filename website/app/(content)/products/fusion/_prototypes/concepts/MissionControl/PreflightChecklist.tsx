"use client";

import { TYPE } from "../../brand";
import { anim, useCycle, useSceneMotion } from "./hooks";
import { MC, STATIONS } from "./palette";

/**
 * "Any GraphQL server, no plugin": the pre-flight checklist. The roster shows
 * stock GraphQL servers in five languages with no runtime package bolted on,
 * and the checklist runs the one build step you add - composition - until a
 * type conflict turns a line red and aborts the countdown before deployment.
 */

const W = 640;
const H = 460;
/** The scene box mirrors the viewBox, so the checklist never letterboxes. */
export const PREFLIGHT_CHECKLIST_RATIO = `${W} / ${H}`;
/** 0 arms the run; the last phase is the rest frame: the aborted build. */
const PHASES = 7;
const REST = PHASES - 1;
const BEAT = 1300;

const ROSTER = { x: 16, y: 44, w: 250 } as const;
const LIST = { x: 292, y: 44, w: 332 } as const;
/** The conflict line: after it the countdown holds and the build stops. */
const CONFLICT = 2;

const CHECKS = [
  "SOURCE SCHEMAS READ",
  "KEYS AND LOOKUPS DECLARED",
  "TYPE COMPATIBILITY",
  "ENUM COMPATIBILITY",
  "COMPOSITE SCHEMA SIGNED",
] as const;

const KEYFRAMES = `
@keyframes mc-preflight-alarm { 0%, 100% { opacity: 1; } 50% { opacity: 0.4; } }
@keyframes mc-preflight-scan { 0%, 100% { opacity: 0.25; } 50% { opacity: 0.9; } }
`;

type CheckState = "pending" | "ok" | "failed" | "stopped";

function checkState(index: number, phase: number): CheckState {
  if (phase === 0) return "pending";
  const reached = phase - 1;
  if (index < CONFLICT) return index <= reached ? "ok" : "pending";
  if (index === CONFLICT) return reached >= CONFLICT ? "failed" : "pending";
  return reached >= CONFLICT ? "stopped" : "pending";
}

const STATE_COLOR: Record<CheckState, string> = {
  pending: MC.dim,
  ok: MC.phosphor,
  failed: MC.alert,
  stopped: MC.dim,
};

const STATE_LABEL: Record<CheckState, string> = {
  pending: "· · ·",
  ok: "OK",
  failed: "CONFLICT",
  stopped: "NOT RUN",
};

export function PreflightChecklist() {
  const running = useSceneMotion();
  const phase = useCycle(running, PHASES, BEAT, REST);
  const aborted = phase - 1 >= CONFLICT;
  const countdown = Math.max(5 - Math.min(phase, CONFLICT + 1), 0);

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={MC.bg} />

      <text
        x={ROSTER.x}
        y={26}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.2em"
      >
        STATION ROSTER
      </text>
      <rect
        x={ROSTER.x}
        y={ROSTER.y}
        width={ROSTER.w}
        height={332}
        rx="9"
        fill={MC.panel}
        stroke={MC.panelEdge}
      />
      <text
        x={ROSTER.x + 10}
        y={ROSTER.y + 24}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
      >
        SUBGRAPH · LANGUAGE
      </text>
      <text
        x={ROSTER.x + ROSTER.w - 10}
        y={ROSTER.y + 24}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        textAnchor="end"
      >
        RUNTIME PLUGIN
      </text>
      {STATIONS.map((station, i) => {
        const y = ROSTER.y + 56 + i * 54;
        return (
          <g key={station.name}>
            <text
              x={ROSTER.x + 14}
              y={y}
              fill={MC.ink}
              fontFamily={MC.mono}
              fontSize={TYPE.caption}
              letterSpacing="0.12em"
            >
              {station.name.toUpperCase()}
            </text>
            <text
              x={ROSTER.x + 14}
              y={y + 16}
              fill={MC.dim}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
              letterSpacing="0.06em"
            >
              {`${station.language} · STOCK GRAPHQL SERVER`}
            </text>
            <text
              x={ROSTER.x + ROSTER.w - 14}
              y={y}
              fill={MC.phosphor}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
              letterSpacing="0.14em"
              textAnchor="end"
            >
              NONE
            </text>
          </g>
        );
      })}

      <text
        x={LIST.x}
        y={26}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.2em"
      >
        COMPOSITION PRE-FLIGHT
      </text>
      <rect
        x={LIST.x}
        y={LIST.y}
        width={LIST.w}
        height={332}
        rx="9"
        fill={MC.panel}
        stroke={aborted ? MC.alert : MC.panelEdge}
        strokeOpacity={aborted ? 0.7 : 1}
        style={{ transition: "stroke 400ms ease" }}
      />
      <text
        x={LIST.x + 16}
        y={LIST.y + 32}
        fill={aborted ? MC.alert : MC.phosphor}
        fontFamily={MC.mono}
        fontSize={TYPE.h5}
        letterSpacing="0.14em"
        style={{
          transition: "fill 400ms ease",
          animation: anim(
            running && aborted,
            "mc-preflight-alarm 1100ms steps(1, end) infinite",
          ),
        }}
      >
        {`T-0${countdown}`}
      </text>
      <text
        x={LIST.x + LIST.w - 16}
        y={LIST.y + 32}
        fill={aborted ? MC.alert : MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.16em"
        textAnchor="end"
        style={{ transition: "fill 400ms ease" }}
      >
        {aborted ? "COUNTDOWN HELD" : "BUILD STEP RUNNING"}
      </text>

      {CHECKS.map((check, i) => {
        const state = checkState(i, phase);
        const y = LIST.y + 74 + i * 40;
        return (
          <g key={check}>
            <text
              x={LIST.x + 16}
              y={y}
              fill={state === "pending" ? MC.dim : MC.ink}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
              letterSpacing="0.1em"
              style={{ transition: "fill 400ms ease" }}
            >
              {check}
            </text>
            <text
              x={LIST.x + LIST.w - 16}
              y={y}
              fill={STATE_COLOR[state]}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
              letterSpacing="0.14em"
              textAnchor="end"
              style={{ transition: "fill 400ms ease" }}
            >
              {STATE_LABEL[state]}
            </text>
            <path
              d={`M${LIST.x + 16} ${y + 10}H${LIST.x + LIST.w - 16}`}
              stroke={state === "failed" ? MC.alert : MC.panelEdge}
              strokeOpacity={state === "failed" ? 0.6 : 0.7}
              style={{
                transition: "stroke 400ms ease",
                animation: anim(
                  running && state === "pending",
                  `mc-preflight-scan 2000ms ease-in-out ${i * 160}ms infinite`,
                ),
              }}
            />
          </g>
        );
      })}

      <text
        x={LIST.x + LIST.w / 2}
        y={LIST.y + 292}
        fill={aborted ? MC.alert : MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        textAnchor="middle"
        style={{ transition: "fill 400ms ease" }}
      >
        {aborted
          ? "Product.price: Float (Catalog) vs String (Billing)"
          : "validating source schemas against one another"}
      </text>

      <rect
        x={16}
        y={H - 52}
        width={W - 32}
        height="36"
        rx="8"
        fill={MC.panel}
        stroke={aborted ? MC.alert : MC.phosphor}
        strokeOpacity="0.55"
        style={{ transition: "stroke 400ms ease" }}
      />
      <text
        x={W / 2}
        y={H - 29}
        fill={aborted ? MC.alert : MC.phosphor}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.2em"
        textAnchor="middle"
        style={{ transition: "fill 400ms ease" }}
      >
        {aborted
          ? "COMPOSITION FAILED · PIPELINE STOPPED · NOTHING DEPLOYED"
          : "COMPOSITION RUNNING · IN THE BUILD, NOT AT RUNTIME"}
      </text>
    </svg>
  );
}
