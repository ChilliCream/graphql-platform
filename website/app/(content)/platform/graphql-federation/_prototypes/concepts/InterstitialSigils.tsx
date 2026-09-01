import type { ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

import { CANON, GatewayChip, MicroLabel } from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";

const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";
const HAIRLINE = "rgba(245,241,234,0.25)";
const BRACE_INK = "rgba(245,241,234,0.6)";
const COMPOSE_RING = "rgba(94,234,212,0.6)";

/** x positions for the five service squares. Held identical across G1, G6 and
 * G7 so the constancy of the services is itself visible across the sequence. */
const SQUARE_XS = [60, 110, 160, 210, 260] as const;
const SQUARE_SIZE = 14;

interface SquareProps {
  readonly x: number;
  readonly y: number;
  readonly color: string;
  readonly size?: number;
}

/** The service token: a filled square. Services never touch. */
function Square({ x, y, color, size = SQUARE_SIZE }: SquareProps) {
  return (
    <rect
      x={x - size / 2}
      y={y - size / 2}
      width={size}
      height={size}
      rx={3}
      fill={color}
    />
  );
}

/**
 * The schema token: a hand-drawn brace, traced as a path rather than set in a
 * font glyph so its scale and tip depth stay under control. `flip` mirrors it
 * to face the other way (an opening brace vs. a closing one).
 */
function bracePath(
  x: number,
  y: number,
  h: number,
  w: number,
  flip = false,
): string {
  const sign = flip ? -1 : 1;
  const midY = y + h / 2;
  const q1 = y + h * 0.32;
  const q3 = y + h * 0.68;
  return `M ${x} ${y} C ${x + sign * w * 0.7} ${y}, ${x + sign * w} ${y + h * 0.18}, ${x + sign * w} ${q1} C ${x + sign * w} ${q1 + h * 0.1}, ${x + sign * w * 1.8} ${midY - h * 0.06}, ${x + sign * w * 2} ${midY} C ${x + sign * w * 1.8} ${midY + h * 0.06}, ${x + sign * w} ${q3 - h * 0.1}, ${x + sign * w} ${q3} C ${x + sign * w} ${y + h - h * 0.18}, ${x + sign * w * 0.7} ${y + h}, ${x} ${y + h}`;
}

/** Top half of a brace, tapering toward a tip near `tipY` instead of closing
 * on itself, so a gap can sit at the brace's midpoint. */
function braceTopHalf(
  x: number,
  y: number,
  tipY: number,
  w: number,
  flip = false,
): string {
  const sign = flip ? -1 : 1;
  const h = (tipY - y) * 2;
  const q1 = y + h * 0.18;
  return `M ${x} ${y} C ${x + sign * w * 0.7} ${y}, ${x + sign * w} ${q1}, ${x + sign * w} ${y + h * 0.32} C ${x + sign * w} ${y + h * 0.42}, ${x + sign * w * 1.8} ${tipY - (tipY - y) * 0.06}, ${x + sign * w * 1.95} ${tipY}`;
}

/** Bottom half of a brace, the mirror of {@link braceTopHalf}. */
function braceBottomHalf(
  x: number,
  yEnd: number,
  tipY: number,
  w: number,
  flip = false,
): string {
  const sign = flip ? -1 : 1;
  const h = (yEnd - tipY) * 2;
  const q3 = yEnd - h * 0.18;
  return `M ${x + sign * w * 1.95} ${tipY} C ${x + sign * w * 1.8} ${tipY + (yEnd - tipY) * 0.06}, ${x + sign * w} ${yEnd - h * 0.42}, ${x + sign * w} ${yEnd - h * 0.32} C ${x + sign * w} ${q3}, ${x + sign * w * 0.7} ${yEnd}, ${x} ${yEnd}`;
}

interface BracePairProps {
  readonly leftX: number;
  readonly rightX: number;
  readonly y: number;
  readonly h: number;
  readonly reach: number;
  readonly stroke: string;
  readonly strokeWidth?: number;
  readonly dashed?: boolean;
}

/** A full, closed opening/closing brace pair, e.g. the "{ }" around a schema. */
function BracePair({
  leftX,
  rightX,
  y,
  h,
  reach,
  stroke,
  strokeWidth = 1.5,
  dashed = false,
}: BracePairProps) {
  return (
    <>
      <path
        d={bracePath(leftX, y, h, reach)}
        fill="none"
        stroke={stroke}
        strokeWidth={strokeWidth}
        strokeDasharray={dashed ? "4 5" : undefined}
      />
      <path
        d={bracePath(rightX, y, h, reach, true)}
        fill="none"
        stroke={stroke}
        strokeWidth={strokeWidth}
        strokeDasharray={dashed ? "4 5" : undefined}
      />
    </>
  );
}

interface SigilFigureProps {
  readonly number: string;
  readonly caption: string;
  readonly children: ReactNode;
}

/** Shared chrome for a chapter's opening glyph: a centered 320x120 SVG stage
 * above a numbered mono caption. */
function SigilFigure({ number, caption, children }: SigilFigureProps) {
  return (
    <figure className="mx-auto flex flex-col items-center gap-4">
      <svg
        viewBox="0 0 320 120"
        role="img"
        aria-label={caption}
        className="h-auto w-[272px] sm:w-80"
      >
        {children}
      </svg>
      <figcaption>
        <MicroLabel>{`${number} · ${caption}`}</MicroLabel>
      </figcaption>
    </figure>
  );
}

/** G1 — five colored service squares on a hairline baseline, one outlined
 * client circle floating above, unconnected. */
function GlyphFiveTeams() {
  return (
    <>
      <line
        x1={40}
        x2={280}
        y1={78}
        y2={78}
        stroke={HAIRLINE}
        strokeWidth={1}
      />
      <circle
        cx={160}
        cy={28}
        r={8}
        fill="none"
        stroke="rgba(245,241,234,0.5)"
        strokeWidth={1.5}
      />
      {SQUARE_XS.map((x, i) => (
        <Square key={CANON[i].name} x={x} y={78} color={CANON[i].color} />
      ))}
    </>
  );
}

const CLIENT_CENTERS = [
  { x: 80, y: 22 },
  { x: 160, y: 16 },
  { x: 240, y: 22 },
] as const;

/** G2 — three faint client circles, each throwing five thin arcs to all five
 * services: n clients times m services. */
function GlyphManyToMany() {
  return (
    <>
      {CLIENT_CENTERS.flatMap((client, ci) =>
        SQUARE_XS.map((sx, si) => {
          const midY = (client.y + 78) / 2 - 10;
          return (
            <path
              key={`${ci}-${si}`}
              d={`M ${client.x} ${client.y} Q ${(client.x + sx) / 2} ${midY}, ${sx} 78`}
              fill="none"
              stroke={CANON[si].color}
              strokeWidth={1}
              strokeOpacity={0.3}
            />
          );
        }),
      )}
      {CLIENT_CENTERS.map((client, i) => (
        <circle
          key={i}
          cx={client.x}
          cy={client.y}
          r={6}
          fill="none"
          stroke="rgba(245,241,234,0.4)"
          strokeWidth={1.5}
        />
      ))}
      {SQUARE_XS.map((x, i) => (
        <Square key={CANON[i].name} x={x} y={78} color={CANON[i].color} />
      ))}
    </>
  );
}

/** G3 — the five services stacked into a single column behind one gray
 * outlined queue, waiting their turn. */
function GlyphOneQueue() {
  return (
    <>
      {SQUARE_XS.map((_, i) => {
        const y = 16 + i * 22;
        const x = 120 + (i % 2 === 1 ? -6 : 0);
        return (
          <Square key={CANON[i].name} x={x} y={y} color={CANON[i].color} />
        );
      })}
      <rect
        x={196}
        y={50}
        width={44}
        height={20}
        rx={6}
        fill="none"
        stroke="rgba(245,241,234,0.35)"
        strokeWidth={1}
      />
      <text
        x={218}
        y={64}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={12}
        fill="rgba(245,241,234,0.5)"
      >
        …
      </text>
    </>
  );
}

/** G4 — one large brace pair with the five services still visible, still
 * spaced apart, between them. The schema wraps the services; it does not
 * touch them. */
function GlyphOneSchema() {
  return (
    <>
      <BracePair
        leftX={64}
        rightX={256}
        y={15}
        h={90}
        reach={9}
        stroke={BRACE_INK}
      />
      {SQUARE_XS.map((x, i) => (
        <Square key={CANON[i].name} x={x} y={60} color={CANON[i].color} />
      ))}
    </>
  );
}

/** G5 — the same big braces, redrawn dashed with a gap at the tip: unfinished,
 * unowned. Each service grows its own small brace beside it in reply. */
function GlyphWhoWritesIt() {
  const midY = 60;
  const gap = 12;
  return (
    <>
      <path
        d={braceTopHalf(64, 15, midY - gap / 2, 9)}
        fill="none"
        stroke={BRACE_INK}
        strokeWidth={1.5}
        strokeDasharray="4 5"
      />
      <path
        d={braceBottomHalf(64, 105, midY + gap / 2, 9)}
        fill="none"
        stroke={BRACE_INK}
        strokeWidth={1.5}
        strokeDasharray="4 5"
      />
      <path
        d={braceTopHalf(256, 15, midY - gap / 2, 9, true)}
        fill="none"
        stroke={BRACE_INK}
        strokeWidth={1.5}
        strokeDasharray="4 5"
      />
      <path
        d={braceBottomHalf(256, 105, midY + gap / 2, 9, true)}
        fill="none"
        stroke={BRACE_INK}
        strokeWidth={1.5}
        strokeDasharray="4 5"
      />
      {SQUARE_XS.map((x, i) => (
        <g key={CANON[i].name}>
          <Square x={x} y={60} color={CANON[i].color} />
          <path
            d={bracePath(x + 11, 52, 16, 4, false)}
            fill="none"
            stroke={CANON[i].color}
            strokeWidth={1.25}
          />
        </g>
      ))}
    </>
  );
}

const COMPOSE_XS = [88, 124, 160, 196, 232] as const;

/** G6 — the composition beat. The top row restates the services untouched;
 * only the small brace tokens below slide together inside one outlined
 * brace, because braces are the only token that ever nests. */
function GlyphSchemasCompose() {
  return (
    <>
      <text
        x={160}
        y={10}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={8}
        letterSpacing="0.18em"
        fill="rgba(245,241,234,0.45)"
      >
        SERVICES UNCHANGED
      </text>
      {SQUARE_XS.map((x, i) => (
        <Square key={CANON[i].name} x={x} y={24} color={CANON[i].color} />
      ))}
      <BracePair
        leftX={80}
        rightX={240}
        y={61}
        h={34}
        reach={6}
        stroke={COMPOSE_RING}
      />
      {COMPOSE_XS.map((x, i) => (
        <line
          key={`v-${CANON[i].name}`}
          x1={SQUARE_XS[i]}
          y1={31}
          x2={x}
          y2={69}
          stroke="rgba(245,241,234,0.2)"
          strokeWidth={1}
        />
      ))}
      {COMPOSE_XS.map((x, i) => (
        <path
          key={`b-${CANON[i].name}`}
          d={bracePath(x - 4, 69, 18, 4)}
          fill="none"
          stroke={CANON[i].color}
          strokeWidth={1.25}
        />
      ))}
      <text
        x={160}
        y={114}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={8}
        letterSpacing="0.18em"
        fill="rgba(94,234,212,0.75)"
      >
        SCHEMAS COMPOSE
      </text>
    </>
  );
}

/** G7 — build time above the line, runtime below it: one gateway holding the
 * one composed schema, fanning out at runtime to the five untouched
 * services. */
function GlyphOneGateway() {
  return (
    <>
      <line
        x1={0}
        x2={320}
        y1={52}
        y2={52}
        stroke="rgba(245,241,234,0.3)"
        strokeWidth={1}
        strokeDasharray="5 7"
      />
      <text
        x={8}
        y={40}
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.18em"
        fill="rgba(245,241,234,0.55)"
      >
        BUILD TIME
      </text>
      <text
        x={8}
        y={70}
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.18em"
        fill="rgba(245,241,234,0.4)"
      >
        RUNTIME
      </text>
      <BracePair
        leftX={140}
        rightX={180}
        y={10}
        h={26}
        reach={5}
        stroke={BRACE_INK}
      />
      {SQUARE_XS.map((x) => (
        <line
          key={x}
          x1={160}
          y1={65}
          x2={x}
          y2={100}
          stroke="rgba(245,241,234,0.25)"
          strokeWidth={1}
        />
      ))}
      {SQUARE_XS.map((x, i) => (
        <Square key={CANON[i].name} x={x} y={100} color={CANON[i].color} />
      ))}
      <GatewayChip x={160} y={52} />
    </>
  );
}

interface Beat {
  readonly caption: string;
  readonly glyph: ReactNode;
}

/** The seven sigils in chapter order. The sequence is the argument: squares
 * (services) never touch across any beat; braces (schemas) are the only
 * token that ever nests. */
const BEATS: readonly Beat[] = [
  { caption: "FIVE TEAMS · ONE SCREEN", glyph: <GlyphFiveTeams /> },
  { caption: "N CLIENTS × M SERVICES", glyph: <GlyphManyToMany /> },
  { caption: "ONE TEAM · ONE QUEUE", glyph: <GlyphOneQueue /> },
  { caption: "ONE SCHEMA", glyph: <GlyphOneSchema /> },
  { caption: "WHO WRITES IT?", glyph: <GlyphWhoWritesIt /> },
  { caption: "SCHEMAS COMPOSE", glyph: <GlyphSchemasCompose /> },
  { caption: "ONE GATEWAY · FIVE CALLS", glyph: <GlyphOneGateway /> },
];

/**
 * Prototype v8 — Interstitial Sigils. Seven small evolving divider glyphs,
 * one above each chapter, built from a strict two-token vocabulary: squares
 * are services and never touch; braces are schemas and are the only things
 * that ever nest. The sigil sequence carries the argument; copy and code
 * boxes below each one follow in normal flow.
 */
export function InterstitialSigils() {
  return (
    <PageSection maxWidth="5xl" className="py-16 sm:py-24">
      <div className="space-y-24 sm:space-y-32">
        {CHAPTERS.map((chapter, i) => {
          const beat = BEATS[i];
          const number = String(i + 1).padStart(2, "0");
          return (
            <div key={chapter.title}>
              <SigilFigure number={number} caption={beat.caption}>
                {beat.glyph}
              </SigilFigure>
              <RevealOnScroll className="mx-auto mt-10 max-w-2xl text-center">
                <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
                  {chapter.title}
                </h3>
                <div className="text-cc-ink mt-4 space-y-3 text-left text-sm sm:text-base">
                  {chapter.body}
                </div>
                {chapter.boxes.length > 0 && (
                  <div className="mt-6 grid gap-4 text-left sm:grid-cols-2">
                    {chapter.boxes.map((box) => (
                      <ProtoCodeBox key={box.label} {...box} />
                    ))}
                  </div>
                )}
              </RevealOnScroll>
            </div>
          );
        })}
      </div>
    </PageSection>
  );
}
