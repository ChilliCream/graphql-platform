import { Fragment } from "react";
import type { CSSProperties, ReactNode } from "react";

import { CANON, GatewayChip, INK_DIM } from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";

const W = 1024;
const H = 4900;
const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

/** Five straight staves. They never bend: only the plate crosses them. */
const LANE_X = [150, 320, 512, 704, 874] as const;
const LANE_TOP = 120;
const LANE_BOTTOM = 4820;
const FADE_START = 4700;

const MARKERS = [
  { s: 0, x: 150, y: 100 },
  { s: 1, x: 320, y: 140 },
  { s: 2, x: 512, y: 180 },
  { s: 3, x: 704, y: 220 },
  { s: 4, x: 874, y: 260 },
] as const;

const TAP_DOT_Y = 1290;

const BRACKET = { x1: 110, x2: 914, yTop: 1552, yBottom: 1570, labelY: 1602 };
const BRACKET_LANE_X = 704;

const SCHEMA_STUB_Y = 2900;

const PLATE = { x: 112, y: 3380, w: 800, h: 200 };
const PLATE_ROW_START = PLATE.y + 62;
const PLATE_ROW_STEP = 26;

const SPINE_X = 524;
const SPINE_TOP = PLATE.y + PLATE.h;
const HORIZON_Y = 4300;
const CHIP = { x: 512, y: 4380 } as const;
const CUE_Y = 4500;

/** Field-to-lane rows for the composition plate, sourced from chapter one so
 * the field names are never retyped. */
const PLATE_ROWS = CHAPTERS[0].boxes[0].lines.map((line, i) => ({
  label: line.text,
  color: CANON[i].color,
  laneX: LANE_X[i],
}));

const GAPS = [
  { x: 470, w: 460, y: 400, h: 320 },
  { x: 95, w: 460, y: 900, h: 320 },
  { x: 95, w: 460, y: 1390, h: 340 },
  { x: 470, w: 460, y: 1900, h: 320 },
  { x: 220, w: 584, y: 2350, h: 300 },
  { x: 220, w: 584, y: 3560, h: 360 },
  { x: 220, w: 584, y: 4450, h: 300 },
] as const;

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

/** Desktop placement: the block's center in the 1024 x H map, as CSS variables. */
function placement(top: number, left: number): CSSProperties {
  return { "--top": pct(top, H), "--left": `${left}%` } as CSSProperties;
}

const SCRIM =
  "radial-gradient(ellipse 62% 58% at 50% 50%, rgba(11,15,26,0.98) 0%, rgba(11,15,26,0.94) 50%, rgba(11,15,26,0.6) 76%, rgba(11,15,26,0) 93%)";

interface CopyBlockProps {
  readonly top: number;
  readonly left: number;
  readonly side?: boolean;
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

interface PlacedBoxProps {
  readonly top: number;
  readonly left: number;
  readonly paired?: boolean;
  readonly label: string;
  readonly color?: string;
  readonly lines: (typeof CHAPTERS)[number]["boxes"][number]["lines"];
}

function PlacedBox({ top, left, paired, label, color, lines }: PlacedBoxProps) {
  return (
    <div
      className={`mx-auto w-[min(100%,21rem)] sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:mx-0 sm:-translate-x-1/2 sm:-translate-y-1/2 ${
        paired ? "sm:w-[min(43%,21rem)]" : "sm:w-[min(88%,21rem)]"
      }`}
      style={placement(top, left)}
    >
      <ProtoCodeBox label={label} color={color} lines={lines} />
    </div>
  );
}

/** Per-chapter copy and box placement in the 1024 x 4900 map. */
const PLACEMENT: readonly {
  readonly copy: Omit<CopyBlockProps, "children">;
  readonly boxes: readonly Omit<PlacedBoxProps, "label" | "color" | "lines">[];
}[] = [
  {
    copy: { top: 560, left: 70, side: true, title: "" },
    boxes: [{ top: 560, left: 28 }],
  },
  {
    copy: { top: 1060, left: 30, side: true, title: "" },
    boxes: [{ top: 1060, left: 72 }],
  },
  { copy: { top: 1600, left: 30, side: true, title: "" }, boxes: [] },
  {
    copy: { top: 2020, left: 70, side: true, title: "" },
    boxes: [{ top: 2020, left: 28 }],
  },
  {
    copy: { top: 2500, left: 50, title: "" },
    boxes: [
      { top: SCHEMA_STUB_Y, left: 27, paired: true },
      { top: SCHEMA_STUB_Y, left: 73, paired: true },
    ],
  },
  {
    copy: { top: 3760, left: 50, title: "" },
    boxes: [{ top: 4100, left: 50 }],
  },
  { copy: { top: 4600, left: 50, title: "" }, boxes: [] },
];

/** The full 1024 x 4900 score: five never-bending staves, crossed once by a
 * translucent composition plate that samples every lane at the field it
 * owns. Desktop only; a compact static figure stands in on mobile. */
function Score() {
  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      aria-hidden="true"
      className="absolute inset-0 z-0 hidden h-full w-full sm:block"
    >
      <defs>
        <linearGradient id="csc-gap" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#fff" />
          <stop offset="0.18" stopColor="#333" />
          <stop offset="0.82" stopColor="#333" />
          <stop offset="1" stopColor="#fff" />
        </linearGradient>
        <linearGradient
          id="csc-fade-bottom"
          x1="0"
          y1={FADE_START}
          x2="0"
          y2={LANE_BOTTOM}
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#fff" />
          <stop offset="1" stopColor="#000" />
        </linearGradient>
        <mask
          id="csc-mask"
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
              fill="url(#csc-gap)"
            />
          ))}
          <rect
            x="0"
            y={FADE_START}
            width={W}
            height={LANE_BOTTOM - FADE_START}
            fill="url(#csc-fade-bottom)"
          />
        </mask>
      </defs>

      {/* The five staves: straight for the whole 4900px, no exceptions. */}
      <g mask="url(#csc-mask)">
        {LANE_X.map((x, i) => (
          <line
            key={i}
            x1={x}
            x2={x}
            y1={LANE_TOP}
            y2={LANE_BOTTOM}
            stroke={CANON[i].color}
            strokeWidth={2.5}
            strokeOpacity={0.9}
            strokeLinecap="round"
          />
        ))}
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

      {/* Beat 2: each app touches every lane once. */}
      {LANE_X.map((x, i) => (
        <circle key={i} cx={x} cy={TAP_DOT_Y} r={5} fill="#fff" />
      ))}

      {/* Beat 3: the chokepoint is a clamp across all five staves, not a merge. */}
      <g>
        <line
          x1={BRACKET.x1}
          x2={BRACKET.x2}
          y1={BRACKET.yTop}
          y2={BRACKET.yTop}
          stroke="rgba(245,241,234,0.5)"
          strokeWidth={1.5}
        />
        <line
          x1={BRACKET.x1}
          x2={BRACKET.x2}
          y1={BRACKET.yBottom}
          y2={BRACKET.yBottom}
          stroke="rgba(245,241,234,0.5)"
          strokeWidth={1.5}
        />
        <line
          x1={BRACKET.x1}
          x2={BRACKET.x1}
          y1={BRACKET.yTop}
          y2={BRACKET.yBottom}
          stroke="rgba(245,241,234,0.5)"
          strokeWidth={1.5}
        />
        <line
          x1={BRACKET.x2}
          x2={BRACKET.x2}
          y1={BRACKET.yTop}
          y2={BRACKET.yBottom}
          stroke="rgba(245,241,234,0.5)"
          strokeWidth={1.5}
        />
        {Array.from({ length: 8 }, (_, i) => (
          <rect
            key={i}
            x={BRACKET_LANE_X - 3}
            y={BRACKET.yTop - 10 - i * 6}
            width={6}
            height={2}
            fill={INK_DIM}
          />
        ))}
        <text
          x={BRACKET.x2}
          y={BRACKET.labelY}
          textAnchor="end"
          fontFamily={MONO}
          fontSize={11}
          letterSpacing="0.18em"
          fill={INK_DIM}
        >
          ONE TEAM · ONE QUEUE
        </text>
      </g>

      {/* Beat 5: the schema is a per-lane artifact, tethered by a short stub. */}
      <line
        x1={LANE_X[0]}
        x2={LANE_X[0]}
        y1={SCHEMA_STUB_Y - 26}
        y2={SCHEMA_STUB_Y + 26}
        stroke={CANON[0].color}
        strokeWidth={2}
      />
      <line
        x1={LANE_X[1]}
        x2={LANE_X[1]}
        y1={SCHEMA_STUB_Y - 26}
        y2={SCHEMA_STUB_Y + 26}
        stroke={CANON[1].color}
        strokeWidth={2}
      />

      {/* Beat 6: the plate reads every stave where it crosses. */}
      <rect
        x={PLATE.x}
        y={PLATE.y}
        width={PLATE.w}
        height={PLATE.h}
        rx={14}
        fill="rgba(13,20,36,0.88)"
        stroke="rgba(94,234,212,0.35)"
      />
      <text
        x={PLATE.x + 20}
        y={PLATE.y + 30}
        fontFamily={MONO}
        fontSize={10}
        letterSpacing="0.16em"
        fill={INK_DIM}
      >
        SCHEMA COMPOSITION · READS THE STAVES, PRINTS ONE SCORE
      </text>
      {PLATE_ROWS.map((row, i) => (
        <g key={row.label}>
          <text
            x={PLATE.x + 60}
            y={PLATE_ROW_START + i * PLATE_ROW_STEP}
            fontFamily={MONO}
            fontSize={12}
            fill="#c9d4e8"
          >
            {row.label}
          </text>
          <circle
            cx={row.laneX}
            cy={PLATE_ROW_START + i * PLATE_ROW_STEP - 4}
            r={5}
            fill={row.color}
          />
        </g>
      ))}
      {/* Lanes redrawn through the plate at low opacity: the guard against
          reading this as a bus bar that merges the staves. */}
      <g opacity={0.25}>
        {LANE_X.map((x, i) => (
          <line
            key={i}
            x1={x}
            x2={x}
            y1={PLATE.y}
            y2={PLATE.y + PLATE.h}
            stroke={CANON[i].color}
            strokeWidth={2.5}
          />
        ))}
      </g>

      {/* The only new line in the drawing: the schema artifact, born at
          build time, dropping to the gateway. */}
      <line
        x1={SPINE_X}
        x2={SPINE_X}
        y1={SPINE_TOP}
        y2={CHIP.y}
        stroke="#5eead4"
        strokeWidth={2.5}
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

      {/* Beat 7: the conductor cues each still-running player from the score. */}
      {LANE_X.map((x, i) => (
        <g key={i}>
          <line
            x1={CHIP.x}
            y1={CHIP.y}
            x2={x}
            y2={CUE_Y}
            stroke="rgba(245,241,234,0.4)"
            strokeWidth={1}
            strokeDasharray="3 5"
          />
          <circle cx={x} cy={CUE_Y} r={5} fill={CANON[i].color} />
        </g>
      ))}
      <text
        x={LANE_X[0] + 10}
        y={CUE_Y + 24}
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.16em"
        fill={INK_DIM}
      >
        ORDINARY GRAPHQL QUERY
      </text>
    </svg>
  );
}

/** Compact 320x420 stand-in for the tall score on phone width: five short
 * staves, the plate crossing them with aligned ticks, the teal spine, the
 * horizon dash, the gateway, and its five cue-lines. */
function MobileScore() {
  const laneX = [40, 100, 160, 220, 280];
  const top = 20;
  const bottom = 260;
  const plateY = 130;
  const plateH = 46;
  const spineBottom = 340;
  const horizonY = 355;
  const chip = { x: 160, y: 375 };
  const cueY = 410;

  return (
    <svg
      viewBox="0 0 320 420"
      aria-hidden="true"
      className="mx-auto block h-auto w-full max-w-xs sm:hidden"
    >
      {laneX.map((x, i) => (
        <line
          key={i}
          x1={x}
          x2={x}
          y1={top}
          y2={bottom}
          stroke={CANON[i].color}
          strokeWidth={2}
          strokeOpacity={0.9}
        />
      ))}

      <rect
        x={16}
        y={plateY}
        width={288}
        height={plateH}
        rx={8}
        fill="rgba(13,20,36,0.88)"
        stroke="rgba(94,234,212,0.35)"
      />
      <g opacity={0.25}>
        {laneX.map((x, i) => (
          <line
            key={i}
            x1={x}
            x2={x}
            y1={plateY}
            y2={plateY + plateH}
            stroke={CANON[i].color}
            strokeWidth={2}
          />
        ))}
      </g>
      {laneX.map((x, i) => (
        <circle
          key={i}
          cx={x}
          cy={plateY + plateH / 2}
          r={3.5}
          fill={CANON[i].color}
        />
      ))}

      <line
        x1={166}
        x2={166}
        y1={plateY + plateH}
        y2={spineBottom}
        stroke="#5eead4"
        strokeWidth={2}
      />

      <line
        x1={16}
        x2={304}
        y1={horizonY}
        y2={horizonY}
        stroke="rgba(245,241,234,0.22)"
        strokeDasharray="4 6"
      />

      <GatewayChip x={chip.x} y={chip.y} w={64} />

      {laneX.map((x, i) => (
        <g key={i}>
          <line
            x1={chip.x}
            y1={chip.y}
            x2={x}
            y2={cueY}
            stroke="rgba(245,241,234,0.4)"
            strokeWidth={1}
            strokeDasharray="2 4"
          />
          <circle cx={x} cy={cueY} r={3.5} fill={CANON[i].color} />
        </g>
      ))}
    </svg>
  );
}

export function ConductorsScore() {
  return (
    <div className="relative mx-auto w-full max-w-5xl sm:aspect-[1024/4900]">
      <Score />
      <div className="flex flex-col gap-14 px-5 py-16 sm:contents">
        {CHAPTERS.map((chapter, i) => (
          <Fragment key={i}>
            <CopyBlock {...PLACEMENT[i].copy} title={chapter.title}>
              {chapter.body}
            </CopyBlock>
            {chapter.boxes.map((box, j) => (
              <PlacedBox
                key={j}
                {...PLACEMENT[i].boxes[j]}
                label={box.label}
                color={box.color}
                lines={box.lines}
              />
            ))}
            {i === 5 && <MobileScore />}
          </Fragment>
        ))}
      </div>
    </div>
  );
}
