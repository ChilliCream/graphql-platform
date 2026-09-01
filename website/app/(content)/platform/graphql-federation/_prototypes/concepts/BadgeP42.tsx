import type { CSSProperties, ReactNode } from "react";
import { Fragment } from "react";

import { PageSection } from "@/src/components/PageSection";

import {
  CANON,
  GatewayChip,
  INK_DIM,
  SchemaCard,
  schemaRowY,
} from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";

const W = 1024;
const H = 6500;
const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";
const CHROME_STROKE = "rgba(245,241,234,0.16)";
const CHROME_FILL = "rgba(12,19,34,0.6)";
const CORAL = "#f27765";
const TEAL = "#5eead4";
const HORIZON_Y = 5400;

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

function placement(top: number, left: number): CSSProperties {
  return { "--top": pct(top, H), "--left": `${left}%` } as CSSProperties;
}

const SCRIM =
  "radial-gradient(ellipse 62% 58% at 50% 50%, rgba(11,15,26,0.98) 0%, rgba(11,15,26,0.94) 50%, rgba(11,15,26,0.6) 76%, rgba(11,15,26,0) 93%)";

interface CopyPlacement {
  readonly top: number;
  readonly left: number;
  readonly width: string;
}

/**
 * The seven chapter copy blocks. Beats 1-4 sit opposite the badge's
 * horizontal position at the same y (proven disjoint-by-column pattern from
 * SameScreen); beats 5-7 have artwork spanning most of the width, so their
 * copy sits in the narrow margin that remains, measured and retuned against
 * real screenshots per the placement guard.
 */
const COPY_PLACEMENT: readonly CopyPlacement[] = [
  { top: 650, left: 78, width: "sm:w-[min(38%,25rem)]" },
  { top: 1340, left: 22, width: "sm:w-[min(38%,25rem)]" },
  { top: 1950, left: 78, width: "sm:w-[min(38%,25rem)]" },
  { top: 2380, left: 22, width: "sm:w-[min(38%,25rem)]" },
  { top: 3050, left: 50, width: "sm:w-[min(56%,34rem)]" },
  { top: 4250, left: 50, width: "sm:w-[min(60%,36rem)]" },
  { top: 5700, left: 50, width: "sm:w-[min(50%,30rem)]" },
];

interface CopyBlockProps extends CopyPlacement {
  readonly title: string;
  readonly children: ReactNode;
}

function CopyBlock({ top, left, width, title, children }: CopyBlockProps) {
  return (
    <div
      className={`relative w-full text-center sm:absolute sm:top-(--top) sm:left-(--left) sm:z-20 sm:-translate-x-1/2 sm:-translate-y-1/2 sm:text-left ${width}`}
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
  width = "sm:w-[min(28%,17rem)]",
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
/* The badge: the one recurring object, redrawn identically everywhere */
/* ------------------------------------------------------------------ */

interface BadgeProps {
  readonly x: number;
  readonly y: number;
  readonly scale?: number;
  readonly opacity?: number;
  readonly haloOpacity?: number;
  readonly id: string;
}

function Badge({
  x,
  y,
  scale = 1,
  opacity = 1,
  haloOpacity = 0.5,
  id,
}: BadgeProps) {
  const w = 64 * scale;
  const h = 24 * scale;
  const left = x - w / 2;
  const top = y - h / 2;
  return (
    <g opacity={opacity}>
      <defs>
        <radialGradient id={`${id}-halo`} cx="50%" cy="50%" r="50%">
          <stop offset="0.08" stopColor="#fff" stopOpacity="0.45" />
          <stop offset="0.5" stopColor="#fff" stopOpacity="0.1" />
          <stop offset="1" stopColor="#0e1522" stopOpacity="0" />
        </radialGradient>
      </defs>
      <circle
        cx={x}
        cy={y}
        r={40 * scale}
        fill={`url(#${id}-halo)`}
        opacity={haloOpacity}
      />
      <rect
        x={left}
        y={top}
        width={w}
        height={h}
        rx={12 * scale}
        fill="#0d1424"
        stroke="rgba(245,241,234,0.28)"
        strokeWidth={1.25}
      />
      <circle cx={left + 14 * scale} cy={y} r={3 * scale} fill="#fff" />
      <text
        x={left + 26 * scale}
        y={y + 4 * scale}
        fontFamily={MONO}
        fontSize={11 * scale}
        letterSpacing="0.03em"
        fill="#c9d4e8"
      >
        P-42
      </text>
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 1: five facts, five owners, scattered, connected to nothing    */
/* ------------------------------------------------------------------ */

const BEAT1 = { x: 310, y: 650 } as const;
const FACTS = [
  { dx: -155, dy: -75, label: "NAME" },
  { dx: 150, dy: -100, label: "PRICE" },
  { dx: -185, dy: 55, label: "ORDERS" },
  { dx: 5, dy: 125, label: "DELIVERY" },
  { dx: 175, dy: 75, label: "ACCOUNT" },
] as const;

function FactChip({
  x,
  y,
  color,
  label,
}: {
  readonly x: number;
  readonly y: number;
  readonly color: string;
  readonly label: string;
}) {
  const w = 110;
  const h = 22;
  return (
    <g transform={`translate(${x - w / 2} ${y - h / 2})`}>
      <rect
        width={w}
        height={h}
        rx={8}
        fill={CHROME_FILL}
        stroke={CHROME_STROKE}
      />
      <rect x={8} y={h / 2 - 4} width={8} height={8} rx={2} fill={color} />
      <text
        x={24}
        y={h / 2 + 3}
        fontFamily={MONO}
        fontSize={8.5}
        letterSpacing="0.08em"
        fill="#c9d4e8"
      >
        {label}
      </text>
    </g>
  );
}

function Beat1() {
  return (
    <g>
      <Badge x={BEAT1.x} y={BEAT1.y} id="bp-b1" />
      {FACTS.map((f, i) => (
        <FactChip
          key={f.label}
          x={BEAT1.x + f.dx}
          y={BEAT1.y + f.dy}
          color={CANON[i].color}
          label={f.label}
        />
      ))}
      <text
        x={BEAT1.x}
        y={BEAT1.y + 210}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={10}
        letterSpacing="0.16em"
        fill={INK_DIM}
      >
        FIVE FACTS · FIVE OWNERS
      </text>
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 2: the badge photocopied into every app's request              */
/* ------------------------------------------------------------------ */

const BEAT2 = { x: 715, y: 1300 } as const;
const REQUEST_ROWS = [
  "GET /products",
  "GET /prices",
  "GET /orders",
  "GET /shipping",
  "GET /account",
] as const;

function RequestChip({
  x,
  y,
  color,
  text,
}: {
  readonly x: number;
  readonly y: number;
  readonly color: string;
  readonly text: string;
}) {
  const w = 190;
  const h = 26;
  return (
    <g transform={`translate(${x - w / 2} ${y - h / 2})`}>
      <rect
        width={w}
        height={h}
        rx={8}
        fill={CHROME_FILL}
        stroke={CHROME_STROKE}
      />
      <rect x={8} y={h / 2 - 3} width={6} height={6} rx={2} fill={color} />
      <text
        x={20}
        y={h / 2 + 3.5}
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.02em"
        fill="#c9d4e8"
      >
        {text}
      </text>
      <g transform={`translate(${w - 66} ${h / 2})`}>
        <Badge
          x={0}
          y={0}
          scale={0.5}
          haloOpacity={0.35}
          id={`bp-b2-${text}`}
        />
      </g>
    </g>
  );
}

function Beat2() {
  const pitch = 34;
  const top = BEAT2.y - pitch * 2;
  return (
    <g>
      <rect
        x={BEAT2.x - 95 + 14}
        y={top - 14 + 14}
        width={190}
        height={pitch * 4 + 26}
        rx={12}
        fill="none"
        stroke="rgba(245,241,234,0.22)"
        opacity={0.35}
      />
      <rect
        x={BEAT2.x - 95 + 28}
        y={top - 14 + 28}
        width={190}
        height={pitch * 4 + 26}
        rx={12}
        fill="none"
        stroke="rgba(245,241,234,0.14)"
        opacity={0.3}
      />
      {REQUEST_ROWS.map((text, i) => (
        <RequestChip
          key={text}
          x={BEAT2.x}
          y={top + i * pitch}
          color={CANON[i].color}
          text={text}
        />
      ))}
      <text
        x={BEAT2.x}
        y={BEAT2.y + 165}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={10}
        letterSpacing="0.14em"
        fill={INK_DIM}
      >
        EVERY APP CARRIES THE ID FIVE TIMES
      </text>
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 3: even the id waits in the queue                              */
/* ------------------------------------------------------------------ */

const BEAT3 = { x: 310, y: 1950 } as const;
const TICKETS = [
  { label: "ADD delivery TO PRODUCT", held: true },
  { label: "RENAME price FIELD", held: false },
  { label: "SPLIT account TYPE", held: false },
] as const;

function Beat3() {
  const cardW = 240;
  const cardH = 64;
  const baseX = BEAT3.x - cardW / 2;
  const baseY = BEAT3.y - 40;
  return (
    <g>
      <text
        x={BEAT3.x}
        y={baseY - 24}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9.5}
        letterSpacing="0.16em"
        fill={INK_DIM}
      >
        API TEAM
      </text>
      {[2, 1, 0].map((i) => {
        const offset = i * 10;
        const opacity = i === 0 ? 1 : i === 1 ? 0.5 : 0.25;
        return (
          <g
            key={i}
            transform={`translate(${baseX + offset} ${baseY + offset})`}
            opacity={opacity}
          >
            <rect
              width={cardW}
              height={cardH}
              rx={12}
              fill={CHROME_FILL}
              stroke={CHROME_STROKE}
            />
            {i === 0 && (
              <>
                <Badge x={30} y={20} scale={0.7} haloOpacity={0.3} id="bp-b3" />
                <text
                  x={58}
                  y={20 + 3}
                  fontFamily={MONO}
                  fontSize={8}
                  letterSpacing="0.02em"
                  fill="#c9d4e8"
                >
                  CHANGE REQUEST #14
                </text>
                <text
                  x={12}
                  y={44}
                  fontFamily={MONO}
                  fontSize={8}
                  letterSpacing="0.02em"
                  fill={INK_DIM}
                >
                  {TICKETS[0].label}
                </text>
                <rect
                  x={cardW - 56}
                  y={cardH - 26}
                  width={42}
                  height={16}
                  rx={8}
                  fill="none"
                  stroke={CORAL}
                  strokeOpacity={0.6}
                />
                <text
                  x={cardW - 35}
                  y={cardH - 15}
                  textAnchor="middle"
                  fontFamily={MONO}
                  fontSize={7.5}
                  letterSpacing="0.08em"
                  fill={CORAL}
                >
                  HELD
                </text>
              </>
            )}
          </g>
        );
      })}
      <text
        x={BEAT3.x}
        y={baseY + cardH + 46}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={10}
        letterSpacing="0.14em"
        fill={INK_DIM}
      >
        EVEN THE ID WAITS
      </text>
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 4: one badge, in the argument slot; a response opposite        */
/* ------------------------------------------------------------------ */

const BEAT4 = { x: 715, y: 2380 } as const;
const RESPONSE_FACTS = [
  { label: "name", color: CANON[0].color },
  { label: "price", color: CANON[1].color },
  { label: "delivery", color: CANON[3].color },
] as const;

function Beat4() {
  const cardW = 168;
  const cardH = 96;
  const cardX = BEAT4.x - cardW / 2;
  const cardY = BEAT4.y - 30;
  return (
    <g>
      <text
        x={BEAT4.x}
        y={BEAT4.y - 90}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.14em"
        fill={INK_DIM}
        opacity={0.8}
      >
        ARGUMENT · id: &quot;P-42&quot;
      </text>
      <Badge x={BEAT4.x} y={BEAT4.y - 60} id="bp-b4" haloOpacity={0.55} />
      <rect
        x={cardX}
        y={cardY}
        width={cardW}
        height={cardH}
        rx={12}
        fill={CHROME_FILL}
        stroke={CHROME_STROKE}
      />
      <text
        x={cardX + 12}
        y={cardY + 18}
        fontFamily={MONO}
        fontSize={8.5}
        letterSpacing="0.14em"
        fill={INK_DIM}
      >
        RESPONSE
      </text>
      {RESPONSE_FACTS.map((f, i) => (
        <g
          key={f.label}
          transform={`translate(${cardX + 12} ${cardY + 36 + i * 20})`}
        >
          <circle r={3} cy={-3} fill={f.color} />
          <text
            x={12}
            y={0}
            fontFamily={MONO}
            fontSize={9}
            letterSpacing="0.04em"
            fill="#c9d4e8"
          >
            {f.label}
          </text>
        </g>
      ))}
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 5: no card knows all the fields, every card knows the same id  */
/* ------------------------------------------------------------------ */

const MINI_W = 130;
const MINI_CENTERS = [
  { x: 100, y: 3430 },
  { x: 220, y: 3460 },
  { x: 340, y: 3440 },
  { x: 460, y: 3465 },
  { x: 580, y: 3445 },
] as const;
const MINI_FIELD = [
  "name: String!",
  "price: Money!",
  "orders: [Order!]",
  "delivery: String!",
  "account: User!",
] as const;

function Beat5() {
  return (
    <g>
      {MINI_CENTERS.map((c, i) => {
        const top = c.y - 62;
        const left = c.x - MINI_W / 2;
        return (
          <g key={CANON[i].name}>
            <SchemaCard
              x={left}
              y={top}
              w={MINI_W}
              label={CANON[i].name}
              color={CANON[i].color}
              file=""
              lines={[
                { code: "id: ID!" },
                { code: MINI_FIELD[i], dim: true },
                { code: "…", dim: true },
              ]}
            />
            <g opacity={0.35}>
              <Badge
                x={left + MINI_W - 22}
                y={schemaRowY(top, 0) - 4}
                scale={0.45}
                haloOpacity={0.15}
                id={`bp-b5-${CANON[i].name}`}
              />
            </g>
          </g>
        );
      })}
      <text
        x={340}
        y={3600}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9.5}
        letterSpacing="0.12em"
        fill={INK_DIM}
        opacity={0.8}
      >
        EVERY TEAM KNOWS THE SAME ID · NO CARD KNOWS ALL THE FIELDS
      </text>
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 6: composed from schemas, joined by the key -- no strokes      */
/* ------------------------------------------------------------------ */

const ARC_CENTERS = [
  { x: 180, y: 4730 },
  { x: 330, y: 4710 },
  { x: 480, y: 4700 },
  { x: 630, y: 4710 },
  { x: 780, y: 4730 },
] as const;
const ARC_W = 130;
const PEN = { x: 930, y: 4720, w: 90, h: 190 } as const;

function UnisonRow({ x, y }: { readonly x: number; readonly y: number }) {
  return (
    <rect
      x={x - 75}
      y={y - 8}
      width={150}
      height={16}
      rx={5}
      fill={TEAL}
      opacity={0.1}
      className="bp42-unison"
    />
  );
}

function Beat6() {
  return (
    <g>
      {ARC_CENTERS.map((c, i) => {
        const top = c.y - 62;
        const left = c.x - ARC_W / 2;
        const idRowY = schemaRowY(top, 0);
        return (
          <g key={CANON[i].name}>
            <UnisonRow x={c.x} y={idRowY - 4} />
            <SchemaCard
              x={left}
              y={top}
              w={ARC_W}
              label={CANON[i].name}
              color={CANON[i].color}
              file=""
              lines={[
                { code: "id: ID!" },
                { code: "…", dim: true },
                { code: "…", dim: true },
              ]}
            />
          </g>
        );
      })}
      <g transform={`translate(${(180 + 780) / 2} 4900)`}>
        <rect
          x={-90}
          y={-14}
          width={180}
          height={28}
          rx={14}
          fill="rgba(12,19,34,0.6)"
          stroke="rgba(94,234,212,0.45)"
        />
        <text
          x={0}
          y={4}
          textAnchor="middle"
          fontFamily={MONO}
          fontSize={9}
          letterSpacing="0.1em"
          fill={TEAL}
        >
          @KEY · THE ROW THEY SHARE
        </text>
      </g>
      <text
        x={480}
        y={4955}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={10}
        letterSpacing="0.12em"
        fill={INK_DIM}
      >
        COMPOSED FROM SCHEMAS · JOINED BY THE KEY
      </text>
      <rect
        x={PEN.x}
        y={PEN.y}
        width={PEN.w}
        height={PEN.h}
        rx={14}
        fill="none"
        stroke="rgba(245,241,234,0.3)"
        strokeDasharray="4 5"
      />
      {CANON.map((service, i) => (
        <rect
          key={service.name}
          x={PEN.x + PEN.w / 2 - 8}
          y={PEN.y + 18 + i * 32}
          width={16}
          height={16}
          rx={3}
          fill={service.color}
          opacity={0.55}
        />
      ))}
      <text
        x={PEN.x + PEN.w / 2}
        y={PEN.y + PEN.h + 20}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={8.5}
        letterSpacing="0.06em"
        fill={INK_DIM}
      >
        SERVICES
      </text>
      <text
        x={PEN.x + PEN.w / 2}
        y={PEN.y + PEN.h + 34}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={8.5}
        letterSpacing="0.06em"
        fill={INK_DIM}
        opacity={0.75}
      >
        NOT READ
      </text>
      <text
        x={PEN.x + PEN.w / 2}
        y={PEN.y + PEN.h + 48}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={8.5}
        letterSpacing="0.06em"
        fill={INK_DIM}
        opacity={0.75}
      >
        SEPARATE DEPLOYS
      </text>
      <line
        x1={60}
        x2={964}
        y1={HORIZON_Y}
        y2={HORIZON_Y}
        stroke="rgba(245,241,234,0.22)"
        strokeDasharray="5 7"
      />
      <text
        x={80}
        y={HORIZON_Y - 16}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        BUILD TIME
      </text>
      <text
        x={80}
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
/* Beat 7: one id, three lookups, one response                        */
/* ------------------------------------------------------------------ */

const CHIP7 = { x: 512, y: 5950 } as const;
const GATEWAY7 = { x: 512, y: 6070 } as const;
const LOOKUP_Y = 6160;
const LOOKUPS = [
  { x: 416, service: 0 },
  { x: 512, service: 1 },
  { x: 608, service: 3 },
] as const;
const DIM_SQUARES = [
  { x: 300, service: 2 },
  { x: 724, service: 4 },
] as const;
const RESPONSE_Y = 6250;

function LookupChit({
  x,
  y,
  color,
}: {
  readonly x: number;
  readonly y: number;
  readonly color: string;
}) {
  const w = 150;
  const h = 26;
  return (
    <g transform={`translate(${x - w / 2} ${y - h / 2})`}>
      <rect
        width={w}
        height={h}
        rx={8}
        fill={CHROME_FILL}
        stroke={CHROME_STROKE}
      />
      <rect x={8} y={h / 2 - 3} width={6} height={6} rx={2} fill={color} />
      <text
        x={20}
        y={h / 2 + 3.5}
        fontFamily={MONO}
        fontSize={8}
        letterSpacing="0.01em"
        fill="#c9d4e8"
      >
        productById
      </text>
      <g transform={`translate(${w - 42} ${h / 2})`}>
        <Badge
          x={0}
          y={0}
          scale={0.4}
          haloOpacity={0.25}
          id={`bp-b7-${color}`}
        />
      </g>
    </g>
  );
}

function Beat7() {
  return (
    <g>
      <g transform={`translate(${CHIP7.x - 90} ${CHIP7.y})`}>
        <rect
          x={0}
          y={-13}
          width={180}
          height={26}
          rx={8}
          fill={CHROME_FILL}
          stroke={CHROME_STROKE}
        />
        <text
          x={90}
          y={4}
          textAnchor="middle"
          fontFamily={MONO}
          fontSize={8.5}
          letterSpacing="0.04em"
          fill="#c9d4e8"
        >
          QUERY · productById
        </text>
      </g>
      <line
        x1={CHIP7.x}
        y1={CHIP7.y + 13}
        x2={GATEWAY7.x}
        y2={GATEWAY7.y - 13}
        stroke="rgba(139,160,188,0.55)"
        strokeWidth={2}
        strokeLinecap="round"
      />
      <GatewayChip x={GATEWAY7.x} y={GATEWAY7.y} w={120} />
      {LOOKUPS.map((l) => (
        <Fragment key={l.x}>
          <line
            x1={GATEWAY7.x}
            y1={GATEWAY7.y + 13}
            x2={l.x}
            y2={LOOKUP_Y - 13}
            stroke={CANON[l.service].color}
            strokeOpacity={0.55}
            strokeDasharray="3 4"
          />
          <LookupChit x={l.x} y={LOOKUP_Y} color={CANON[l.service].color} />
        </Fragment>
      ))}
      {DIM_SQUARES.map((d) => (
        <rect
          key={d.x}
          x={d.x - 8}
          y={LOOKUP_Y - 8}
          width={16}
          height={16}
          rx={3}
          fill={CANON[d.service].color}
          opacity={0.22}
        />
      ))}
      <g transform={`translate(${GATEWAY7.x} ${RESPONSE_Y})`}>
        <Badge x={-110} y={0} scale={0.65} haloOpacity={0.4} id="bp-b7-resp" />
        {RESPONSE_FACTS.map((f, i) => (
          <g key={f.label} transform={`translate(${-40 + i * 78} 0)`}>
            <circle r={3} cy={-2} fill={f.color} />
            <text
              x={8}
              y={2}
              fontFamily={MONO}
              fontSize={8.5}
              letterSpacing="0.02em"
              fill="#c9d4e8"
            >
              {f.label}
            </text>
          </g>
        ))}
      </g>
      <text
        x={GATEWAY7.x}
        y={RESPONSE_Y + 40}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={10}
        letterSpacing="0.12em"
        fill={INK_DIM}
      >
        ONE ID, THREE LOOKUPS, ONE RESPONSE
      </text>
    </g>
  );
}

/* ------------------------------------------------------------------ */
/* Mobile identity strip: a tiny badge + the beat's own caption        */
/* ------------------------------------------------------------------ */

const MOBILE_CAPTIONS = [
  "Five facts, five owners",
  "Every app carries the id five times",
  "Even the id waits",
  "One argument, one response",
  "Every team knows the same id",
  "Composed from schemas, joined by the key",
  "One id, three lookups, one response",
] as const;

function MobileBadgeStrip({ beat }: { readonly beat: number }) {
  return (
    <div className="mb-4 flex items-center justify-center gap-3 sm:hidden">
      <svg viewBox="0 0 64 24" aria-hidden="true" className="h-6 w-16 shrink-0">
        <Badge x={32} y={12} id={`bp-mobile-${beat}`} haloOpacity={0.4} />
      </svg>
      <span className="text-cc-nav-label font-mono text-[9px] tracking-[0.14em] uppercase">
        {MOBILE_CAPTIONS[beat]}
      </span>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Section                                                             */
/* ------------------------------------------------------------------ */

const BOX_LAYOUT: readonly {
  readonly top: number;
  readonly left: number;
  readonly width?: string;
}[][] = [
  [{ top: 1010, left: 26, width: "sm:w-[min(28%,17rem)]" }],
  [{ top: 1620, left: 74, width: "sm:w-[min(28%,17rem)]" }],
  [],
  [{ top: 2650, left: 74, width: "sm:w-[min(30%,19rem)]" }],
  [
    { top: 3800, left: 27, width: "sm:w-[min(28%,17rem)]" },
    { top: 3800, left: 73, width: "sm:w-[min(28%,17rem)]" },
  ],
  [{ top: 5150, left: 50, width: "sm:w-[min(34%,20rem)]" }],
  [],
];

/**
 * Prototype v16: "Badge P-42" (round 2). The recurring object is the
 * smallest one possible -- a 64x24 identity pill -- redrawn identically at
 * every beat, in a new role each time. Beat 6 draws the join as sameness (an
 * identical glowing id row shared by five still-separate schema cards and
 * the composite) rather than convergence: no connecting strokes anywhere in
 * that beat, and services appear only inside a labelled "not read" pen.
 */
export function BadgeP42() {
  return (
    <PageSection maxWidth="6xl">
      <div className="relative mx-auto w-full max-w-5xl">
        <style>{`
          @media (prefers-reduced-motion: no-preference) {
            .bp42-unison { animation: bp42-breathe 4s ease-in-out infinite; }
          }
          @keyframes bp42-breathe {
            0%, 100% { opacity: 0.06; }
            50% { opacity: 0.16; }
          }
        `}</style>
        <div className="relative sm:aspect-[1024/6500]">
          <svg
            viewBox={`0 0 ${W} ${H}`}
            aria-hidden="true"
            className="absolute inset-0 z-0 hidden h-full w-full sm:block"
          >
            <g id="bp42-beat-1">
              <Beat1 />
            </g>
            <g id="bp42-beat-2">
              <Beat2 />
            </g>
            <g id="bp42-beat-3">
              <Beat3 />
            </g>
            <g id="bp42-beat-4">
              <Beat4 />
            </g>
            <g id="bp42-beat-5">
              <Beat5 />
            </g>
            <g id="bp42-beat-6">
              <Beat6 />
            </g>
            <g id="bp42-beat-7">
              <Beat7 />
            </g>
          </svg>

          <div className="flex flex-col gap-14 px-5 py-16 sm:contents">
            {CHAPTERS.map((chapter, i) => (
              <Fragment key={i}>
                <MobileBadgeStrip beat={i} />
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
              </Fragment>
            ))}
          </div>
        </div>
      </div>
    </PageSection>
  );
}
