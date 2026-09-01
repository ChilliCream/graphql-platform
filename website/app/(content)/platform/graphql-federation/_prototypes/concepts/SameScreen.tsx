"use client";

import { Fragment } from "react";
import type { CSSProperties, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";

import {
  CANON,
  GatewayChip,
  GlowNode,
  HorizonRule,
  INK_DIM,
  NodeCaption,
  SchemaCard,
} from "../Primitives";
import { schemaCardHeight } from "../../visuals/stage";
import { CHAPTERS, ProtoCodeBox } from "../story";
import {
  PulseGlyph,
  easeInOutCubic,
  measure,
  ramp,
  useVisual,
} from "../../visuals/anim";

const W = 1024;
const H = 5500;
const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";
const CANVAS_BG = "#0b0f1a";
const DASHED = "rgba(245,241,234,0.28)";
const GREY_CABLE = "rgba(139,160,188,0.55)";

/** Device-frame geometry, in its own local (unscaled) unit system. */
const FRAME_W = 180;
const FRAME_H = 240;
const HEADER_Y = 28;
const SLOT_W = 150;
const SLOT_H = 16;
const SLOT_START_Y = 40;
const SLOT_PITCH = 22;

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

function placement(top: number, left: number): CSSProperties {
  return { "--top": pct(top, H), "--left": `${left}%` } as CSSProperties;
}

const SCRIM =
  "radial-gradient(ellipse 62% 58% at 50% 50%, rgba(11,15,26,0.98) 0%, rgba(11,15,26,0.94) 50%, rgba(11,15,26,0.6) 76%, rgba(11,15,26,0) 93%)";

interface Placement {
  readonly top: number;
  readonly left: number;
  readonly narrow?: boolean;
}

/** Copy positions: one per chapter, alternating sides opposite the frame so
 * the two never share a horizontal half of the 1024-wide map. */
const COPY_PLACEMENT: readonly Placement[] = [
  { top: 360, left: 74, narrow: true },
  { top: 1030, left: 26, narrow: true },
  { top: 1600, left: 76, narrow: true },
  { top: 2350, left: 24, narrow: true },
  { top: 2875, left: 74, narrow: true },
  { top: 3650, left: 50, narrow: false },
  { top: 5050, left: 26, narrow: true },
];

interface CopyBlockProps extends Placement {
  readonly title: string;
  readonly children: ReactNode;
}

function CopyBlock({ top, left, narrow, title, children }: CopyBlockProps) {
  return (
    <div
      className={`relative w-full text-center sm:absolute sm:top-(--top) sm:left-(--left) sm:z-20 sm:-translate-x-1/2 sm:-translate-y-1/2 sm:text-left ${
        narrow ? "sm:w-[min(40%,25rem)]" : "sm:w-[min(60%,32rem)]"
      }`}
      style={placement(top, left)}
    >
      <div
        aria-hidden="true"
        className="pointer-events-none absolute -inset-x-10 -inset-y-14 hidden sm:block"
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

interface BoxSlotProps {
  readonly top: number;
  readonly left: number;
  readonly width?: string;
  readonly children: ReactNode;
}

function BoxSlot({
  top,
  left,
  width = "sm:w-[min(30%,19rem)]",
  children,
}: BoxSlotProps) {
  return (
    <div
      className={`mx-auto w-[min(100%,21rem)] sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:mx-0 sm:-translate-x-1/2 sm:-translate-y-1/2 ${width}`}
      style={placement(top, left)}
    >
      {children}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Device frame                                                        */
/* ------------------------------------------------------------------ */

interface FrameProps {
  /** Center x, top y, in the 1024 x H map (or a mini local viewBox). */
  readonly x: number;
  readonly y: number;
  readonly scale?: number;
  readonly counter: string;
  readonly captionOverride?: string;
  readonly frameOpacity?: number;
  readonly slotOpacities?: readonly [number, number, number, number, number];
  readonly idPrefix: string;
}

/**
 * The one recurring silhouette this concept redraws at every beat: a
 * rounded 180x240 device frame with five service-tinted field slots and a
 * port notch cut into the bottom stroke. Only `slotOpacities`,
 * `frameOpacity` and the caption change beat to beat.
 */
function Frame({
  x,
  y,
  scale = 1,
  counter,
  captionOverride,
  frameOpacity = 1,
  slotOpacities = [0.85, 0.85, 0.85, 0.85, 0.85],
  idPrefix,
}: FrameProps) {
  const w = FRAME_W * scale;
  const h = FRAME_H * scale;
  const left = x - w / 2;
  const slotW = SLOT_W * scale;
  const slotH = SLOT_H * scale;
  const slotLeft = x - slotW / 2;
  const portY = y + h;

  return (
    <g opacity={frameOpacity}>
      <rect
        x={left}
        y={y}
        width={w}
        height={h}
        rx={16 * scale}
        fill="rgba(12,19,34,0.55)"
        stroke="rgba(245,241,234,0.28)"
        strokeWidth={1.5}
      />
      <line
        x1={left}
        x2={left + w}
        y1={y + HEADER_Y * scale}
        y2={y + HEADER_Y * scale}
        stroke="rgba(245,241,234,0.14)"
      />
      {CANON.map((service, i) => (
        <g key={service.name}>
          <rect
            x={slotLeft}
            y={y + (SLOT_START_Y + i * SLOT_PITCH) * scale}
            width={slotW}
            height={slotH}
            rx={4 * scale}
            fill={service.color}
            fillOpacity={slotOpacities[i] * 0.14}
            stroke={service.color}
            strokeOpacity={slotOpacities[i]}
            strokeWidth={1}
          />
          <rect
            x={slotLeft + 6 * scale}
            y={
              y +
              (SLOT_START_Y + i * SLOT_PITCH) * scale +
              slotH / 2 -
              3 * scale
            }
            width={6 * scale}
            height={6 * scale}
            rx={2 * scale}
            fill={service.color}
            opacity={slotOpacities[i]}
          />
        </g>
      ))}
      {/* Port: a gap punched into the bottom stroke plus two flanking ticks. */}
      <rect
        x={x - 7 * scale}
        y={portY - 1.5 * scale}
        width={14 * scale}
        height={3 * scale}
        fill={CANVAS_BG}
      />
      <line
        x1={x - 9 * scale}
        x2={x - 9 * scale}
        y1={portY - 4 * scale}
        y2={portY + 4 * scale}
        stroke="rgba(245,241,234,0.28)"
        strokeWidth={1}
      />
      <line
        x1={x + 9 * scale}
        x2={x + 9 * scale}
        y1={portY - 4 * scale}
        y2={portY + 4 * scale}
        stroke="rgba(245,241,234,0.28)"
        strokeWidth={1}
      />
      <text
        x={x}
        y={portY + 22 * scale}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9 * scale}
        letterSpacing="0.2em"
        fill={INK_DIM}
        stroke={CANVAS_BG}
        strokeWidth={5}
        paintOrder="stroke"
      >
        {(captionOverride ?? `THE PRODUCT PAGE · ${counter}`).toUpperCase()}
      </text>
    </g>
  );
}

/** Faint outline-only echo of the frame, used for the ghost-app duplicates in beat 2. */
function FrameGhost({
  x,
  y,
  opacity,
}: {
  readonly x: number;
  readonly y: number;
  readonly opacity: number;
}) {
  return (
    <rect
      x={x - FRAME_W / 2}
      y={y}
      width={FRAME_W}
      height={FRAME_H}
      rx={16}
      fill="none"
      stroke="rgba(245,241,234,0.28)"
      strokeWidth={1.5}
      opacity={opacity}
    />
  );
}

function cable(
  x0: number,
  y0: number,
  x1: number,
  y1: number,
  color: string,
  opacity = 1,
) {
  const midY = y0 + (y1 - y0) * 0.55;
  return (
    <path
      key={`${x1}-${y1}`}
      d={`M${x0} ${y0} C ${x0} ${midY}, ${x1} ${y0 + (y1 - y0) * 0.25}, ${x1} ${y1}`}
      fill="none"
      stroke={color}
      strokeWidth={2}
      strokeOpacity={opacity}
      strokeLinecap="round"
    />
  );
}

/* ------------------------------------------------------------------ */
/* Beat 1: scatter, connected to nothing                               */
/* ------------------------------------------------------------------ */

const BEAT1 = { x: 260, y: 380 } as const;
const SCATTER = [
  { x: 90, y: 700, dx: 0 },
  { x: 150, y: 735, dx: 1 },
  { x: 110, y: 775, dx: 2 },
  { x: 170, y: 690, dx: 3 },
  { x: 200, y: 745, dx: 4 },
] as const;

function Beat1() {
  const port = { x: BEAT1.x, y: BEAT1.y + FRAME_H };
  return (
    <g>
      <text
        x={BEAT1.x}
        y={BEAT1.y - 16}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9.5}
        letterSpacing="0.18em"
        fill={INK_DIM}
      >
        ANY CLIENT · WEB / IOS / PARTNER
      </text>
      <Frame x={BEAT1.x} y={BEAT1.y} counter="01/07" idPrefix="ss-b1" />
      <line
        x1={port.x}
        y1={port.y + 30}
        x2={port.x}
        y2={port.y + 48}
        stroke={DASHED}
        strokeDasharray="3 4"
      />
      <text
        x={port.x + 14}
        y={port.y + 52}
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.14em"
        fill={INK_DIM}
      >
        NO SINGLE PLACE TO ASK
      </text>
      {SCATTER.map((s) => (
        <g key={s.dx}>
          <rect
            x={s.x}
            y={s.y}
            width={16}
            height={16}
            rx={3}
            fill={CANON[s.dx].color}
            opacity={0.8}
          />
          <text
            x={s.x + 22}
            y={s.y + 12}
            fontFamily={MONO}
            fontSize={8.5}
            letterSpacing="0.1em"
            fill={INK_DIM}
            opacity={0.85}
          >
            {CANON[s.dx].name.toUpperCase()}
          </text>
        </g>
      ))}
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 2: every app merges it itself                                  */
/* ------------------------------------------------------------------ */

const BEAT2 = { x: 764, y: 1000 } as const;
const B2_TARGETS = [
  { x: 664, y: 1280 },
  { x: 714, y: 1300 },
  { x: 764, y: 1306 },
  { x: 814, y: 1300 },
  { x: 864, y: 1280 },
] as const;
const B2_ARC = measure([
  [BEAT2.x, BEAT2.y + FRAME_H],
  [BEAT2.x, BEAT2.y + FRAME_H + 70],
  [BEAT2.x + 100, BEAT2.y + FRAME_H + 190],
  [864, 1280],
]);

function Beat2({ set }: { readonly set: ReturnType<typeof useVisual>["set"] }) {
  const port = { x: BEAT2.x, y: BEAT2.y + FRAME_H };
  return (
    <g>
      <FrameGhost x={BEAT2.x + 14} y={BEAT2.y + 14} opacity={0.35} />
      <FrameGhost x={BEAT2.x + 28} y={BEAT2.y + 28} opacity={0.18} />
      {B2_TARGETS.map((t, i) =>
        cable(port.x, port.y, t.x, t.y, CANON[i].color, 0.85),
      )}
      <Frame x={BEAT2.x} y={BEAT2.y} counter="02/07" idPrefix="ss-b2" />
      {B2_TARGETS.map((t, i) => (
        <rect
          key={CANON[i].name}
          x={t.x - 8}
          y={t.y}
          width={16}
          height={16}
          rx={3}
          fill={CANON[i].color}
        />
      ))}
      {/* Crossed-wires merge() glyph in the frame footer. */}
      <g transform={`translate(${BEAT2.x - 34} ${BEAT2.y + FRAME_H - 32})`}>
        <line x1={0} y1={0} x2={9} y2={9} stroke={INK_DIM} strokeWidth={1.1} />
        <line x1={0} y1={9} x2={9} y2={0} stroke={INK_DIM} strokeWidth={1.1} />
        <text
          x={14}
          y={8}
          fontFamily={MONO}
          fontSize={9}
          letterSpacing="0.06em"
          fill={INK_DIM}
        >
          merge()
        </text>
      </g>
      <text
        x={BEAT2.x}
        y={BEAT2.y - 16}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9.5}
        letterSpacing="0.16em"
        fill={INK_DIM}
        opacity={0.75}
      >
        EVERY APP
      </text>
      <PulseGlyph
        set={set}
        id="ss-p2"
        main="#ffffff"
        soft="#ffffff"
        filter="ss-soft"
      />
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 3: one API team, one queue                                     */
/* ------------------------------------------------------------------ */

const BEAT3 = { x: 260, y: 1620 } as const;
const QUEUE_ROWS = [
  { label: "REQ · CATALOG", color: CANON[0].color },
  { label: "REQ · BILLING", color: CANON[1].color },
  { label: "REQ · SHIPPING", color: CANON[3].color },
] as const;

function Beat3() {
  const port = { x: BEAT3.x, y: BEAT3.y + FRAME_H };
  const cardX = BEAT3.x - 20;
  const cardY = port.y + 90;
  const cardW = 130;
  const cardH = 160;
  const parkX = cardX + cardW + 90;

  return (
    <g>
      <line
        x1={port.x}
        y1={port.y}
        x2={cardX + cardW / 2}
        y2={cardY}
        stroke={GREY_CABLE}
        strokeWidth={2}
        strokeLinecap="round"
      />
      <Frame x={BEAT3.x} y={BEAT3.y} counter="03/07" idPrefix="ss-b3" />
      <rect
        x={cardX}
        y={cardY}
        width={cardW}
        height={cardH}
        rx={12}
        fill="rgba(12,19,34,0.6)"
        stroke="rgba(245,241,234,0.16)"
      />
      <text
        x={cardX + cardW / 2}
        y={cardY + 20}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9.5}
        letterSpacing="0.16em"
        fill={INK_DIM}
      >
        ONE API TEAM
      </text>
      <line
        x1={cardX + 10}
        x2={cardX + cardW - 10}
        y1={cardY + 30}
        y2={cardY + 30}
        stroke="rgba(245,241,234,0.12)"
      />
      {QUEUE_ROWS.map((row, i) => (
        <g
          key={row.label}
          transform={`translate(${cardX + 10} ${cardY + 42 + i * 38})`}
        >
          <rect width={7} height={7} rx={2} fill={row.color} />
          <text
            x={12}
            y={7}
            fontFamily={MONO}
            fontSize={8}
            letterSpacing="0.05em"
            fill="#c9d4e8"
          >
            {row.label}
          </text>
          <rect
            x={0}
            y={14}
            width={38}
            height={14}
            rx={7}
            fill="none"
            stroke="#f27765"
            strokeOpacity={0.5}
          />
          <text
            x={19}
            y={24}
            textAnchor="middle"
            fontFamily={MONO}
            fontSize={7.5}
            letterSpacing="0.1em"
            fill="#f27765"
          >
            HELD
          </text>
        </g>
      ))}
      {CANON.map((service, i) => (
        <rect
          key={service.name}
          x={parkX}
          y={cardY + 6 + i * 26}
          width={16}
          height={16}
          rx={3}
          fill={service.color}
          opacity={0.35}
        />
      ))}
      <text
        x={parkX}
        y={cardY - 8}
        fontFamily={MONO}
        fontSize={8.5}
        letterSpacing="0.1em"
        fill={INK_DIM}
        opacity={0.7}
      >
        UNTOUCHED
      </text>
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 4: one query                                                    */
/* ------------------------------------------------------------------ */

const BEAT4 = { x: 764, y: 2240 } as const;

function Beat4() {
  const port = { x: BEAT4.x, y: BEAT4.y + FRAME_H };
  const chip = { x: BEAT4.x, y: port.y + 74 };
  return (
    <g>
      <line
        x1={port.x}
        y1={port.y}
        x2={chip.x}
        y2={chip.y - 13}
        stroke={GREY_CABLE}
        strokeWidth={2}
        strokeLinecap="round"
      />
      <Frame
        x={BEAT4.x}
        y={BEAT4.y}
        counter="04/07"
        idPrefix="ss-b4"
        slotOpacities={[0.9, 0.9, 0.25, 0.9, 0.25]}
      />
      <GlowNode x={chip.x} y={chip.y} id="ss-b4-glow" r={5} />
      <GatewayChip x={chip.x} y={chip.y} label="ONE SCHEMA" w={140} />
      <text
        x={chip.x}
        y={chip.y + 30}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={8.5}
        letterSpacing="0.14em"
        fill={INK_DIM}
        opacity={0.75}
      >
        GRAPHQL ENDPOINT
      </text>
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 5: no single team to author it                                 */
/* ------------------------------------------------------------------ */

const BEAT5 = { x: 220, y: 2680 } as const;
const PEN_ANGLES = [-30, 10, 90, 130, 170] as const;

function Beat5() {
  const sheet = { x: BEAT5.x - 75, y: BEAT5.y + 280, w: 170, h: 110 };
  const cx = sheet.x + sheet.w / 2;
  const cy = sheet.y + sheet.h / 2;
  return (
    <g>
      <Frame
        x={BEAT5.x}
        y={BEAT5.y}
        counter="05/07"
        idPrefix="ss-b5"
        frameOpacity={0.5}
        slotOpacities={[0.5, 0.5, 0.5, 0.5, 0.5]}
      />
      <text
        x={BEAT5.x}
        y={BEAT5.y - 16}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.16em"
        fill={INK_DIM}
        opacity={0.7}
      >
        NOT THE PROBLEM ANYMORE
      </text>
      <rect
        x={sheet.x}
        y={sheet.y}
        width={sheet.w}
        height={sheet.h}
        rx={10}
        fill="none"
        stroke="rgba(245,241,234,0.3)"
        strokeDasharray="5 6"
      />
      <text
        x={cx}
        y={cy}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.12em"
        fill={INK_DIM}
      >
        SCHEMA
      </text>
      <text
        x={cx}
        y={cy + 20}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.12em"
        fill={INK_DIM}
      >
        AUTHOR?
      </text>
      {CANON.map((service, i) => {
        const angle = (PEN_ANGLES[i] * Math.PI) / 180;
        const px = cx + Math.cos(angle) * 104;
        const py = cy + Math.sin(angle) * 84;
        return (
          <path
            key={service.name}
            d={`M${px} ${py} l 9 -4 l -3 10 z`}
            fill={service.color}
            opacity={0.85}
          />
        );
      })}
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 6: screen off, build time                                      */
/* ------------------------------------------------------------------ */

const BEAT6 = { x: 200, y: 4050 } as const;
const MINI_CARD_W = 104;
const MINI_LINES = [{ code: "id: ID!" }, { code: "name: String!", dim: true }];
const MINI_ARC = [
  { x: 318, y: 4070 },
  { x: 442, y: 4030 },
  { x: 566, y: 4020 },
  { x: 690, y: 4030 },
  { x: 814, y: 4070 },
] as const;
const PEN = { x: 945, y: 4050, w: 70, h: 190 } as const;
const HORIZON_Y = 4700;

function Beat6() {
  const arcHeight = schemaCardHeight(MINI_LINES.length);
  const composeY = Math.max(...MINI_ARC.map((m) => m.y)) + arcHeight + 30;

  return (
    <g>
      <Frame
        x={BEAT6.x}
        y={BEAT6.y}
        counter="06/07"
        idPrefix="ss-b6"
        frameOpacity={0.55}
        slotOpacities={[0.12, 0.12, 0.12, 0.12, 0.12]}
        captionOverride="Screen off · build time"
      />
      {MINI_ARC.map((m, i) => (
        <SchemaCard
          key={CANON[i].name}
          x={m.x}
          y={m.y}
          w={MINI_CARD_W}
          label={CANON[i].name}
          file=""
          color={CANON[i].color}
          lines={MINI_LINES}
        />
      ))}
      <g transform={`translate(${(318 + 918) / 2} ${composeY})`}>
        <rect
          x={-68}
          y={-14}
          width={136}
          height={28}
          rx={14}
          fill="rgba(12,19,34,0.6)"
          stroke="rgba(94,234,212,0.45)"
        />
        <path
          d="M-46 0 l6 6 l10 -12"
          fill="none"
          stroke="#5eead4"
          strokeWidth={1.6}
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <text
          x={4}
          y={4}
          textAnchor="middle"
          fontFamily={MONO}
          fontSize={9}
          letterSpacing="0.14em"
          fill="#5eead4"
        >
          CC COMPOSE
        </text>
      </g>
      <rect
        x={PEN.x}
        y={PEN.y}
        width={PEN.w}
        height={PEN.h}
        rx={14}
        fill="none"
        stroke="rgba(245,241,234,0.3)"
        strokeDasharray="5 6"
      />
      {CANON.map((service, i) => (
        <rect
          key={service.name}
          x={PEN.x + PEN.w / 2 - 8}
          y={PEN.y + 18 + i * 30}
          width={16}
          height={16}
          rx={3}
          fill={service.color}
          opacity={0.6}
        />
      ))}
      <text
        x={PEN.x + PEN.w / 2}
        y={PEN.y + PEN.h + 18}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={8.5}
        letterSpacing="0.1em"
        fill={INK_DIM}
      >
        SERVICES
      </text>
      <text
        x={PEN.x + PEN.w / 2}
        y={PEN.y + PEN.h + 32}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={8.5}
        letterSpacing="0.1em"
        fill={INK_DIM}
      >
        NOT READ
      </text>
      <line
        x1={80}
        x2={944}
        y1={HORIZON_Y}
        y2={HORIZON_Y}
        stroke="rgba(245,241,234,0.22)"
        strokeDasharray="5 7"
      />
      <text
        x={100}
        y={HORIZON_Y - 16}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        BUILD TIME
      </text>
      <text
        x={100}
        y={HORIZON_Y + 26}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
        opacity={0.7}
      >
        RUNTIME
      </text>
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 7: gateway, distributed executor                               */
/* ------------------------------------------------------------------ */

const BEAT7 = { x: 764, y: 4800 } as const;
const CHIP7 = { x: 764, y: 5110 } as const;
const B7_TARGETS = [
  { x: 600, y: 5160 },
  { x: 680, y: 5180 },
  { x: 764, y: 5186 },
  { x: 848, y: 5180 },
  { x: 928, y: 5160 },
] as const;

function Beat7({ set }: { readonly set: ReturnType<typeof useVisual>["set"] }) {
  const port = { x: BEAT7.x, y: BEAT7.y + FRAME_H };
  return (
    <g>
      <line
        x1={port.x}
        y1={port.y}
        x2={CHIP7.x}
        y2={CHIP7.y - 13}
        stroke={GREY_CABLE}
        strokeWidth={2}
        strokeLinecap="round"
      />
      <Frame
        x={BEAT7.x}
        y={BEAT7.y}
        counter="07/07"
        idPrefix="ss-b7"
        slotOpacities={[1, 1, 1, 1, 1]}
      />
      <GatewayChip x={CHIP7.x} y={CHIP7.y} />
      {B7_TARGETS.map((t, i) => (
        <Fragment key={CANON[i].name}>
          <line
            x1={CHIP7.x}
            y1={CHIP7.y + 13}
            x2={t.x}
            y2={t.y}
            stroke={CANON[i].color}
            strokeOpacity={0.6}
            strokeDasharray="3 4"
          />
          <rect
            x={t.x - 8}
            y={t.y}
            width={16}
            height={16}
            rx={3}
            fill={CANON[i].color}
          />
        </Fragment>
      ))}
      <NodeCaption
        x={CHIP7.x - 70}
        y={CHIP7.y}
        label="Distributed executor"
        toX={CHIP7.x - 44}
      />
      <PulseGlyph
        set={set}
        id="ss-p7"
        main="#5eead4"
        soft="#c9f7ee"
        filter="ss-soft"
      />
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Mobile fallback: a small self-contained SVG per beat                */
/* ------------------------------------------------------------------ */

const MOBILE_SLOTS: readonly (readonly [
  number,
  number,
  number,
  number,
  number,
])[] = [
  [0.85, 0.85, 0.85, 0.85, 0.85],
  [0.85, 0.85, 0.85, 0.85, 0.85],
  [0.85, 0.85, 0.85, 0.85, 0.85],
  [0.9, 0.9, 0.25, 0.9, 0.25],
  [0.5, 0.5, 0.5, 0.5, 0.5],
  [0.12, 0.12, 0.12, 0.12, 0.12],
  [1, 1, 1, 1, 1],
];

function MobileFrame({ beat }: { readonly beat: number }) {
  const cx = 110;
  const top = 10;
  const scale = 0.62;
  const port = { x: cx, y: top + FRAME_H * scale };
  const counter = `0${beat + 1}/07`;

  return (
    <svg
      viewBox="0 0 220 210"
      aria-hidden="true"
      className="mx-auto mb-4 block w-full max-w-[220px] sm:hidden"
    >
      {beat === 1 && (
        <>
          <FrameGhost x={cx + 8} y={top + 8} opacity={0.3} />
        </>
      )}
      <Frame
        x={cx}
        y={top}
        scale={scale}
        counter={counter}
        idPrefix={`ss-m${beat}`}
        frameOpacity={beat === 4 ? 0.5 : beat === 5 ? 0.55 : 1}
        slotOpacities={MOBILE_SLOTS[beat]}
        captionOverride={beat === 5 ? "Screen off" : undefined}
      />
      {beat === 1 &&
        [-30, -12, 12, 30].map((dx) => (
          <line
            key={dx}
            x1={port.x}
            y1={port.y}
            x2={port.x + dx}
            y2={port.y + 26}
            stroke={CANON[dx < 0 ? 0 : 3].color}
            strokeOpacity={0.6}
            strokeWidth={1.5}
          />
        ))}
      {beat === 2 && (
        <line
          x1={port.x}
          y1={port.y}
          x2={port.x}
          y2={port.y + 22}
          stroke={GREY_CABLE}
          strokeWidth={1.5}
        />
      )}
      {beat === 3 && (
        <line
          x1={port.x}
          y1={port.y}
          x2={port.x}
          y2={port.y + 20}
          stroke={GREY_CABLE}
          strokeWidth={1.5}
        />
      )}
      {beat === 6 && (
        <line
          x1={port.x}
          y1={port.y}
          x2={port.x}
          y2={port.y + 20}
          stroke={GREY_CABLE}
          strokeWidth={1.5}
        />
      )}
    </svg>
  );
}

/* ------------------------------------------------------------------ */
/* Section                                                              */
/* ------------------------------------------------------------------ */

const BOX_LAYOUT: readonly {
  readonly top: number;
  readonly left: number;
  readonly width?: string;
}[][] = [
  [{ top: 190, left: 26, width: "sm:w-[min(30%,19rem)]" }],
  [{ top: 1420, left: 26, width: "sm:w-[min(30%,19rem)]" }],
  [],
  [{ top: 2050, left: 74, width: "sm:w-[min(30%,19rem)]" }],
  [
    { top: 3280, left: 27, width: "sm:w-[min(30%,19rem)]" },
    { top: 3280, left: 73, width: "sm:w-[min(30%,19rem)]" },
  ],
  [{ top: 4510, left: 50, width: "sm:w-[min(34%,20rem)]" }],
  [],
];

/**
 * Prototype v13: "The Same Screen" (round 2). Backbone is recurrence, not a
 * line: one 180x240 device-frame silhouette redrawn at every beat with an
 * etched `0N/07` counter, the port at its bottom edge standing in for
 * "what answers this port today". Difference lives entirely in the wiring
 * around the frame -- scatter, arced cables, a queue card, a socket chip, a
 * dashed empty sheet, sleep, and finally a gateway fan-out -- never in a
 * line that spans the whole canvas, so the 200-400px starfield between
 * vignettes reads as intentional silence.
 */
export function SameScreen() {
  const { rootRef, set } = useVisual(9000, (t, h) => {
    if (t < 4500) {
      const u = ramp(t, 400, 3800);
      if (u > 0 && u < 1) {
        h.placePulse(
          "ss-p2",
          B2_ARC,
          easeInOutCubic(u),
          Math.min(t / 200, 1) * (1 - ramp(t, 3600, 3800)),
          2.4,
        );
      } else {
        h.hidePulse("ss-p2");
      }
      h.hidePulse("ss-p7");
    } else {
      const u = ramp(t, 5000, 8400);
      if (u > 0 && u < 1) {
        h.placePulse(
          "ss-p7",
          measure([
            [BEAT7.x, BEAT7.y + FRAME_H],
            [CHIP7.x, CHIP7.y - 13],
          ]),
          easeInOutCubic(u),
          Math.min((t - 4500) / 200, 1) * (1 - ramp(t, 8200, 8400)),
          2.2,
        );
      } else {
        h.hidePulse("ss-p7");
      }
      h.hidePulse("ss-p2");
    }
  });

  return (
    <PageSection maxWidth="6xl">
      <div ref={rootRef} className="relative mx-auto w-full max-w-5xl">
        <div className="relative sm:aspect-[1024/5500]">
          <svg
            viewBox={`0 0 ${W} ${H}`}
            aria-hidden="true"
            className="absolute inset-0 z-0 hidden h-full w-full sm:block"
          >
            <defs>
              <filter id="ss-soft" x="-60%" y="-60%" width="220%" height="220%">
                <feGaussianBlur stdDeviation="2.2" />
              </filter>
            </defs>
            <g id="ss-beat-0">
              <Beat1 />
            </g>
            <g id="ss-beat-1">
              <Beat2 set={set} />
            </g>
            <g id="ss-beat-2">
              <Beat3 />
            </g>
            <g id="ss-beat-3">
              <Beat4 />
            </g>
            <g id="ss-beat-4">
              <Beat5 />
            </g>
            <g id="ss-beat-5">
              <Beat6 />
            </g>
            <g id="ss-beat-6">
              <Beat7 set={set} />
            </g>
          </svg>

          <div className="flex flex-col gap-14 px-5 py-16 sm:contents">
            {CHAPTERS.map((chapter, i) => (
              <Fragment key={i}>
                <MobileFrame beat={i} />
                <CopyBlock {...COPY_PLACEMENT[i]} title={chapter.title}>
                  {chapter.body}
                </CopyBlock>
                {chapter.boxes.map((box, j) => {
                  const layout = BOX_LAYOUT[i][j];
                  if (!layout) return null;
                  return (
                    <BoxSlot
                      key={j}
                      top={layout.top}
                      left={layout.left}
                      width={layout.width}
                    >
                      <ProtoCodeBox
                        label={box.label}
                        color={box.color}
                        lines={box.lines}
                      />
                    </BoxSlot>
                  );
                })}
                {i === 5 && (
                  <div className="sm:hidden">
                    <HorizonRule />
                  </div>
                )}
              </Fragment>
            ))}
          </div>
        </div>
      </div>
    </PageSection>
  );
}
