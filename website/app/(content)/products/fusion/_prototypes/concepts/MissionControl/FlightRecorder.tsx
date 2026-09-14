"use client";

import { TYPE } from "../../brand";
import { anim, useCycle, useSceneMotion } from "./hooks";
import { MC } from "./palette";

/**
 * "Composition protects the graph, Nitro protects your clients": the flight
 * recorder. Composition stamps the change green, then the recorder replays the
 * operations real registered clients run against it and marks each one safe,
 * risky or breaking before the change is merged.
 */

const W = 640;
const H = 440;
/** The scene box mirrors the viewBox, so the recorder never letterboxes. */
export const FLIGHT_RECORDER_RATIO = `${W} / ${H}`;
/** 0 arms the tape; the last phase is the rest frame: every replay classified. */
const PHASES = 7;
const REST = PHASES - 1;
const BEAT = 1400;

const DECK = { x: 16, y: 92, w: W - 32, h: 268 } as const;
const ROW_H = 52;

type Verdict = "SAFE" | "RISKY" | "BREAKING";

interface Replay {
  readonly client: string;
  readonly operation: string;
  readonly detail: string;
  readonly verdict: Verdict;
}

const REPLAYS: readonly Replay[] = [
  {
    client: "web 3.8",
    operation: "productCard { name price }",
    detail: "18.4k calls / 24h",
    verdict: "SAFE",
  },
  {
    client: "partner-api 2.1",
    operation: "orderList { total delivery }",
    detail: "2.1k calls / 24h",
    verdict: "SAFE",
  },
  {
    client: "web 3.7",
    operation: "product { legacySku }",
    detail: "deprecated field, 340 calls / 24h",
    verdict: "RISKY",
  },
  {
    client: "mobile 4.2",
    operation: "productCard { name rating }",
    detail: "1.2k calls / 24h, no rollout to replace it",
    verdict: "BREAKING",
  },
];

const VERDICT_COLOR: Record<Verdict, string> = {
  SAFE: MC.phosphor,
  RISKY: MC.amber,
  BREAKING: MC.alert,
};

const KEYFRAMES = `
@keyframes mc-rec-reel { to { transform: rotate(360deg); } }
@keyframes mc-rec-tape { from { stroke-dashoffset: 0; } to { stroke-dashoffset: -32; } }
@keyframes mc-rec-blink { 0%, 100% { opacity: 1; } 50% { opacity: 0.3; } }
`;

interface ReelProps {
  readonly cx: number;
  readonly running: boolean;
  readonly delay: number;
}

function Reel({ cx, running, delay }: ReelProps) {
  return (
    <g
      style={{
        transformBox: "view-box",
        transformOrigin: `${cx}px 56px`,
        animation: anim(
          running,
          `mc-rec-reel 5200ms linear ${delay}ms infinite`,
        ),
      }}
    >
      <circle cx={cx} cy={56} r="24" fill="none" stroke={MC.line} />
      <circle cx={cx} cy={56} r="7" fill="none" stroke={MC.dim} />
      <path
        d={`M${cx - 24} 56H${cx + 24}M${cx} 32V80`}
        stroke={MC.line}
        strokeOpacity="0.7"
      />
    </g>
  );
}

export function FlightRecorder() {
  const running = useSceneMotion();
  const phase = useCycle(running, PHASES, BEAT, REST);
  const classified = Math.max(0, Math.min(phase - 1, REPLAYS.length));
  const blocked = classified >= REPLAYS.length;

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="h-full w-full">
      <style>{KEYFRAMES}</style>
      <rect width={W} height={H} fill={MC.bg} />

      <Reel cx={56} running={running} delay={0} />
      <Reel cx={W - 56} running={running} delay={260} />
      <path
        d={`M80 56H${W - 80}`}
        stroke={MC.signal}
        strokeOpacity="0.5"
        strokeDasharray="10 6"
        style={{
          animation: anim(running, "mc-rec-tape 1400ms linear infinite"),
        }}
      />
      <text
        x={W / 2}
        y={30}
        fill={MC.ink}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        SCHEMA CHANGE · REMOVE Product.rating
      </text>
      <text
        x={W / 2}
        y={78}
        fill={MC.phosphor}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        COMPOSITION: GREEN · SOURCE SCHEMAS STILL COMPOSE
      </text>

      <rect
        x={DECK.x}
        y={DECK.y}
        width={DECK.w}
        height={DECK.h}
        rx="9"
        fill={MC.panel}
        stroke={MC.panelEdge}
      />
      <text
        x={DECK.x + 16}
        y={DECK.y + 24}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.16em"
      >
        NITRO REPLAY · OPERATIONS PUBLISHED BY REGISTERED CLIENTS
      </text>

      {REPLAYS.map((replay, i) => {
        const y = DECK.y + 52 + i * ROW_H;
        const done = i < classified;
        const color = done ? VERDICT_COLOR[replay.verdict] : MC.dim;
        return (
          <g key={replay.client}>
            <path
              d={`M${DECK.x + 16} ${y + 26}H${DECK.x + DECK.w - 16}`}
              stroke={MC.panelEdge}
              strokeOpacity="0.6"
            />
            <circle
              cx={DECK.x + 26}
              cy={y - 4}
              r="4"
              fill={color}
              style={{ transition: "fill 400ms ease" }}
            />
            <text
              x={DECK.x + 40}
              y={y}
              fill={done ? MC.ink : MC.dim}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
              style={{ transition: "fill 400ms ease" }}
            >
              {`${replay.client}  ${replay.operation}`}
            </text>
            <text
              x={DECK.x + 40}
              y={y + 15}
              fill={MC.dim}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
              letterSpacing="0.1em"
            >
              {replay.detail}
            </text>
            <text
              x={DECK.x + DECK.w - 16}
              y={y + 4}
              fill={color}
              fontFamily={MC.mono}
              fontSize={TYPE.label}
              letterSpacing="0.16em"
              textAnchor="end"
              style={{
                transition: "fill 400ms ease",
                animation: anim(
                  running && done && replay.verdict === "BREAKING",
                  "mc-rec-blink 1200ms steps(1, end) infinite",
                ),
              }}
            >
              {done ? replay.verdict : "REPLAYING"}
            </text>
          </g>
        );
      })}

      <rect
        x={DECK.x}
        y={H - 56}
        width={DECK.w}
        height="40"
        rx="8"
        fill={MC.panel}
        stroke={blocked ? MC.alert : MC.panelEdge}
        strokeOpacity={blocked ? 0.7 : 1}
        style={{ transition: "stroke 400ms ease" }}
      />
      <text
        x={W / 2}
        y={H - 31}
        fill={blocked ? MC.alert : MC.dim}
        fontFamily={MC.mono}
        fontSize={TYPE.label}
        letterSpacing="0.18em"
        textAnchor="middle"
        style={{ transition: "fill 400ms ease" }}
      >
        {blocked
          ? "1 BREAKING · 1 RISKY · FLAGGED BEFORE THE MERGE"
          : "REPLAYING REAL CLIENT OPERATIONS"}
      </text>
    </svg>
  );
}
