"use client";

import { useRef } from "react";

import { TYPE, svgLabelGap, svgLabelSize } from "../tokens";
import {
  anim,
  useCycle,
  useNarrowViewport,
  useSceneMotion,
  useSvgLabelScale,
} from "./hooks";
import { dotLines, wrapWords } from "./lines";
import { useSceneRatio } from "./Scene";
import { MC } from "../palette";

/**
 * Animated console that replays registered clients' operations against a
 * composed schema change and marks each one safe, risky or breaking.
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

/**
 * Below 1024px the deck re-flows to a single narrow column: the schema/
 * composition banners split at their word boundaries instead of pinning to
 * their desktop footprint, the reel/tape header art drops out (no room),
 * and each replay row grows to as many lines as its client, operation and
 * detail text need. `MOBILE_W` matches this panel's measured rendered width
 * at 375px; the row geometry below is computed once, from the (static)
 * replay copy, so `MOBILE_H` always matches what actually renders.
 */
const MOBILE_W = 333;
const MOBILE_INSET = 16;
const MOBILE_LINE = 16;

const MOBILE_HEADER_Y1 = 26;
const MOBILE_HEADER_Y2 = 54;
const MOBILE_DECK_Y = 96;

const DECK_HEADER_TEXT =
  "NITRO REPLAY · OPERATIONS PUBLISHED BY REGISTERED CLIENTS";
/** The deck header's wrapped lines, computed once so the row cursor below
 * starts below however many lines it actually takes. */
const DECK_HEADER_LINES = wrapWords(DECK_HEADER_TEXT, 28);

interface RowLayout {
  readonly replay: Replay;
  readonly opLines: readonly string[];
  readonly detailLines: readonly string[];
  readonly nameY: number;
  readonly opY: number;
  readonly detailY: number;
  readonly dividerY: number;
  readonly nextY: number;
}

const MOBILE_ROWS: readonly RowLayout[] = (() => {
  const headerY0 = MOBILE_DECK_Y + 22;
  const headerLastY = headerY0 + (DECK_HEADER_LINES.length - 1) * MOBILE_LINE;
  let cursor = headerLastY + 30;
  return REPLAYS.map((replay) => {
    const opLines = wrapWords(replay.operation, 36);
    const detailLines = wrapWords(replay.detail, 32);
    const nameY = cursor;
    const opY = nameY + MOBILE_LINE;
    const detailY = opY + (opLines.length - 1) * MOBILE_LINE + MOBILE_LINE;
    const lastLineY = detailY + (detailLines.length - 1) * MOBILE_LINE;
    const dividerY = lastLineY + 14;
    const nextY = dividerY + 26;
    cursor = nextY;
    return {
      replay,
      opLines,
      detailLines,
      nameY,
      opY,
      detailY,
      dividerY,
      nextY,
    };
  });
})();

const MOBILE_DECK_BOTTOM =
  (MOBILE_ROWS[MOBILE_ROWS.length - 1]?.dividerY ?? MOBILE_DECK_Y) + 20;
const MOBILE_FOOTER_Y = MOBILE_DECK_BOTTOM + 40;
const MOBILE_FOOTER_H = 76;
const MOBILE_H = MOBILE_FOOTER_Y + MOBILE_FOOTER_H + 16;
const MOBILE_RATIO = `${MOBILE_W} / ${MOBILE_H}`;

interface StackedTextProps {
  readonly x: number;
  readonly y: number;
  readonly lines: readonly string[];
  readonly lineHeight: number;
  readonly fill: string;
  readonly fontSize: number;
  readonly letterSpacing?: string;
  readonly textAnchor?: "start" | "middle" | "end";
}

function StackedText({
  x,
  y,
  lines,
  lineHeight,
  fill,
  fontSize,
  letterSpacing,
  textAnchor,
}: StackedTextProps) {
  return (
    <text
      x={x}
      y={y}
      fill={fill}
      fontFamily={MC.mono}
      fontSize={fontSize}
      letterSpacing={letterSpacing}
      textAnchor={textAnchor}
    >
      {lines.map((line, i) => (
        <tspan key={i} x={x} dy={i === 0 ? 0 : lineHeight}>
          {line}
        </tspan>
      ))}
    </text>
  );
}

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
  const svgRef = useRef<SVGSVGElement>(null);
  const desktopScale = useSvgLabelScale(svgRef, W);
  const mobile = useNarrowViewport();
  const mobileScale = useSvgLabelScale(svgRef, MOBILE_W);
  const scale = mobile ? mobileScale : desktopScale;
  const label = svgLabelSize(TYPE.label, scale);
  /** Gap between a replay row's operation line and its detail line. */
  const rowGap = svgLabelGap(15, label, TYPE.label, 1.15);
  /**
   * A sidebar-narrow desktop slot boosts `label` here the same way a narrow
   * viewport does. The deck header's 55 characters fit this same `W`-wide
   * viewBox at `label`'s un-boosted size and above, but not once boosted
   * much past it (1024 renders at scale ~0.801) — past roughly this scale
   * it wraps onto two lines instead of clipping past its own trailing
   * character.
   */
  const crowded = !mobile && scale < 0.9;
  /**
   * The extra room the deck header's own wrap onto two lines needs; the
   * replay rows, the deck's own rendered height and the footer banner all
   * shift down by this same one-line gap so nothing collides, and the
   * viewBox grows to still hold it.
   */
  const headerWrapGap = crowded ? svgLabelGap(16, label, TYPE.label) : 0;
  const viewBoxH = crowded ? Math.round(H + headerWrapGap) : H;
  useSceneRatio(mobile ? MOBILE_RATIO : crowded ? `${W} / ${viewBoxH}` : null);

  const schemaText = "SCHEMA CHANGE · REMOVE Product.rating";
  const compositionText = "COMPOSITION: GREEN · SUBGRAPHS STILL COMPOSE";
  const deckHeaderText = DECK_HEADER_TEXT;
  const deckHeaderLines = crowded ? dotLines(deckHeaderText) : [deckHeaderText];
  const footerAbortedText = "1 BREAKING · 1 RISKY · FLAGGED BEFORE THE MERGE";
  const footerRunningText = "REPLAYING REAL CLIENT OPERATIONS";

  if (mobile) {
    const m = MOBILE_INSET;
    return (
      <svg
        ref={svgRef}
        viewBox={`0 0 ${MOBILE_W} ${MOBILE_H}`}
        className="h-full w-full"
      >
        <rect width={MOBILE_W} height={MOBILE_H} fill={MC.bg} />

        <text
          x={MOBILE_W / 2}
          y={MOBILE_HEADER_Y1}
          fill={MC.ink}
          fontFamily={MC.mono}
          fontSize={label}
          letterSpacing="0.16em"
          textAnchor="middle"
        >
          {schemaText}
        </text>
        <StackedText
          x={MOBILE_W / 2}
          y={MOBILE_HEADER_Y2}
          lines={dotLines(compositionText)}
          lineHeight={MOBILE_LINE}
          fill={MC.phosphor}
          fontSize={label}
          letterSpacing="0.16em"
          textAnchor="middle"
        />

        <rect
          x={m}
          y={MOBILE_DECK_Y}
          width={MOBILE_W - m * 2}
          height={MOBILE_DECK_BOTTOM - MOBILE_DECK_Y}
          rx="9"
          fill={MC.panel}
          stroke={MC.panelEdge}
        />
        <StackedText
          x={m + 16}
          y={MOBILE_DECK_Y + 22}
          lines={DECK_HEADER_LINES}
          lineHeight={MOBILE_LINE}
          fill={MC.dim}
          fontSize={label}
          letterSpacing="0.16em"
        />

        {MOBILE_ROWS.map(
          ({ replay, opLines, detailLines, nameY, opY, detailY, dividerY }) => {
            const done = REPLAYS.indexOf(replay) < classified;
            const color = done ? VERDICT_COLOR[replay.verdict] : MC.dim;
            const verdictText = done ? replay.verdict : "REPLAYING";
            return (
              <g key={replay.client}>
                <path
                  d={`M${m + 16} ${dividerY}H${MOBILE_W - m - 16}`}
                  stroke={MC.panelEdge}
                  strokeOpacity="0.6"
                />
                <circle
                  cx={m + 26}
                  cy={nameY - 4}
                  r="4"
                  fill={color}
                  style={{ transition: "fill 400ms ease" }}
                />
                <text
                  x={m + 40}
                  y={nameY}
                  fill={done ? MC.ink : MC.dim}
                  fontFamily={MC.mono}
                  fontSize={label}
                  style={{ transition: "fill 400ms ease" }}
                >
                  {replay.client}
                </text>
                <StackedText
                  x={m + 40}
                  y={opY}
                  lines={opLines}
                  lineHeight={MOBILE_LINE}
                  fill={done ? MC.ink : MC.dim}
                  fontSize={label}
                />
                <StackedText
                  x={m + 40}
                  y={detailY}
                  lines={detailLines}
                  lineHeight={MOBILE_LINE}
                  fill={MC.dim}
                  fontSize={label}
                  letterSpacing="0.1em"
                />
                <text
                  x={MOBILE_W - m - 16}
                  y={nameY}
                  fill={color}
                  fontFamily={MC.mono}
                  fontSize={label}
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
                  {verdictText}
                </text>
              </g>
            );
          },
        )}

        <rect
          x={m}
          y={MOBILE_FOOTER_Y}
          width={MOBILE_W - m * 2}
          height={MOBILE_FOOTER_H}
          rx="8"
          fill={MC.panel}
          stroke={blocked ? MC.alert : MC.panelEdge}
          strokeOpacity={blocked ? 0.7 : 1}
          style={{ transition: "stroke 400ms ease" }}
        />
        <StackedText
          x={MOBILE_W / 2}
          y={MOBILE_FOOTER_Y + 26}
          lines={
            blocked
              ? dotLines(footerAbortedText)
              : wrapWords(footerRunningText, 32)
          }
          lineHeight={MOBILE_LINE}
          fill={blocked ? MC.alert : MC.dim}
          fontSize={label}
          letterSpacing="0.18em"
          textAnchor="middle"
        />
      </svg>
    );
  }

  return (
    <svg
      ref={svgRef}
      viewBox={`0 0 ${W} ${viewBoxH}`}
      className="h-full w-full"
    >
      <style>{KEYFRAMES}</style>
      <rect width={W} height={viewBoxH} fill={MC.bg} />

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
        fontSize={label}
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        {schemaText}
      </text>
      <text
        x={W / 2}
        y={78}
        fill={MC.phosphor}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.16em"
        textAnchor="middle"
      >
        {compositionText}
      </text>

      <rect
        x={DECK.x}
        y={DECK.y}
        width={DECK.w}
        height={DECK.h + headerWrapGap}
        rx="9"
        fill={MC.panel}
        stroke={MC.panelEdge}
      />
      <text
        x={DECK.x + 16}
        y={DECK.y + 24}
        fill={MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.16em"
      >
        {deckHeaderLines.map((line, li) => (
          <tspan key={li} x={DECK.x + 16} dy={li === 0 ? 0 : headerWrapGap}>
            {line}
          </tspan>
        ))}
      </text>

      {REPLAYS.map((replay, i) => {
        const y = DECK.y + 52 + headerWrapGap + i * (ROW_H + (rowGap - 15));
        const done = i < classified;
        const color = done ? VERDICT_COLOR[replay.verdict] : MC.dim;
        const nameText = `${replay.client}  ${replay.operation}`;
        const verdictText = done ? replay.verdict : "REPLAYING";
        return (
          <g key={replay.client}>
            <path
              d={`M${DECK.x + 16} ${y + 11 + rowGap}H${DECK.x + DECK.w - 16}`}
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
              fontSize={label}
              style={{ transition: "fill 400ms ease" }}
            >
              {nameText}
            </text>
            <text
              x={DECK.x + 40}
              y={y + rowGap}
              fill={MC.dim}
              fontFamily={MC.mono}
              fontSize={label}
              letterSpacing="0.1em"
            >
              {replay.detail}
            </text>
            <text
              x={DECK.x + DECK.w - 16}
              y={y + 4}
              fill={color}
              fontFamily={MC.mono}
              fontSize={label}
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
              {verdictText}
            </text>
          </g>
        );
      })}

      <rect
        x={DECK.x}
        y={viewBoxH - 56}
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
        y={viewBoxH - 31}
        fill={blocked ? MC.alert : MC.dim}
        fontFamily={MC.mono}
        fontSize={label}
        letterSpacing="0.18em"
        textAnchor="middle"
        style={{ transition: "fill 400ms ease" }}
      >
        {blocked ? footerAbortedText : footerRunningText}
      </text>
    </svg>
  );
}
