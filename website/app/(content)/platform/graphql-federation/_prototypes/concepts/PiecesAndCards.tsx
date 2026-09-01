"use client";

import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

import {
  CANON,
  GatewayChip,
  INK_DIM,
  MicroLabel,
  SchemaCard,
  schemaRowY,
} from "../Primitives";
import { CHAPTERS } from "../story";

const TEAL = "#5eead4";
const CARD_FILL = "#0d1424";
const CARD_STROKE = "rgba(245,241,234,0.16)";
const GHOST_STROKE = "rgba(245,241,234,0.2)";
const VB_W = 680;
const VB_H = 300;

/** Home row: the five pieces spread across the table, untouched. */
const HOME: readonly { readonly x: number; readonly y: number }[] = [
  { x: 90, y: 244 },
  { x: 235, y: 244 },
  { x: 380, y: 244 },
  { x: 525, y: 244 },
  { x: 610, y: 176 },
];

/**
 * Beat 5/6 shared layout: each piece sits under its own card slot. The
 * slots vary in width (wide for the two readable schema cards, narrow for
 * the three ghost sheets) so the readable ones have enough room for their
 * longest line without their text spilling into the neighboring card.
 */
const CARD_SLOTS: readonly { readonly x: number; readonly w: number }[] = [
  { x: 6, w: 190 },
  { x: 210, w: 190 },
  { x: 410, w: 84 },
  { x: 504, w: 84 },
  { x: 598, w: 76 },
];
const CARD_ROW_Y = 20;

/** Piece row for beats 5/6, centered under each card slot above it. */
const SIDE_BY_SIDE: readonly { readonly x: number; readonly y: number }[] =
  CARD_SLOTS.map((slot) => ({ x: slot.x + slot.w / 2, y: 272 }));

const NAMEPLATES: readonly string[] = [
  "01 — One screen, five pieces",
  "02 — Every app deals its own hand",
  "03 — One card, one owner, one line",
  "04 — The client sees one card",
  "05 — Five hands, no dealer",
  "06 — Cards stack · pieces don't",
  "07 — One card in front, five pieces in play",
];

function Nameplate({ children }: { readonly children: string }) {
  return (
    <p className="text-cc-nav-label mt-3 text-center font-mono text-[10px] tracking-[0.2em] uppercase">
      {children}
    </p>
  );
}

/** A service token: the site's rounded square, drawn as a small SVG chip. */
function Piece({
  x,
  y,
  i,
  opacity = 1,
}: {
  readonly x: number;
  readonly y: number;
  readonly i: number;
  readonly opacity?: number;
}) {
  const service = CANON[i];
  return (
    <g opacity={opacity}>
      <ellipse cx={x} cy={y + 11} rx={11} ry={3} fill="rgba(0,0,0,0.35)" />
      <rect
        x={x - 9}
        y={y - 9}
        width={18}
        height={18}
        rx={5}
        fill={service.color}
        stroke={service.soft}
        strokeWidth={1}
      />
      <text
        x={x}
        y={y + 26}
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize={9}
        letterSpacing="0.08em"
        fill={INK_DIM}
      >
        {service.name}
      </text>
    </g>
  );
}

/** A plain document card: thin-stroke sheet, optionally holding a title line. */
function Card({
  x,
  y,
  w,
  h,
  title,
  rotate = 0,
  opacity = 1,
}: {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly title?: string;
  readonly rotate?: number;
  readonly opacity?: number;
}) {
  return (
    <g
      opacity={opacity}
      transform={
        rotate ? `rotate(${rotate} ${x + w / 2} ${y + h / 2})` : undefined
      }
    >
      <rect
        x={x}
        y={y}
        width={w}
        height={h}
        rx={6}
        fill={CARD_FILL}
        stroke={CARD_STROKE}
      />
      {title && (
        <text
          x={x + w / 2}
          y={y + h / 2 + 3}
          textAnchor="middle"
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize={10}
          letterSpacing="0.04em"
          fill="#c9d4e8"
        >
          {title}
        </text>
      )}
    </g>
  );
}

/** Dashed outline where a card used to lie, or where one has not arrived yet. */
function GhostCard({
  x,
  y,
  w,
  h,
  pulse = false,
}: {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly h: number;
  readonly pulse?: boolean;
}) {
  return (
    <rect
      x={x}
      y={y}
      width={w}
      height={h}
      rx={6}
      fill="none"
      stroke={GHOST_STROKE}
      strokeDasharray="4 5"
      className={
        pulse
          ? "motion-safe:animate-[pulse_4s_ease-in-out_infinite] motion-reduce:animate-none"
          : undefined
      }
    />
  );
}

/** The soft-lit table vignette shell every beat renders inside. */
function Vignette({
  children,
  label,
}: {
  readonly children: React.ReactNode;
  readonly label: string;
}) {
  return (
    <div
      className="border-cc-card-border overflow-x-auto rounded-2xl border p-3"
      style={{
        background:
          "radial-gradient(ellipse 70% 65% at 50% 55%, rgba(12,19,34,0.9), rgba(12,19,34,0) 75%)",
      }}
    >
      <svg
        viewBox={`0 0 ${VB_W} ${VB_H}`}
        role="img"
        aria-label={label}
        className="h-auto w-full min-w-[560px] lg:min-w-0"
      >
        {children}
      </svg>
    </div>
  );
}

/** Beat 1 — five pieces spread, a screen card with five color-tinted rows. */
function Beat1() {
  return (
    <Vignette label="Five pieces spread around a five-field screen card">
      <SchemaCard
        x={VB_W / 2 - 100}
        y={30}
        w={200}
        label="Product page"
        color={CANON[0].color}
        file="one screen"
        lines={CANON.map((s) => ({ code: s.name.toLowerCase() }))}
      />
      {HOME.map((p, i) => (
        <Piece key={CANON[i].name} x={p.x} y={p.y} i={i} />
      ))}
    </Vignette>
  );
}

const REQUESTS = [
  "GET /products/P-42",
  "GET /prices/P-42",
  "GET /orders?product=P-42",
  "GET /shipping/P-42",
  "GET /account",
];

/** Beat 2 pieces: a radial pile around P=(340,150). */
const BEAT2_PIECES: readonly { readonly x: number; readonly y: number }[] = [
  { x: 219, y: 80 },
  { x: 200, y: 150 },
  { x: 219, y: 220 },
  { x: 461, y: 80 },
  { x: 461, y: 220 },
];

/** Beat 2 slips: five fanned request slips, far ends touching their piece rims. */
const BEAT2_SLIPS: readonly {
  readonly x: number;
  readonly y: number;
  readonly rot: number;
}[] = [
  { x: 219, y: 111, rot: 30 },
  { x: 211, y: 138, rot: 0 },
  { x: 219, y: 165, rot: -30 },
  { x: 312, y: 111, rot: -30 },
  { x: 312, y: 165, rot: 30 },
];

/** Beat 2 — a hand-assembled pile: five slips fanned toward each piece. */
function Beat2() {
  return (
    <Vignette label="A screen card buried under five fanned request slips">
      <Card x={280} y={128} w={120} h={44} opacity={0.5} />
      {BEAT2_SLIPS.map((s, i) => (
        <Card
          key={REQUESTS[i]}
          x={s.x}
          y={s.y}
          w={150}
          h={24}
          title={REQUESTS[i]}
          rotate={s.rot}
        />
      ))}
      {BEAT2_PIECES.map((p, i) => (
        <Piece key={CANON[i].name} x={p.x} y={p.y} i={i} />
      ))}
    </Vignette>
  );
}

/** Beat 3 — one big API card; pieces pushed single-file behind a queue mark. */
function Beat3() {
  const qx = 560;
  return (
    <Vignette label="One oversized API card with the five pieces queued behind it">
      <Card x={100} y={120} w={360} h={64} title="ONE BIG API" rotate={0} />
      <text
        x={qx}
        y={70}
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize={10}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        QUEUE
      </text>
      <line
        x1={qx}
        x2={qx}
        y1={80}
        y2={264}
        stroke={GHOST_STROKE}
        strokeDasharray="2 4"
      />
      {CANON.map((_, i) => (
        <Piece key={CANON[i].name} x={qx} y={92 + i * 40} i={i} />
      ))}
    </Vignette>
  );
}

/** Beat 4 — one query, one response, squared edge to edge; pieces near the rim. */
function Beat4() {
  const cx = VB_W / 2;
  const y = 130;
  return (
    <Vignette label="A query card and a response card squared together, pieces dimmed at the rim">
      <Card x={cx - 130} y={y} w={130} h={64} title="query" />
      <Card x={cx} y={y} w={130} h={64} title="response" />
      {HOME.map((p, i) => (
        <Piece key={CANON[i].name} x={p.x} y={26} i={i} opacity={0.3} />
      ))}
    </Vignette>
  );
}

const KEY_LINE = '@key(fields: "id")';

/**
 * Catalog's and Billing's schema cards, condensed to fit their slot width:
 * the type/@key header wraps onto its own line (a common source-schema
 * style) and one representative field stands in for the full set, so the
 * longest line stays well inside the 190px-wide slot instead of spilling
 * into the neighboring card.
 */
const CATALOG_LINES = [
  { code: "" },
  { code: `  ${KEY_LINE} {` },
  { code: "  name: String!" },
  { code: "}" },
];
const BILLING_LINES = [
  { code: "" },
  { code: `  ${KEY_LINE} {` },
  { code: "  price: Money!" },
  { code: "}" },
];

/** Overlays the teal `@key` line onto a Beat 5 schema card at the given slot x. */
function KeyRow({ x }: { readonly x: number }) {
  return (
    <text
      x={x + 16}
      y={schemaRowY(CARD_ROW_Y, 1)}
      xmlSpace="preserve"
      fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
      fontSize={12}
      fill="#c9d4e8"
    >
      {"  "}
      <tspan fill={TEAL}>{KEY_LINE}</tspan>
      {" {"}
    </text>
  );
}

/** Beat 5 — each piece with its own schema card; a dashed empty card at center. */
function Beat5() {
  const ch5 = CHAPTERS[4];
  const [catalogBox, billingBox] = ch5.boxes;
  return (
    <Vignette label="Five pieces each with their own schema card, one dashed and empty at center">
      <SchemaCard
        x={CARD_SLOTS[0].x}
        y={CARD_ROW_Y}
        w={CARD_SLOTS[0].w}
        label={catalogBox.label}
        color={CANON[0].color}
        file=""
        lines={CATALOG_LINES}
      />
      <KeyRow x={CARD_SLOTS[0].x} />
      <SchemaCard
        x={CARD_SLOTS[1].x}
        y={CARD_ROW_Y}
        w={CARD_SLOTS[1].w}
        label={billingBox.label}
        color={CANON[1].color}
        file=""
        lines={BILLING_LINES}
      />
      <KeyRow x={CARD_SLOTS[1].x} />
      {[2, 3, 4].map((i) => (
        <GhostCard
          key={CANON[i].name}
          x={CARD_SLOTS[i].x}
          y={CARD_ROW_Y}
          w={CARD_SLOTS[i].w}
          h={108}
        />
      ))}
      <GhostCard x={256} y={155} w={84} h={70} />
      {SIDE_BY_SIDE.map((p, i) => (
        <Piece key={CANON[i].name} x={p.x} y={p.y} i={i} />
      ))}
    </Vignette>
  );
}

/**
 * Beat 6 — the schemas squared into one stack; pieces unmoved beside their
 * ghosts. The ghost outlines sit exactly where beat 5's per-piece cards lay
 * (same `CARD_SLOTS`); the stack itself moves to a fresh spot below that
 * row, so it never has to sit on top of a ghost it is supposed to be
 * distinct from. Only two representative lines (the header and the
 * ownership-dotted id row) are shown so the stack's bounding box clears the
 * piece row beneath it.
 */
function Beat6() {
  const ch6 = CHAPTERS[5];
  const composite = ch6.boxes[0];
  const stackX = VB_W / 2 - 85;
  const stackY = 148;
  const stackW = 170;
  const idLine = composite.lines[1];
  const closeLine = composite.lines[6];
  const stackLines = [
    { code: composite.lines[0].text },
    { code: idLine.text },
    { code: closeLine.text },
  ];
  const stackH = 52 + 18 * stackLines.length;
  return (
    <Vignette label="Five schema cards stacked into one composite card, pieces unmoved beside dashed ghost outlines">
      {CARD_SLOTS.map((slot, i) => (
        <GhostCard
          key={CANON[i].name}
          x={slot.x}
          y={CARD_ROW_Y}
          w={slot.w}
          h={i < 2 ? 124 : 108}
          pulse
        />
      ))}
      <path d="M291 6 h-6 v14 h6" fill="none" stroke={TEAL} />
      <path d="M389 6 h6 v14 h-6" fill="none" stroke={TEAL} />
      <text
        x={VB_W / 2}
        y={16}
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize={10}
        letterSpacing="0.2em"
        fill={TEAL}
      >
        COMPOSITION
      </text>
      <rect
        x={stackX + 6}
        y={stackY + 6}
        width={stackW}
        height={stackH}
        rx={8}
        fill={CARD_FILL}
        stroke={CARD_STROKE}
      />
      <rect
        x={stackX + 3}
        y={stackY + 3}
        width={stackW}
        height={stackH}
        rx={8}
        fill={CARD_FILL}
        stroke={CARD_STROKE}
      />
      <SchemaCard
        x={stackX}
        y={stackY}
        w={stackW}
        label={composite.label}
        color={TEAL}
        file=""
        lines={stackLines}
      />
      {idLine.dots?.map((d, k) => (
        <circle
          key={d}
          cx={stackX + stackW - 14 - k * 10}
          cy={stackY + 48 + 18}
          r={3}
          fill={d}
        />
      ))}
      {SIDE_BY_SIDE.map((p, i) => (
        <Piece key={CANON[i].name} x={p.x} y={p.y} i={i} />
      ))}
    </Vignette>
  );
}

/**
 * Beat 7 — the composite card mounted in the gateway; three short feelers
 * reach three pieces. The three targeted services sit close beneath the
 * gateway so each feeler is a short line that actually lands on its piece
 * (not a line that trails off into empty table); the other two pieces stand
 * further out, present but untouched this query.
 */
const GATEWAY_TARGETS: readonly {
  readonly i: number;
  readonly x: number;
  readonly y: number;
  readonly ox: number;
}[] = [
  { i: 1, x: 260, y: 100, ox: 270 },
  { i: 2, x: 340, y: 100, ox: 340 },
  { i: 3, x: 420, y: 100, ox: 410 },
];
const GATEWAY_BYSTANDERS: readonly {
  readonly i: number;
  readonly x: number;
  readonly y: number;
}[] = [
  { i: 0, x: 90, y: 230 },
  { i: 4, x: 610, y: 230 },
];

function Beat7() {
  const gx = VB_W / 2;
  const gy = 32;
  return (
    <Vignette label="The composite card in a gateway chip, short feelers touching three pieces">
      <GatewayChip x={gx} y={gy} w={150} label="COMPOSITE · GATEWAY" />
      {GATEWAY_TARGETS.map(({ i, x, y, ox }) => (
        <line
          key={CANON[i].name}
          x1={ox}
          y1={gy + 13}
          x2={x}
          y2={y - 9}
          stroke={CANON[i].soft}
          strokeWidth={1.5}
        />
      ))}
      {GATEWAY_TARGETS.map(({ i, x, y }) => (
        <Piece key={CANON[i].name} x={x} y={y} i={i} />
      ))}
      {GATEWAY_BYSTANDERS.map(({ i, x, y }) => (
        <Piece key={CANON[i].name} x={x} y={y} i={i} />
      ))}
      <Card x={270} y={245} w={140} h={32} title="query" />
    </Vignette>
  );
}

const BEATS: readonly (() => React.JSX.Element)[] = [
  Beat1,
  Beat2,
  Beat3,
  Beat4,
  Beat5,
  Beat6,
  Beat7,
];

/** Compact mobile substitute for beats whose card text gets too small: a stack of readable cards plus an unmoved piece row. */
function MobileCardFallback({
  cards,
  ghosts,
}: {
  readonly cards: readonly {
    readonly label: string;
    readonly lines: readonly string[];
  }[];
  readonly ghosts: number;
}) {
  return (
    <div className="flex flex-col gap-3 sm:hidden">
      {cards.map((c) => (
        <div
          key={`${c.label}:${c.lines.join(" ")}`}
          className="border-cc-card-border rounded-lg border bg-[#0d1424] p-3"
        >
          <MicroLabel>{c.label}</MicroLabel>
          <div className="mt-1 flex flex-col gap-0.5 font-mono text-[11px] text-[#c9d4e8]">
            {c.lines.map((l) => (
              <span key={l}>{l}</span>
            ))}
          </div>
        </div>
      ))}
      {ghosts > 0 && (
        <p className="text-cc-nav-label font-mono text-[10px] tracking-[0.15em] uppercase">
          + {ghosts} more, ghosted
        </p>
      )}
      <div className="flex items-center justify-center gap-3 pt-1">
        {CANON.map((s) => (
          <span
            key={s.name}
            className="inline-block h-2.5 w-2.5 rounded-[3px]"
            style={{ background: s.color }}
            aria-hidden="true"
          />
        ))}
      </div>
      <p className="text-cc-nav-label text-center font-mono text-[10px] tracking-[0.15em] uppercase">
        Pieces: unmoved
      </p>
    </div>
  );
}

const band = "py-14 sm:py-20";
const grid = "grid grid-cols-1 gap-8 lg:grid-cols-12 lg:gap-10";

export function PiecesAndCards() {
  return (
    <PageSection maxWidth="6xl" className="pt-10 pb-20 sm:pt-12 sm:pb-28">
      <p className="text-cc-nav-label mx-auto max-w-2xl text-center font-mono text-[10px] tracking-[0.2em] uppercase">
        Pieces = services · cards = schemas and queries · pieces never stack.
      </p>

      {CHAPTERS.map((chapter, i) => {
        const Beat = BEATS[i];
        const vignetteOnLeft = i % 2 === 1;
        return (
          <RevealOnScroll key={chapter.title} className={band}>
            <div
              className={`border-cc-card-border scroll-mt-24 border-t pt-8 ${i === 0 ? "border-t-0 pt-0" : ""}`}
            >
              <div className={grid}>
                <div
                  className={`lg:col-span-5 lg:self-center ${vignetteOnLeft ? "lg:order-2" : "lg:order-1"}`}
                >
                  <h3 className="font-heading text-cc-heading text-sm sm:text-base">
                    {chapter.title}
                  </h3>
                  <div className="text-cc-ink mt-3 space-y-3 text-sm leading-relaxed">
                    {chapter.body}
                  </div>
                </div>
                <div
                  className={`lg:col-span-7 ${vignetteOnLeft ? "lg:order-1" : "lg:order-2"}`}
                >
                  <div className="hidden sm:block">
                    <Beat />
                  </div>
                  {i === 1 && (
                    <MobileCardFallback
                      cards={REQUESTS.slice(0, 3).map((r) => ({
                        label: "request",
                        lines: [r],
                      }))}
                      ghosts={2}
                    />
                  )}
                  {i === 4 && (
                    <MobileCardFallback
                      cards={CHAPTERS[4].boxes.map((b) => ({
                        label: b.label,
                        lines: b.lines.map((l) => l.text),
                      }))}
                      ghosts={3}
                    />
                  )}
                  {i !== 1 && i !== 4 && (
                    <div className="sm:hidden">
                      <Beat />
                    </div>
                  )}
                  <Nameplate>{NAMEPLATES[i]}</Nameplate>
                </div>
              </div>
            </div>
          </RevealOnScroll>
        );
      })}
    </PageSection>
  );
}
