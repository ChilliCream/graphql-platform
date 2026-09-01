import { Fragment } from "react";
import type { CSSProperties, ReactNode } from "react";

import { RevealOnScroll } from "@/src/components/RevealOnScroll";
import { Eyebrow } from "@/src/design-system/Eyebrow";

import {
  CANON,
  GatewayChip,
  GlowNode,
  INK_DIM,
  NodeCaption,
  SchemaCard,
  schemaRowY,
} from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";
import { schemaCardHeight } from "../../visuals/stage";

const W = 1024;
const H = 5120;
const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

/** Lane x per CANON index, matching the production TransitStory map. */
const LANE_X = [150, 320, 512, 704, 874] as const;
const LANE_TOP_Y = [100, 140, 180, 220, 260] as const;
const LANE_BOTTOM_Y = 4750;
const FADE_START_Y = 4700;

/** Tap nodes where each lane's contribution peels off toward the document. */
const TAPS = [
  { i: 0, y: 3180, dim: false },
  { i: 1, y: 3230, dim: false },
  { i: 2, y: 3280, dim: true },
  { i: 3, y: 3330, dim: false },
  { i: 4, y: 3380, dim: true },
] as const;

const CARD = { x: 352, y: 4000, w: 320 } as const;
const CARD_LINES = [
  { code: "type Product {" },
  { code: "  id: ID!" },
  { code: "  name: String!" },
  { code: "  weight: Float!" },
  { code: "  price: Money!" },
  { code: "  delivery: String!" },
  { code: "}" },
] as const;

/** Row index (into CARD_LINES) each lane's leader(s) land on. */
const LEADERS = [
  { lane: 0, rows: [1, 2, 3] }, // Catalog: id, name, weight
  { lane: 1, rows: [1, 4] }, // Billing: id, price
  { lane: 3, rows: [1, 5] }, // Shipping: id, delivery
] as const;

/** Which lanes own which row, for the color-square gutter. */
const ROW_OWNERS: Record<number, readonly number[]> = {
  1: [0, 1, 3], // id: Catalog, Billing, Shipping
  2: [0], // name: Catalog
  3: [0], // weight: Catalog
  4: [1], // price: Billing
  5: [3], // delivery: Shipping
};

const HORIZON_Y = 4280;
const CHIP = { x: 512, y: 4360 } as const;
const CARD_BOTTOM_Y = CARD.y + schemaCardHeight(CARD_LINES.length);

/** Fan-out targets on return: three lanes the chapter-4 query actually needs. */
const FAN_OUT = [
  { lane: 0, solid: true },
  { lane: 1, solid: true },
  { lane: 2, solid: false },
  { lane: 3, solid: true },
  { lane: 4, solid: false },
] as const;
const FAN_OUT_Y = 4560;

function leaderPath(tapX: number, tapY: number, rowY: number): string {
  const c1y = tapY + 240;
  return `M${tapX + 8} ${tapY} C ${tapX + 8} ${c1y}, 300 ${rowY - 60}, ${CARD.x} ${rowY}`;
}

function fanOutPath(laneX: number): string {
  const c1y = CHIP.y + 90;
  const c2y = FAN_OUT_Y - 40;
  return `M${CHIP.x} ${CHIP.y + 13} C ${CHIP.x} ${c1y}, ${laneX} ${c2y}, ${laneX} ${FAN_OUT_Y}`;
}

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

/** Desktop placement: the block's center in the 1024 x H map, as CSS variables. */
function placement(top: number, left: number): CSSProperties {
  return { "--top": pct(top, H), "--left": `${left}%` } as CSSProperties;
}

const SCRIM =
  "radial-gradient(ellipse 62% 58% at 50% 50%, rgba(11,15,26,0.98) 0%, rgba(11,15,26,0.94) 50%, rgba(11,15,26,0.6) 76%, rgba(11,15,26,0) 93%)";

interface CopyBlockProps {
  readonly index: number;
  readonly top: number;
  readonly left: number;
  readonly side?: boolean;
  readonly title: string;
  readonly children: ReactNode;
}

function CopyBlock({
  index,
  top,
  left,
  side,
  title,
  children,
}: CopyBlockProps) {
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
      <RevealOnScroll className="relative">
        <Eyebrow color="ink-dim" size="2xs">
          {`Chapter ${index + 1} / ${CHAPTERS.length}`}
        </Eyebrow>
        <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 mt-2 text-balance">
          {title}
        </h3>
        <div className="text-cc-ink mt-4 space-y-3 text-sm sm:text-base">
          {children}
        </div>
      </RevealOnScroll>
    </div>
  );
}

interface MobileCodeBoxProps {
  readonly top: number;
  readonly left: number;
  readonly paired?: boolean;
  readonly desktopHidden?: boolean;
  readonly label: string;
  readonly color?: string;
  readonly lines: (typeof CHAPTERS)[number]["boxes"][number]["lines"];
}

/** Places a shared ProtoCodeBox on the desktop map; mobile flow stays untouched. */
function MapCodeBox({
  top,
  left,
  paired,
  desktopHidden,
  label,
  color,
  lines,
}: MobileCodeBoxProps) {
  return (
    <div
      className={`mx-auto w-[min(100%,21rem)] sm:mx-0 ${
        desktopHidden
          ? "sm:hidden"
          : `sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:-translate-x-1/2 sm:-translate-y-1/2 ${
              paired ? "sm:w-[min(43%,21rem)]" : "sm:w-[min(88%,21rem)]"
            }`
      }`}
      style={desktopHidden ? undefined : placement(top, left)}
    >
      <ProtoCodeBox label={label} color={color} lines={lines} />
    </div>
  );
}

interface Placement {
  readonly copy: {
    readonly top: number;
    readonly left: number;
    readonly side?: boolean;
  };
  readonly boxes: readonly {
    readonly top: number;
    readonly left: number;
    readonly paired?: boolean;
    readonly desktopHidden?: boolean;
  }[];
}

/**
 * Desktop x/y for each chapter's copy and code boxes. Chapters 1-5 mirror the
 * production TransitStory map; chapter 6's box is replaced on desktop by the
 * SVG SchemaCard geometry below (its ProtoCodeBox stays for the mobile flow).
 */
const PLACEMENTS: readonly Placement[] = [
  { copy: { top: 560, left: 70, side: true }, boxes: [{ top: 560, left: 28 }] },
  {
    copy: { top: 1060, left: 30, side: true },
    boxes: [{ top: 1060, left: 72 }],
  },
  { copy: { top: 1540, left: 50 }, boxes: [] },
  {
    copy: { top: 2020, left: 70, side: true },
    boxes: [{ top: 2020, left: 28 }],
  },
  {
    copy: { top: 2500, left: 50 },
    boxes: [
      { top: 2900, left: 27, paired: true },
      { top: 2900, left: 73, paired: true },
    ],
  },
  {
    copy: { top: 3725, left: 50 },
    boxes: [{ top: 4115, left: 50, desktopHidden: true }],
  },
  { copy: { top: 4880, left: 50 }, boxes: [] },
];

function AssemblingMap() {
  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      aria-hidden="true"
      className="absolute inset-0 z-0 hidden h-full w-full sm:block"
    >
      <defs>
        <linearGradient
          id="atd-lane-fade"
          x1="0"
          y1={FADE_START_Y}
          x2="0"
          y2={LANE_BOTTOM_Y}
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#fff" stopOpacity="1" />
          <stop offset="1" stopColor="#fff" stopOpacity="0" />
        </linearGradient>
        <mask
          id="atd-lane-mask"
          maskUnits="userSpaceOnUse"
          x="0"
          y="0"
          width={W}
          height={H}
        >
          <rect x="0" y="0" width={W} height={FADE_START_Y} fill="#fff" />
          <rect
            x="0"
            y={FADE_START_Y}
            width={W}
            height={LANE_BOTTOM_Y - FADE_START_Y}
            fill="url(#atd-lane-fade)"
          />
        </mask>
        <marker
          id="atd-arrow"
          viewBox="0 0 8 8"
          refX="6"
          refY="4"
          markerWidth="6"
          markerHeight="6"
          orient="auto-start-reverse"
        >
          <path d="M0 0 L8 4 L0 8 Z" fill={INK_DIM} />
        </marker>
      </defs>

      {/* Straight lanes, kept straight through the whole section. */}
      <g mask="url(#atd-lane-mask)">
        {LANE_X.map((x, i) => (
          <line
            key={i}
            x1={x}
            x2={x}
            y1={LANE_TOP_Y[i] + 12}
            y2={LANE_BOTTOM_Y}
            stroke={CANON[i].color}
            strokeWidth={2.5}
            strokeOpacity={0.9}
            strokeLinecap="round"
          />
        ))}
      </g>

      {LANE_X.map((x, i) => (
        <g key={i}>
          <rect
            x={x - 8}
            y={LANE_TOP_Y[i] - 8}
            width={16}
            height={16}
            rx={4}
            fill={CANON[i].color}
          />
          <text
            x={x + 20}
            y={LANE_TOP_Y[i] + 5}
            textAnchor="start"
            fontFamily={MONO}
            fontSize={13}
            letterSpacing="0.18em"
            fill={INK_DIM}
          >
            {CANON[i].name.toUpperCase()}
          </text>
        </g>
      ))}

      {/* Tap nodes: where each lane's contribution leaves toward the document. */}
      {TAPS.map((tap) => (
        <rect
          key={tap.i}
          x={LANE_X[tap.i] - 4}
          y={tap.y - 4}
          width={8}
          height={8}
          rx={2}
          fill={CANON[tap.i].color}
          opacity={tap.dim ? 0.35 : 1}
        />
      ))}

      {/* Leaders: dashed cubics from each contributing lane to its row. */}
      {LEADERS.flatMap((owner) =>
        owner.rows.map((row) => {
          const tap = TAPS.find((t) => t.i === owner.lane);
          if (!tap) return null;
          return (
            <path
              key={`${owner.lane}-${row}`}
              d={leaderPath(LANE_X[owner.lane], tap.y, schemaRowY(CARD.y, row))}
              fill="none"
              stroke={CANON[owner.lane].color}
              strokeOpacity={0.75}
              strokeWidth={1}
              strokeDasharray="4 5"
            />
          );
        }),
      )}

      <GlowNode x={512} y={CARD.y - 8} id="atd-schema-glow" r={8} />
      <NodeCaption
        x={512 - 140}
        y={CARD.y - 8}
        label="Schema composition"
        toX={512 - 12}
      />

      <SchemaCard
        x={CARD.x}
        y={CARD.y}
        w={CARD.w}
        label="Composite schema"
        color="#5eead4"
        file="composite.graphql"
        lines={CARD_LINES}
      />
      {Object.entries(ROW_OWNERS).map(([rowStr, owners]) => {
        const row = Number(rowStr);
        const rowY = schemaRowY(CARD.y, row) - 5;
        return owners.map((lane, k) => (
          <rect
            key={`${row}-${lane}`}
            x={CARD.x + 10}
            y={rowY + (k - (owners.length - 1) / 2) * 7}
            width={6}
            height={6}
            rx={1.5}
            fill={CANON[lane].color}
          />
        ));
      })}

      {/* Build-time / runtime horizon. */}
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

      {/* The document, not a merged line, is what crosses into the gateway. */}
      <line
        x1={512}
        x2={512}
        y1={CARD_BOTTOM_Y}
        y2={CHIP.y - 13}
        stroke="#5eead4"
        strokeWidth={2.5}
        strokeLinecap="round"
      />
      <GatewayChip x={CHIP.x} y={CHIP.y} />

      {FAN_OUT.map(({ lane, solid }) => (
        <path
          key={lane}
          d={fanOutPath(LANE_X[lane])}
          fill="none"
          stroke={CANON[lane].color}
          strokeOpacity={solid ? 0.85 : 0.3}
          strokeWidth={1.5}
          strokeDasharray="4 5"
          markerEnd="url(#atd-arrow)"
        />
      ))}
      <text
        x={512}
        y={FAN_OUT_Y + 30}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={10.5}
        letterSpacing="0.18em"
        fill={INK_DIM}
        stroke="#0b0f1a"
        strokeWidth={8}
        paintOrder="stroke"
      >
        CALLS ONLY WHAT THE QUERY NEEDS
      </text>
    </svg>
  );
}

/**
 * Prototype v2 — "Assembling the Document": composition is drawn as its true
 * output. Dashed leaders run from each still-straight service lane to the
 * exact row of the composite schema it contributes, and it is the resulting
 * document — not a merged line — that crosses the horizon into the gateway.
 */
export function AssemblingTheDocument() {
  return (
    <div className="relative mx-auto w-full max-w-5xl sm:aspect-[1024/5120]">
      <AssemblingMap />
      <div className="flex flex-col gap-14 px-5 py-16 sm:contents">
        {CHAPTERS.map((chapter, i) => (
          <Fragment key={i}>
            <CopyBlock index={i} {...PLACEMENTS[i].copy} title={chapter.title}>
              {chapter.body}
            </CopyBlock>
            {chapter.boxes.map((box, j) => (
              <MapCodeBox
                key={j}
                {...PLACEMENTS[i].boxes[j]}
                label={box.label}
                color={box.color}
                lines={box.lines}
              />
            ))}
          </Fragment>
        ))}
      </div>
    </div>
  );
}
