import { Fragment } from "react";
import type { CSSProperties, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";

import {
  CANON,
  GatewayChip,
  GlowNode,
  HorizonRule,
  INK_DIM,
  MicroLabel,
  NodeCaption,
} from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";

const W = 1024;
const H = 5320;
const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

const MARKERS = [
  { s: 0, x: 150, y: 100 },
  { s: 1, x: 320, y: 140 },
  { s: 2, x: 512, y: 180 },
  { s: 3, x: 704, y: 220 },
  { s: 4, x: 874, y: 260 },
] as const;

const LANE_END_Y = 4712;
const TERMINAL_Y = 4720;

const PANEL = { x: 110, y: 3760, w: 804, h: 260, rx: 14 } as const;
const PANEL_HEADER_H = 36;
const COLUMN_ROW_Y = 3806;
const ROW_GUTTER_X = 130;
const ROW_LABELS = ["id", "name", "weight", "price", "delivery"] as const;
const rowY = (i: number) => 3850 + i * 34;

/** Which lane (marker index) answers each row. Empty = no field owned. */
const ROW_OWNERS: readonly (readonly number[])[] = [
  [0, 1, 3], // id · @key present on Catalog, Billing, Shipping
  [0], // name · Catalog
  [0], // weight · Catalog
  [1], // price · Billing
  [3], // delivery · Shipping
];

const HORIZON_Y = 4300;
const GATEWAY = { x: 512, y: 4380 } as const;
const QUERY_CHIP = { x: 720, y: 4380, w: 150, h: 64 } as const;
const FAN_TARGETS = [0, 1, 3] as const;
const GHOST_TARGETS = [2, 4] as const;

const GAPS = [
  { x: 470, w: 460, y: 400, h: 320 },
  { x: 95, w: 460, y: 900, h: 320 },
  { x: 220, w: 584, y: 1390, h: 300 },
  { x: 470, w: 460, y: 1860, h: 320 },
  { x: 220, w: 584, y: 2350, h: 300 },
  { x: 220, w: 584, y: 3160, h: 440 },
] as const;

interface PositionedCopy {
  readonly top: number;
  readonly left: number;
  readonly side?: boolean;
}

interface PositionedBox {
  readonly top: number;
  readonly left: number;
  readonly paired?: boolean;
}

interface ChapterLayout {
  readonly copy: PositionedCopy;
  readonly boxes: readonly PositionedBox[];
}

/** Chapters 1-5 keep the transit map's original placement; 6 and 7 clear the dispatch panel. */
const LAYOUT: readonly ChapterLayout[] = [
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
    copy: { top: 3380, left: 50 },
    boxes: [{ top: 4150, left: 27, paired: true }],
  },
  { copy: { top: 5040, left: 50 }, boxes: [] },
];

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

function placement(top: number, left: number): CSSProperties {
  return { "--top": pct(top, H), "--left": `${left}%` } as CSSProperties;
}

const SCRIM =
  "radial-gradient(ellipse 62% 58% at 50% 50%, rgba(11,15,26,0.98) 0%, rgba(11,15,26,0.94) 50%, rgba(11,15,26,0.6) 76%, rgba(11,15,26,0) 93%)";

interface CopyBlockProps extends PositionedCopy {
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

interface BoxSlotProps extends PositionedBox {
  readonly children: ReactNode;
}

function BoxSlot({ top, left, paired, children }: BoxSlotProps) {
  return (
    <div
      className={`mx-auto w-[min(100%,21rem)] sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:mx-0 sm:-translate-x-1/2 sm:-translate-y-1/2 ${
        paired ? "sm:w-[min(43%,21rem)]" : "sm:w-[min(88%,21rem)]"
      }`}
      style={placement(top, left)}
    >
      {children}
    </div>
  );
}

/**
 * Mobile-only stand-in for the composition panel: a real HTML grid (field
 * rows, five color-dot columns), rendered between chapter 6's copy and its
 * code box so the routing table survives without SVG.
 */
function MobileDispatchGrid() {
  return (
    <div className="border-cc-card-border bg-cc-surface w-full rounded-xl border p-4 sm:hidden">
      <MicroLabel>Composition · who answers what</MicroLabel>
      <div
        className="mt-3 grid gap-y-2"
        style={{ gridTemplateColumns: "5rem repeat(5, 1fr)" }}
      >
        <span aria-hidden="true" />
        {CANON.map((service) => (
          <span key={service.name} className="justify-self-center">
            <span
              aria-hidden="true"
              className="inline-block h-2.5 w-2.5 rounded-[3px]"
              style={{ background: service.color }}
              title={service.name}
            />
            <span className="sr-only">{service.name}</span>
          </span>
        ))}
        {ROW_LABELS.map((label, i) => (
          <Fragment key={label}>
            <span className="text-cc-ink-dim font-mono text-xs">{label}</span>
            {CANON.map((service, s) => (
              <span key={service.name} className="justify-self-center">
                {ROW_OWNERS[i].includes(s) ? (
                  <span
                    className="inline-block h-2 w-2 rounded-full"
                    style={{ background: service.color }}
                  />
                ) : (
                  <span
                    className="inline-block h-2 w-2 rounded-full border"
                    style={{ borderColor: "rgba(245,241,234,0.25)" }}
                  />
                )}
              </span>
            ))}
          </Fragment>
        ))}
      </div>
    </div>
  );
}

function DispatchMap() {
  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      aria-hidden="true"
      className="absolute inset-0 z-0 hidden h-full w-full sm:block"
    >
      <defs>
        <linearGradient id="db-gap" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0" stopColor="#fff" />
          <stop offset="0.18" stopColor="#333" />
          <stop offset="0.82" stopColor="#333" />
          <stop offset="1" stopColor="#fff" />
        </linearGradient>
        <mask
          id="db-mask"
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
              fill="url(#db-gap)"
            />
          ))}
        </mask>
        <marker
          id="db-chevron"
          viewBox="0 0 10 10"
          refX="6"
          refY="5"
          markerWidth="6"
          markerHeight="6"
          orient="auto-start-reverse"
        >
          <path d="M0 0 L10 5 L0 10 Z" fill="currentColor" />
        </marker>
      </defs>

      {/* Straight lanes, drawn first so the panel sits on top of them. */}
      <g mask="url(#db-mask)">
        {MARKERS.map((m) => (
          <line
            key={m.s}
            x1={m.x}
            y1={m.y + 8}
            x2={m.x}
            y2={LANE_END_Y}
            stroke={CANON[m.s].color}
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

      {/* Composition panel: a thin routing table spanning all five lanes. */}
      <rect
        x={PANEL.x}
        y={PANEL.y}
        width={PANEL.w}
        height={PANEL.h}
        rx={PANEL.rx}
        fill="rgba(13,20,36,0.86)"
        stroke="rgba(245,241,234,0.13)"
      />
      <text
        x={PANEL.x + PANEL.w / 2}
        y={PANEL.y + PANEL_HEADER_H / 2 + 4}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={10}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        COMPOSITION · WHO ANSWERS WHAT
      </text>
      <line
        x1={PANEL.x}
        x2={PANEL.x + PANEL.w}
        y1={PANEL.y + PANEL_HEADER_H}
        y2={PANEL.y + PANEL_HEADER_H}
        stroke="rgba(245,241,234,0.1)"
      />

      {MARKERS.map((m) => (
        <g key={m.s}>
          <line
            x1={m.x}
            x2={m.x}
            y1={PANEL.y}
            y2={COLUMN_ROW_Y}
            stroke={CANON[m.s].color}
            strokeOpacity={0.5}
          />
          <rect
            x={m.x - 5}
            y={COLUMN_ROW_Y}
            width={10}
            height={10}
            rx={3}
            fill={CANON[m.s].color}
          />
        </g>
      ))}

      {ROW_LABELS.map((label, i) => (
        <g key={label}>
          <line
            x1={PANEL.x + 16}
            x2={PANEL.x + PANEL.w - 16}
            y1={rowY(i) - 20}
            y2={rowY(i) - 20}
            stroke="rgba(245,241,234,0.06)"
          />
          <text
            x={ROW_GUTTER_X}
            y={rowY(i) + 4}
            fontFamily={MONO}
            fontSize={11}
            fill={INK_DIM}
          >
            {label}
          </text>
          {MARKERS.map((m) =>
            ROW_OWNERS[i].includes(m.s) ? (
              <circle
                key={m.s}
                cx={m.x}
                cy={rowY(i)}
                r={4}
                fill={CANON[m.s].color}
              />
            ) : (
              <circle
                key={m.s}
                cx={m.x}
                cy={rowY(i)}
                r={4}
                fill="none"
                stroke="rgba(245,241,234,0.25)"
              />
            ),
          )}
        </g>
      ))}

      <GlowNode x={PANEL.x + PANEL.w / 2} y={PANEL.y} id="db-panel" r={8} />
      <NodeCaption
        x={PANEL.x + PANEL.w / 2 - 20}
        y={PANEL.y - 22}
        toX={PANEL.x + PANEL.w / 2 - 10}
        label="Schema composition"
      />

      {/* Runtime stem: panel to gateway. */}
      <line
        x1={512}
        x2={512}
        y1={PANEL.y + PANEL.h}
        y2={GATEWAY.y - 13}
        stroke="#5eead4"
        strokeWidth={2.5}
        strokeOpacity={0.9}
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

      <GatewayChip x={GATEWAY.x} y={GATEWAY.y} />

      {/* Query chip beside the gateway, echoing the chapter-4 query. */}
      <rect
        x={QUERY_CHIP.x - QUERY_CHIP.w / 2}
        y={QUERY_CHIP.y - QUERY_CHIP.h / 2}
        width={QUERY_CHIP.w}
        height={QUERY_CHIP.h}
        rx={10}
        fill="#0d1424"
        stroke="rgba(245,241,234,0.13)"
      />
      {(["name", "price", "delivery"] as const).map((field, i) => {
        const owner = field === "name" ? 0 : field === "price" ? 1 : 3;
        return (
          <g key={field}>
            <text
              x={QUERY_CHIP.x - QUERY_CHIP.w / 2 + 12}
              y={QUERY_CHIP.y - 14 + i * 15}
              fontFamily={MONO}
              fontSize={10}
              fill="#c9d4e8"
            >
              {field}
            </text>
            <circle
              cx={QUERY_CHIP.x + QUERY_CHIP.w / 2 - 14}
              cy={QUERY_CHIP.y - 18 + i * 15}
              r={3}
              fill={CANON[owner].color}
            />
          </g>
        );
      })}
      <line
        x1={GATEWAY.x + 44}
        x2={QUERY_CHIP.x - QUERY_CHIP.w / 2}
        y1={GATEWAY.y}
        y2={QUERY_CHIP.y}
        stroke="rgba(245,241,234,0.3)"
        strokeDasharray="4 5"
      />

      {/* Selective fan-out: three real calls, two ghosts. */}
      {FAN_TARGETS.map((s) => {
        const target = MARKERS[s];
        const midY = GATEWAY.y + (4620 - GATEWAY.y) * 0.55;
        return (
          <path
            key={s}
            d={`M${GATEWAY.x} ${GATEWAY.y + 13} Q ${GATEWAY.x} ${midY}, ${target.x} 4620`}
            fill="none"
            stroke={CANON[s].color}
            strokeWidth={1.5}
            strokeDasharray="3 4"
            markerEnd="url(#db-chevron)"
            color={CANON[s].color}
          />
        );
      })}
      {GHOST_TARGETS.map((s) => {
        const target = MARKERS[s];
        const dx = target.x - GATEWAY.x;
        const len = Math.hypot(dx, 40) || 1;
        const ex = GATEWAY.x + (dx / len) * 40;
        const ey = GATEWAY.y + 13 + (40 / len) * 40;
        return (
          <g key={s} opacity={0.25}>
            <line
              x1={GATEWAY.x}
              y1={GATEWAY.y + 13}
              x2={ex}
              y2={ey}
              stroke={CANON[s].color}
              strokeWidth={1.5}
              strokeDasharray="2 4"
            />
            <text
              x={ex}
              y={ey + 12}
              textAnchor="middle"
              fontFamily={MONO}
              fontSize={9}
              letterSpacing="0.14em"
              fill={INK_DIM}
            >
              NOT CALLED
            </text>
          </g>
        );
      })}

      {MARKERS.map((m) => (
        <rect
          key={m.s}
          x={m.x - 8}
          y={TERMINAL_Y - 8}
          width={16}
          height={16}
          rx={4}
          fill={CANON[m.s].color}
        />
      ))}
    </svg>
  );
}

/**
 * Prototype v4: The Dispatch Board. Replaces the merging-lines runtime beat
 * with a routing-table panel spanning all five lanes and a selective
 * fan-out at the gateway, in place across the same 7-chapter transit map.
 */
export function DispatchBoard() {
  return (
    <PageSection maxWidth="6xl">
      <div className="relative mx-auto w-full max-w-5xl sm:aspect-[1024/5320]">
        <DispatchMap />
        <div className="flex flex-col gap-14 px-5 py-16 sm:contents">
          {CHAPTERS.map((chapter, i) => {
            const layout = LAYOUT[i];
            return (
              <Fragment key={i}>
                <CopyBlock
                  top={layout.copy.top}
                  left={layout.copy.left}
                  side={layout.copy.side}
                  title={chapter.title}
                >
                  {chapter.body}
                </CopyBlock>
                {i === 5 && (
                  <div className="sm:hidden">
                    <MobileDispatchGrid />
                  </div>
                )}
                {chapter.boxes.map((box, j) => (
                  <BoxSlot
                    key={j}
                    top={layout.boxes[j].top}
                    left={layout.boxes[j].left}
                    paired={layout.boxes[j].paired}
                  >
                    <ProtoCodeBox
                      label={box.label}
                      color={box.color}
                      lines={box.lines}
                    />
                  </BoxSlot>
                ))}
                {i === 5 && (
                  <div className="sm:hidden">
                    <HorizonRule />
                  </div>
                )}
              </Fragment>
            );
          })}
        </div>
      </div>
    </PageSection>
  );
}
