import type { CSSProperties, ReactNode } from "react";
import { Fragment } from "react";

import { CANON, GatewayChip, INK_DIM } from "../Primitives";
import type { Chapter } from "../story";
import { CHAPTERS, ProtoCodeBox } from "../story";

const W = 1024;
const H = 4900;
const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

/** Canvas background used to "punch" a notch or a keyhole through a tile. */
const CANVAS_BG = "#0b0f1a";
/** PCB-style tile fill, matching the rest of the prototypes' code chrome. */
const TILE_BG = "#0d1424";

const MARKERS = [
  { s: 0, x: 150, y: 100 },
  { s: 1, x: 320, y: 140 },
  { s: 2, x: 512, y: 180 },
  { s: 3, x: 704, y: 220 },
  { s: 4, x: 874, y: 260 },
] as const;

const LANE_END_Y = 4820;
const LANE_FADE_START = 4680;

const FRAME = { x: 512, y: 2560, w: 340, h: 220 } as const;

const PANEL_LINE_TOP_Y = 4130;
const HORIZON_Y = 4300;
const CHIP = { x: 512, y: 4380 } as const;
const FANOUT_END_Y = 4520;

const GAPS = [
  { x: 470, w: 460, y: 400, h: 320 },
  { x: 95, w: 460, y: 900, h: 320 },
  { x: 220, w: 584, y: 1390, h: 300 },
  { x: 470, w: 460, y: 1860, h: 320 },
  { x: 172, w: 680, y: 2760, h: 620 },
  { x: 130, w: 764, y: 3400, h: 780 },
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

/** Copy positions: one per chapter, in the same 1024 x 4900 coordinate space
 * the background map uses. */
const COPY_PLACEMENT: readonly Placement[] = [
  { top: 560, left: 70, side: true },
  { top: 1060, left: 30, side: true },
  { top: 1540, left: 50 },
  { top: 2020, left: 70, side: true },
  { top: 2900, left: 50 },
  { top: 3550, left: 50 },
  { top: 4600, left: 50 },
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

interface CodeBoxSlotProps extends Placement {
  readonly box: Chapter["boxes"][number];
  readonly width?: string;
}

function CodeBoxSlot({
  top,
  left,
  box,
  width = "sm:w-[min(88%,21rem)]",
}: CodeBoxSlotProps) {
  return (
    <div
      className={`mx-auto w-[min(100%,21rem)] sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:mx-0 sm:-translate-x-1/2 sm:-translate-y-1/2 ${width}`}
      style={placement(top, left)}
    >
      <ProtoCodeBox label={box.label} color={box.color} lines={box.lines} />
    </div>
  );
}

/** Strips a schema box down to its bare field lines (drops the `type X {`
 * opener and the closing `}`), so a tile can show only its owned fields. */
function fieldLines(box: Chapter["boxes"][number] | undefined) {
  if (!box) return [];
  return box.lines
    .map((l) => l.text.trim())
    .filter((t) => t !== "}" && !t.startsWith("type "));
}

/** Minimal PCB-key glyph: a loop and two teeth, drawn in `currentColor`. */
function KeyGlyph({ x, y }: { readonly x: number; readonly y: number }) {
  return (
    <g stroke="#5eead4" strokeWidth={1.1} fill="none" opacity={0.9}>
      <circle cx={x} cy={y} r={2.3} />
      <line x1={x + 2.1} y1={y} x2={x + 7} y2={y} />
      <line x1={x + 5} y1={y} x2={x + 5} y2={y + 2.2} />
      <line x1={x + 7} y1={y} x2={x + 7} y2={y + 2.2} />
    </g>
  );
}

interface TileCardProps {
  readonly label: string;
  readonly color: string;
  readonly fields: readonly string[];
  readonly notchSide: "left" | "right";
  readonly idPrefix: string;
}

/**
 * A team's source schema drawn as a dark, PCB-style tile: a colored border,
 * a header row, its field rows, and a semicircular notch cut into one edge
 * where its `@key` field lives. Two tiles with notches on facing edges read
 * as two halves of one keyhole once composition clicks them together.
 */
function TileCard({
  label,
  color,
  fields,
  notchSide,
  idPrefix,
}: TileCardProps) {
  const notchX = notchSide === "right" ? 210 : 0;
  const labelX = notchSide === "right" ? 176 : 34;
  const labelAnchor = notchSide === "right" ? "end" : "start";
  const keyX = notchSide === "right" ? 182 : 12;

  return (
    <svg
      viewBox="0 0 210 120"
      className="w-full max-w-[220px] overflow-visible"
      role="img"
      aria-label={`${label} tile`}
    >
      <rect
        x={1}
        y={1}
        width={208}
        height={118}
        rx={10}
        fill={TILE_BG}
        stroke={color}
        strokeWidth={1.5}
      />
      <circle cx={notchX} cy={60} r={9} fill={CANVAS_BG} />
      <rect x={4} y={11} width={9} height={9} rx={2.5} fill={color} />
      <text
        x={17}
        y={19}
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.14em"
        fill={INK_DIM}
      >
        {label.toUpperCase()}
      </text>
      <line x1={4} x2={206} y1={28} y2={28} stroke="rgba(245,241,234,0.12)" />
      {fields.map((f, i) => (
        <text
          key={i}
          x={12}
          y={46 + i * 16}
          fontFamily={MONO}
          fontSize={10.5}
          fill={f.startsWith("id") ? "#5eead4" : "#c9d4e8"}
        >
          {f}
        </text>
      ))}
      <text
        x={labelX}
        y={64}
        textAnchor={labelAnchor}
        fontFamily={MONO}
        fontSize={8.5}
        fill="#5eead4"
      >
        {idPrefix}
      </text>
      <KeyGlyph x={keyX} y={64} />
    </svg>
  );
}

interface TileSubProps {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly color: string;
  readonly label: string;
  readonly fields: readonly string[];
}

/** One tile as it sits inside the composed panel: smaller, no notch of its
 * own to draw (the seams carry that job), just its border, dot and fields. */
function PanelTile({ x, y, w, h, color, label, fields }: TileSubProps) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height={h}
        fill={TILE_BG}
        stroke={color}
        strokeWidth={1}
        strokeOpacity={0.5}
      />
      <rect x={x + 8} y={y + 8} width={7} height={7} rx={2} fill={color} />
      <text
        x={x + 19}
        y={y + 15}
        fontFamily={MONO}
        fontSize={7.5}
        letterSpacing="0.1em"
        fill={INK_DIM}
      >
        {label.toUpperCase()}
      </text>
      {fields.map((f, i) => (
        <g key={i}>
          <circle cx={x + 11} cy={y + 30 + i * 13} r={2} fill={color} />
          <text
            x={x + 18}
            y={y + 33 + i * 13}
            fontFamily={MONO}
            fontSize={8.5}
            fill="#c9d4e8"
          >
            {f}
          </text>
        </g>
      ))}
    </g>
  );
}

const PANEL_W = 560;
const PANEL_H = 300;
/** Hand-placed tessellation: Catalog/Billing on top, Ordering/Shipping/User
 * on the bottom, widths tuned so every shared edge lines up exactly. */
const CATALOG_W = 260;
const BILLING_W = 280;
const ORDERING_W = 180;
const SHIPPING_W = 190;
const USER_W = 170;
const TOP_H = 140;
const BOTTOM_H = 140;
const PANEL_PAD_X = 10;
const PANEL_HEADER_H = 20;

/**
 * The five source schemas clicked into one composite panel. The panel stays
 * visibly plural: every tile keeps its own border color, every seam between
 * tiles is drawn twice (once per owner), and the seam where Catalog and
 * Billing meet opens into a glowing keyhole labeled `@key id`.
 */
function CompositionPanel() {
  const catalogFields = fieldLines(CHAPTERS[5].boxes[0]).filter((f) =>
    ["id", "name", "weight"].some((k) => f.startsWith(k)),
  );
  const billingFields = fieldLines(CHAPTERS[5].boxes[0]).filter((f) =>
    ["id", "price"].some((k) => f.startsWith(k)),
  );
  const shippingFields = fieldLines(CHAPTERS[5].boxes[0]).filter((f) =>
    f.startsWith("delivery"),
  );
  const orderingFields = [CHAPTERS[0].boxes[0].lines[2].text];
  const userFields = [CHAPTERS[0].boxes[0].lines[4].text];

  const catalogX = PANEL_PAD_X;
  const billingX = catalogX + CATALOG_W;
  const orderingX = PANEL_PAD_X;
  const shippingX = orderingX + ORDERING_W;
  const userX = shippingX + SHIPPING_W;
  const topY = PANEL_HEADER_H;
  const bottomY = topY + TOP_H;
  const keyholeX = billingX;
  const keyholeY = topY + TOP_H / 2;

  return (
    <svg
      viewBox={`0 0 ${PANEL_W} ${PANEL_H}`}
      className="w-full max-w-[36rem] overflow-visible"
      role="img"
      aria-label="Composite schema, seams between the five teams' tiles still visible"
    >
      <text
        x={PANEL_W / 2}
        y={12}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={10}
        letterSpacing="0.16em"
        fill={INK_DIM}
      >
        COMPOSITE SCHEMA · SEAMS STAY VISIBLE
      </text>

      <PanelTile
        x={catalogX}
        y={topY}
        w={CATALOG_W}
        h={TOP_H}
        color={CANON[0].color}
        label={CANON[0].name}
        fields={catalogFields}
      />
      <PanelTile
        x={billingX}
        y={topY}
        w={BILLING_W}
        h={TOP_H}
        color={CANON[1].color}
        label={CANON[1].name}
        fields={billingFields}
      />
      <PanelTile
        x={orderingX}
        y={bottomY}
        w={ORDERING_W}
        h={BOTTOM_H}
        color={CANON[2].color}
        label={CANON[2].name}
        fields={orderingFields}
      />
      <PanelTile
        x={shippingX}
        y={bottomY}
        w={SHIPPING_W}
        h={BOTTOM_H}
        color={CANON[3].color}
        label={CANON[3].name}
        fields={shippingFields}
      />
      <PanelTile
        x={userX}
        y={bottomY}
        w={USER_W}
        h={BOTTOM_H}
        color={CANON[4].color}
        label={CANON[4].name}
        fields={userFields}
      />

      {/* Every shared edge drawn twice, 2px apart, one stroke per owner. */}
      <line
        x1={billingX - 1}
        x2={billingX - 1}
        y1={topY}
        y2={bottomY}
        stroke={CANON[0].color}
        strokeWidth={1.5}
      />
      <line
        x1={billingX + 1}
        x2={billingX + 1}
        y1={topY}
        y2={bottomY}
        stroke={CANON[1].color}
        strokeWidth={1.5}
      />
      <line
        x1={shippingX - 1}
        x2={shippingX - 1}
        y1={bottomY}
        y2={bottomY + BOTTOM_H}
        stroke={CANON[2].color}
        strokeWidth={1.5}
      />
      <line
        x1={shippingX + 1}
        x2={shippingX + 1}
        y1={bottomY}
        y2={bottomY + BOTTOM_H}
        stroke={CANON[3].color}
        strokeWidth={1.5}
      />
      <line
        x1={userX - 1}
        x2={userX - 1}
        y1={bottomY}
        y2={bottomY + BOTTOM_H}
        stroke={CANON[3].color}
        strokeWidth={1.5}
      />
      <line
        x1={userX + 1}
        x2={userX + 1}
        y1={bottomY}
        y2={bottomY + BOTTOM_H}
        stroke={CANON[4].color}
        strokeWidth={1.5}
      />
      <line
        x1={PANEL_PAD_X}
        x2={PANEL_W - PANEL_PAD_X}
        y1={bottomY - 0.75}
        y2={bottomY - 0.75}
        stroke="rgba(245,241,234,0.28)"
        strokeWidth={1}
      />
      <line
        x1={PANEL_PAD_X}
        x2={PANEL_W - PANEL_PAD_X}
        y1={bottomY + 0.75}
        y2={bottomY + 0.75}
        stroke="rgba(245,241,234,0.28)"
        strokeWidth={1}
      />

      {/* Catalog and Billing's half-notches align into one keyhole. */}
      <text
        x={keyholeX}
        y={keyholeY - 14}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9}
        fill="#5eead4"
      >
        @key id
      </text>
      <circle
        cx={keyholeX}
        cy={keyholeY}
        r={9}
        fill={CANVAS_BG}
        stroke="#5eead4"
        strokeWidth={1.5}
      />
    </svg>
  );
}

/**
 * The whole thesis of this concept in one static, ~340-wide square SVG:
 * five tiles clicked into one panel, colored seams, and the @key keyhole
 * where Catalog and Billing meet. Mobile hides the tall service lanes
 * entirely, so this is the only artifact that has to carry the idea alone.
 */
function MobileTessellation() {
  return (
    <div className="mx-auto w-full max-w-[21rem] sm:hidden">
      <CompositionPanel />
      <p className="text-cc-nav-label mt-2 text-center font-mono text-[10px] tracking-[0.2em] uppercase">
        Five teams, one panel, seams still visible
      </p>
    </div>
  );
}

interface TilesRowProps {
  readonly top: number;
}

/** Beat 5: Catalog and Billing's source schemas, redrawn as two notched
 * tiles whose notches already point at each other. */
function TilesRow({ top }: TilesRowProps) {
  const catalogBox = CHAPTERS[4].boxes[0];
  const billingBox = CHAPTERS[4].boxes[1];

  return (
    <div
      className="mx-auto flex w-[min(100%,26rem)] items-center justify-center gap-3 sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:mx-0 sm:w-[min(90%,30rem)] sm:-translate-x-1/2 sm:-translate-y-1/2"
      style={placement(top, 50)}
    >
      <TileCard
        label={catalogBox.label}
        color={catalogBox.color ?? CANON[0].color}
        fields={fieldLines(catalogBox)}
        notchSide="right"
        idPrefix="id"
      />
      <TileCard
        label={billingBox.label}
        color={billingBox.color ?? CANON[1].color}
        fields={fieldLines(billingBox)}
        notchSide="left"
        idPrefix="id"
      />
    </div>
  );
}

interface PanelSlotProps {
  readonly top: number;
}

function PanelSlot({ top }: PanelSlotProps) {
  return (
    <div
      className="hidden sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:block sm:w-[min(90%,36rem)] sm:-translate-x-1/2 sm:-translate-y-1/2"
      style={placement(top, 50)}
    >
      <CompositionPanel />
    </div>
  );
}

/**
 * Background map: five service lanes running straight and unbroken from top
 * to bottom (they never converge), the beat-3/4 empty frame posing "one
 * schema, who writes it?", a teal connector from the composed panel to the
 * gateway, and the gateway chip's five-color map stripe with its dashed
 * fan-out to every still-running lane.
 */
function TransitMap() {
  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      aria-hidden="true"
      className="absolute inset-0 z-0 hidden h-full w-full sm:block"
    >
      <defs>
        <linearGradient
          id="kt-rail"
          x1="0"
          y1={PANEL_LINE_TOP_Y}
          x2="0"
          y2={CHIP.y - 13}
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor="#5eead4" />
          <stop offset="1" stopColor="#16b9e4" />
        </linearGradient>
        <linearGradient id="kt-gap" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#fff" />
          <stop offset="0.18" stopColor="#333" />
          <stop offset="0.82" stopColor="#333" />
          <stop offset="1" stopColor="#fff" />
        </linearGradient>
        <mask
          id="kt-mask"
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
              fill="url(#kt-gap)"
            />
          ))}
        </mask>
        <linearGradient
          id="kt-fade"
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
          id="kt-lanefade"
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
            fill="url(#kt-fade)"
          />
        </mask>
        {CANON.map((service, i) => (
          <marker
            key={i}
            id={`kt-arrow-${i}`}
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

      <g mask="url(#kt-lanefade)">
        <g mask="url(#kt-mask)">
          {MARKERS.map((m) => (
            <path
              key={`lane-${m.s}`}
              d={`M${m.x} ${m.y + 12} L${m.x} ${LANE_END_Y}`}
              fill="none"
              stroke={CANON[m.s].color}
              strokeWidth={2.5}
              strokeOpacity={0.9}
              strokeLinecap="round"
            />
          ))}
        </g>
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

      {/* Beat 3/4: an empty frame poses the question the tiles answer. */}
      <rect
        x={FRAME.x - FRAME.w / 2}
        y={FRAME.y - FRAME.h / 2}
        width={FRAME.w}
        height={FRAME.h}
        rx={14}
        fill="none"
        stroke="rgba(245,241,234,0.25)"
        strokeDasharray="5 7"
      />
      <text
        x={FRAME.x}
        y={FRAME.y + FRAME.h / 2 + 26}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.16em"
        fill={INK_DIM}
      >
        ONE SCHEMA — WHO WRITES IT?
      </text>

      {/* Teal connector: the composed panel hands its schema to the gateway. */}
      <line
        x1={512}
        x2={512}
        y1={PANEL_LINE_TOP_Y}
        y2={CHIP.y - 13}
        stroke="url(#kt-rail)"
        strokeWidth={6}
        strokeOpacity={0.12}
      />
      <line
        x1={512}
        x2={512}
        y1={PANEL_LINE_TOP_Y}
        y2={CHIP.y - 13}
        stroke="url(#kt-rail)"
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
      {/* The chip's five-color stripe: it holds the map from every tile. */}
      {CANON.map((service, i) => (
        <rect
          key={`stripe-${i}`}
          x={CHIP.x - 20 + i * 8}
          y={CHIP.y + 16}
          width={8}
          height={10}
          fill={service.color}
        />
      ))}

      {MARKERS.map((m, i) => {
        const dx = m.x - CHIP.x;
        const c1x = CHIP.x + dx * 0.15;
        const c2x = CHIP.x + dx * 0.6;
        return (
          <path
            key={`fanout-${m.s}`}
            d={`M${CHIP.x} ${CHIP.y + 28} C ${c1x} ${CHIP.y + 70}, ${c2x} ${CHIP.y + 110}, ${m.x} ${FANOUT_END_Y}`}
            fill="none"
            stroke={CANON[m.s].color}
            strokeWidth={1}
            strokeOpacity={0.8}
            strokeDasharray="3 5"
            markerEnd={`url(#kt-arrow-${i})`}
          />
        );
      })}
    </svg>
  );
}

/**
 * "Keyed Tiles": each team's schema is a notched tile; composition clicks
 * the tiles into one panel whose seams stay visible. Service lanes run
 * straight and unbroken the whole way down — what travels is the artifact,
 * not the services — and `@key` gets a physical meaning: it is the notch
 * that lets two teams' definitions of the same thing interlock.
 */
export function KeyedTiles() {
  return (
    <div className="relative mx-auto w-full max-w-5xl sm:aspect-[1024/4900]">
      <TransitMap />
      <div className="flex flex-col gap-14 px-5 py-16 sm:contents">
        {CHAPTERS.map((chapter, i) => (
          <Fragment key={i}>
            <CopyBlock {...COPY_PLACEMENT[i]} title={chapter.title}>
              {chapter.body}
            </CopyBlock>
            {i === 0 && (
              <CodeBoxSlot top={560} left={28} box={chapter.boxes[0]} />
            )}
            {i === 1 && (
              <CodeBoxSlot top={1060} left={72} box={chapter.boxes[0]} />
            )}
            {i === 3 && (
              <CodeBoxSlot top={2020} left={28} box={chapter.boxes[0]} />
            )}
            {i === 4 && <TilesRow top={3180} />}
            {i === 5 && <PanelSlot top={3950} />}
            {i === 5 && <MobileTessellation />}
          </Fragment>
        ))}
      </div>
    </div>
  );
}
