import type { CSSProperties, ReactNode } from "react";
import { Fragment } from "react";

import { CANON, GatewayChip, GlowNode, INK_DIM } from "../Primitives";
import type { Chapter } from "../story";
import { CHAPTERS, ProtoCodeBox } from "../story";

const W = 1024;
const H = 4900;

const MARKERS = [
  { s: 0, x: 150, y: 100 },
  { s: 1, x: 320, y: 140 },
  { s: 2, x: 512, y: 180 },
  { s: 3, x: 704, y: 220 },
  { s: 4, x: 874, y: 260 },
] as const;

/** Y where every lane taps a branch toward the composition hub. */
const TAP_Y = 3480;
/** Per-lane branch y, staggered so the five stubs do not overlap. */
const BRANCH_Y = [3456, 3468, 3480, 3492, 3504] as const;
const LANE_END_Y = 4750;
const LANE_FADE_START = 4650;

const HUB = { x: 512, y: TAP_Y } as const;
const RAIL_TOP_Y = 3492;

const HORIZON_Y = 4300;
const CHIP = { x: 512, y: 4380 } as const;
const FANOUT_END_Y = 4560;

const GAPS = [
  { x: 470, w: 460, y: 400, h: 320 },
  { x: 95, w: 460, y: 900, h: 320 },
  { x: 220, w: 584, y: 1390, h: 300 },
  { x: 470, w: 460, y: 1860, h: 320 },
  { x: 220, w: 584, y: 2350, h: 300 },
  { x: 220, w: 584, y: 3545, h: 360 },
  { x: 220, w: 584, y: 4450, h: 300 },
] as const;

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

/** Desktop placement: the block's center in the 1024 x H map, as CSS variables. */
function placement(top: number, left: number): CSSProperties {
  return { "--top": pct(top, H), "--left": `${left}%` } as CSSProperties;
}

const SCRIM =
  "radial-gradient(ellipse 62% 58% at 50% 50%, rgba(11,15,26,0.98) 0%, rgba(11,15,26,0.94) 50%, rgba(11,15,26,0.6) 76%, rgba(11,15,26,0) 93%)";

interface Placement {
  readonly top: number;
  readonly left: number;
  readonly side?: boolean;
}

interface BoxPlacement extends Placement {
  readonly paired?: boolean;
}

/** Copy positions, unchanged from the production map for chapters 1-5. */
const COPY_PLACEMENT: readonly Placement[] = [
  { top: 560, left: 70, side: true },
  { top: 1060, left: 30, side: true },
  { top: 1540, left: 50 },
  { top: 2020, left: 70, side: true },
  { top: 2500, left: 50 },
  { top: 3740, left: 50 },
  { top: 4600, left: 50 },
];

/** Box positions per chapter; chapters with no boxes get an empty array. */
const BOX_PLACEMENT: readonly (readonly BoxPlacement[])[] = [
  [{ top: 560, left: 28 }],
  [{ top: 1060, left: 72 }],
  [],
  [{ top: 2020, left: 28 }],
  [
    { top: 2900, left: 27, paired: true },
    { top: 2900, left: 73, paired: true },
  ],
  [{ top: 4100, left: 50 }],
  [],
];

interface CopyBlockProps extends Placement {
  readonly title: string;
  readonly children: ReactNode;
}

function CopyBlock({ top, left, side, title, children }: CopyBlockProps) {
  return (
    <div
      className={`relative w-full text-center sm:absolute sm:top-(--top) sm:left-(--left) sm:z-20 sm:-translate-x-1/2 sm:-translate-y-1/2 ${
        side ? "sm:w-[min(44%,26rem)] sm:text-left" : "sm:w-[min(92%,34rem)]"
      }`}
      style={placement(top, left)}
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -inset-x-32 -inset-y-20 hidden sm:block"
        style={{ background: SCRIM }}
      />
      <div className="relative">
        <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
          {title}
        </h3>
        <div className="text-cc-ink mt-4 space-y-3 text-sm sm:text-base">
          {children}
        </div>
      </div>
    </div>
  );
}

interface CodeBoxSlotProps extends BoxPlacement {
  readonly box: Chapter["boxes"][number];
}

function CodeBoxSlot({ top, left, paired, box }: CodeBoxSlotProps) {
  return (
    <div
      className={`mx-auto w-[min(100%,21rem)] sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:mx-0 sm:-translate-x-1/2 sm:-translate-y-1/2 ${
        paired ? "sm:w-[min(43%,21rem)]" : "sm:w-[min(88%,21rem)]"
      }`}
      style={placement(top, left)}
    >
      <ProtoCodeBox label={box.label} color={box.color} lines={box.lines} />
    </div>
  );
}

const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

/**
 * Compact, presentational stand-in for the chapter-6 moment on mobile, where
 * the full 1024x4900 map is hidden: five stubs tap into one glow node, one
 * teal stem drops to the gateway, and three dashed arrows return outward.
 */
function MobileTapDiagram() {
  const stubXs = [40, 90, 160, 230, 280] as const;
  const hubX = 160;
  const hubY = 60;
  const gatewayY = 150;

  return (
    <svg
      viewBox="0 0 320 200"
      aria-hidden="true"
      className="mx-auto w-full max-w-xs sm:hidden"
    >
      <defs>
        <linearGradient
          id="sr-m-rail"
          x1="0"
          y1={hubY}
          x2="0"
          y2={gatewayY}
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#5eead4" />
          <stop offset="1" stopColor="#16b9e4" />
        </linearGradient>
      </defs>
      {stubXs.map((x, i) => (
        <g key={i}>
          <line
            x1={x}
            y1={20}
            x2={x}
            y2={40}
            stroke={CANON[i].color}
            strokeWidth={2.5}
            strokeOpacity={0.9}
          />
          <line
            x1={x}
            y1={40}
            x2={hubX}
            y2={hubY}
            stroke={CANON[i].color}
            strokeWidth={1.5}
            strokeOpacity={0.7}
          />
        </g>
      ))}
      <circle cx={hubX} cy={hubY} r={5} fill="#ffffff" />
      <line
        x1={hubX}
        y1={hubY + 5}
        x2={hubX}
        y2={gatewayY - 13}
        stroke="url(#sr-m-rail)"
        strokeWidth={2.5}
      />
      <rect
        x={hubX - 34}
        y={gatewayY - 13}
        width={68}
        height={22}
        rx={7}
        fill="#0d1424"
        stroke="rgba(94,234,212,0.45)"
      />
      <text
        x={hubX}
        y={gatewayY + 2}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={8}
        letterSpacing="0.16em"
        fill="#5eead4"
      >
        GATEWAY
      </text>
      {[100, 160, 220].map((x, i) => (
        <path
          key={i}
          d={`M${hubX} ${gatewayY + 9} C ${hubX} ${gatewayY + 30}, ${x} ${gatewayY + 20}, ${x} ${gatewayY + 40}`}
          fill="none"
          stroke={CANON[i + 1].color}
          strokeWidth={1}
          strokeOpacity={0.8}
          strokeDasharray="2 4"
        />
      ))}
    </svg>
  );
}

function TransitMap() {
  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      aria-hidden="true"
      className="absolute inset-0 z-0 hidden h-full w-full sm:block"
    >
      <defs>
        <linearGradient
          id="sr-rail"
          x1="0"
          y1={RAIL_TOP_Y}
          x2="0"
          y2={CHIP.y - 13}
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#5eead4" />
          <stop offset="1" stopColor="#16b9e4" />
        </linearGradient>
        <linearGradient id="sr-gap" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#fff" />
          <stop offset="0.18" stopColor="#333" />
          <stop offset="0.82" stopColor="#333" />
          <stop offset="1" stopColor="#fff" />
        </linearGradient>
        <mask
          id="sr-mask"
          maskUnits="userSpaceOnUse"
          x="0"
          y="0"
          width={W}
          height={H}
        >
          <rect x="0" y="0" width={W} height={H} fill="#fff" />
          {GAPS.map((g, i) => (
            <rect
              key={i}
              x={g.x}
              y={g.y}
              width={g.w}
              height={g.h}
              fill="url(#sr-gap)"
            />
          ))}
        </mask>
        <linearGradient
          id="sr-fade"
          x1="0"
          y1={LANE_FADE_START}
          x2="0"
          y2={LANE_END_Y}
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#fff" />
          <stop offset="1" stopColor="#fff" stopOpacity="0" />
        </linearGradient>
        <mask
          id="sr-lanefade"
          maskUnits="userSpaceOnUse"
          x="0"
          y="0"
          width={W}
          height={H}
        >
          <rect x="0" y="0" width={W} height={LANE_FADE_START} fill="#fff" />
          <rect
            x="0"
            y={LANE_FADE_START}
            width={W}
            height={LANE_END_Y - LANE_FADE_START}
            fill="url(#sr-fade)"
          />
        </mask>
        {CANON.map((service, i) => (
          <marker
            key={i}
            id={`sr-arrow-${i}`}
            viewBox="0 0 8 8"
            refX="6"
            refY="4"
            markerWidth={5}
            markerHeight={5}
            orient="auto-start-reverse"
          >
            <path d="M0 0 L8 4 L0 8 Z" fill={service.color} />
          </marker>
        ))}
      </defs>

      <g mask="url(#sr-mask)">
        {/* Five straight service lanes: unchanged above the tap, dimmed below it. */}
        {MARKERS.map((m) => (
          <path
            key={`above-${m.s}`}
            d={`M${m.x} ${m.y + 12} L${m.x} ${TAP_Y}`}
            fill="none"
            stroke={CANON[m.s].color}
            strokeWidth={2.5}
            strokeOpacity={0.9}
            strokeLinecap="round"
          />
        ))}
        <g mask="url(#sr-lanefade)">
          {MARKERS.map((m) => (
            <path
              key={`below-${m.s}`}
              d={`M${m.x} ${TAP_Y} L${m.x} ${LANE_END_Y}`}
              fill="none"
              stroke={CANON[m.s].color}
              strokeWidth={2.5}
              strokeOpacity={0.55}
              strokeLinecap="round"
            />
          ))}
        </g>

        {/* Short branches tapping each lane into the composition hub. */}
        {MARKERS.map((m, i) => (
          <g key={`tap-${m.s}`}>
            {m.x !== HUB.x && (
              <line
                x1={m.x}
                y1={BRANCH_Y[i]}
                x2={HUB.x}
                y2={BRANCH_Y[i]}
                stroke={CANON[m.s].color}
                strokeWidth={1.5}
                strokeOpacity={0.7}
              />
            )}
            <rect
              x={m.x - 4}
              y={BRANCH_Y[i] - 4}
              width={8}
              height={8}
              rx={2}
              fill={CANON[m.s].color}
            />
          </g>
        ))}

        {/* The new rail: born at the hub, carrying the composite schema alone. */}
        <line
          x1={HUB.x}
          y1={RAIL_TOP_Y}
          x2={CHIP.x}
          y2={CHIP.y - 13}
          stroke="url(#sr-rail)"
          strokeWidth={6}
          strokeOpacity={0.12}
        />
        <line
          x1={HUB.x}
          y1={RAIL_TOP_Y}
          x2={CHIP.x}
          y2={CHIP.y - 13}
          stroke="url(#sr-rail)"
          strokeWidth={2.5}
        />
      </g>

      {MARKERS.map((m) => (
        <g key={m.s}>
          <rect
            x={m.x - 8}
            y={m.y - 8}
            width={16}
            height={16}
            rx={4}
            fill={CANON[m.s].color}
          />
          <text
            x={m.x + 20}
            y={m.y + 5}
            textAnchor="start"
            fontFamily={MONO}
            fontSize={13}
            letterSpacing="0.18em"
            fill={INK_DIM}
          >
            {CANON[m.s].name.toUpperCase()}
          </text>
        </g>
      ))}

      <GlowNode x={HUB.x} y={HUB.y} id="sr-hub" r={10} />
      <text
        x={HUB.x - 122}
        y={HUB.y + 4}
        textAnchor="end"
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        SCHEMA COMPOSITION
      </text>
      <line
        x1={HUB.x - 112}
        x2={HUB.x - 38}
        y1={HUB.y}
        y2={HUB.y}
        stroke="rgba(245,241,234,0.3)"
        strokeDasharray="4 5"
      />

      <line
        x1={120}
        x2={904}
        y1={HORIZON_Y}
        y2={HORIZON_Y}
        stroke="rgba(245,241,234,0.22)"
        strokeDasharray="5 7"
      />
      <text
        x={140}
        y={HORIZON_Y - 16}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        BUILD TIME
      </text>
      <text
        x={140}
        y={HORIZON_Y + 26}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
        opacity={0.7}
      >
        RUNTIME
      </text>

      <GatewayChip x={CHIP.x} y={CHIP.y} />

      {/* Runtime fan-out: dashed return traffic from the gateway back to the
          five lanes, which never stopped running below the horizon. */}
      {MARKERS.map((m, i) => {
        const dx = m.x - CHIP.x;
        const c1x = CHIP.x + dx * 0.15;
        const c2x = CHIP.x + dx * 0.6;
        return (
          <path
            key={`fanout-${m.s}`}
            d={`M${CHIP.x} ${CHIP.y + 14} C ${c1x} ${CHIP.y + 80}, ${c2x} ${CHIP.y + 140}, ${m.x} ${FANOUT_END_Y}`}
            fill="none"
            stroke={CANON[m.s].color}
            strokeWidth={1}
            strokeOpacity={0.8}
            strokeDasharray="3 5"
            markerEnd={`url(#sr-arrow-${i})`}
          />
        );
      })}
      <text
        x={HUB.x + 12}
        y={CHIP.y + 100}
        fontFamily={MONO}
        fontSize={10}
        letterSpacing="0.18em"
        fill={INK_DIM}
      >
        QUERY PLAN FAN-OUT
      </text>
    </svg>
  );
}

/**
 * "The Sixth Rail": the composite schema drawn as a new teal rail born at the
 * composition hub, not a merge of the five service lines. The five lanes keep
 * running, dimmed, past the tap; the gateway's fan-out arrows point back out
 * to them, drawn as runtime traffic returning to lines that never stopped.
 */
export function SixthRail() {
  return (
    <div className="relative mx-auto w-full max-w-5xl sm:aspect-[1024/4900]">
      <TransitMap />
      <div className="flex flex-col gap-14 px-5 py-16 sm:contents">
        {CHAPTERS.map((chapter, i) => (
          <Fragment key={i}>
            <CopyBlock {...COPY_PLACEMENT[i]} title={chapter.title}>
              {chapter.body}
            </CopyBlock>
            {i === 5 && <MobileTapDiagram />}
            {BOX_PLACEMENT[i].map((placement, j) => (
              <CodeBoxSlot key={j} {...placement} box={chapter.boxes[j]} />
            ))}
          </Fragment>
        ))}
      </div>
    </div>
  );
}
