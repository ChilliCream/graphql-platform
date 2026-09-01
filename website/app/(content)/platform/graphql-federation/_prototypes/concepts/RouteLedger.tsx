import { Fragment } from "react";
import type { CSSProperties, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";

import {
  CANON,
  GatewayChip,
  HorizonRule,
  INK_DIM,
  MicroLabel,
} from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";

const W = 1024;
const H = 4900;
const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";
const DASHED_STROKE = "rgba(245,241,234,0.3)";

const RAIL_X = 88;
const RAIL_TOP = 120;
const RAIL_BOTTOM = 4780;
const HORIZON_Y = 4300;
const HORIZON_GAP = 60;
const GATEWAY = { x: RAIL_X, y: 4380 } as const;
const FAN_Y = 4423;

/** Chapter y-positions on the rail; index 6 (the gateway beat) is drawn separately. */
const CHAPTER_Y = [560, 1060, 1540, 2020, 2500, 3740, 4600] as const;

const CHIP_X = 128;
const TICK_W = 4;
const TICK_H = 14;
const TICK_GAP = 6;
const TICK_STEP = TICK_W + TICK_GAP;
const ROW_WIDTH = 5 * TICK_STEP - TICK_GAP;

const tickX = (i: number, x0: number = CHIP_X) => x0 + i * TICK_STEP;

/** Left edge of copy blocks and the right-hand code-box column, in canvas px. */
const COPY_LEFT = 180;
const BOX_LEFT = 640;

const pct = (v: number, total: number) => `${(v / total) * 100}%`;
const copyLeftPct = pct(COPY_LEFT, W);
const boxLeftPct = pct(BOX_LEFT, W);

const MOBILE_CAPTIONS = [
  "5 owners",
  "×3 apps",
  "queue",
  "1 query",
  "no author",
  "composite schema",
  "gateway",
] as const;

function TickRow({
  x = CHIP_X,
  y,
  opacity = 1,
}: {
  readonly x?: number;
  readonly y: number;
  readonly opacity?: number;
}) {
  return (
    <>
      {CANON.map((s, i) => (
        <rect
          key={s.name}
          x={tickX(i, x)}
          y={y}
          width={TICK_W}
          height={TICK_H}
          rx={2}
          fill={s.color}
          opacity={opacity}
        />
      ))}
    </>
  );
}

function ChipCaption({
  x = CHIP_X,
  y,
  children,
}: {
  readonly x?: number;
  readonly y: number;
  readonly children: string;
}) {
  return (
    <text
      x={x}
      y={y}
      fontFamily={MONO}
      fontSize={10}
      letterSpacing="0.2em"
      fill={INK_DIM}
    >
      {children.toUpperCase()}
    </text>
  );
}

/**
 * The state chip beside a chapter badge: five ticks whose arrangement (never
 * their count) changes per beat, so the eye never reads "five became one."
 */
function StateChip({ beat, y }: { readonly beat: number; readonly y: number }) {
  const topY = y - TICK_H / 2;
  const capY = y + TICK_H / 2 + 14;

  switch (beat) {
    case 0:
      return (
        <g>
          <TickRow y={topY} />
          <ChipCaption y={capY}>{MOBILE_CAPTIONS[0]}</ChipCaption>
        </g>
      );
    case 1:
      return (
        <g>
          {[-5, 0, 5].map((dy) => (
            <g key={dy} opacity={0.4}>
              <TickRow y={topY + dy} />
            </g>
          ))}
          <ChipCaption y={capY + 6}>{MOBILE_CAPTIONS[1]}</ChipCaption>
        </g>
      );
    case 2:
      return (
        <g>
          <rect
            x={CHIP_X + 8}
            y={topY - 8}
            width={12}
            height={14}
            rx={2}
            fill="rgba(245,241,234,0.25)"
          />
          {CANON.map((s, i) => (
            <rect
              key={s.name}
              x={CHIP_X + 12}
              y={topY - 2 - i * 3}
              width={TICK_W}
              height={TICK_H}
              rx={2}
              fill={s.color}
              opacity={0.9 - i * 0.1}
            />
          ))}
          <ChipCaption y={capY + 10}>{MOBILE_CAPTIONS[2]}</ChipCaption>
        </g>
      );
    case 3:
      return (
        <g>
          <path
            d={`M${CHIP_X} ${topY + TICK_H + 6} h${ROW_WIDTH}`}
            stroke={DASHED_STROKE}
            fill="none"
          />
          <TickRow y={topY} />
          <text
            x={CHIP_X + ROW_WIDTH / 2}
            y={topY + TICK_H + 20}
            textAnchor="middle"
            fontFamily={MONO}
            fontSize={12}
            fill="#5eead4"
          >
            {"{ }"}
          </text>
        </g>
      );
    case 4: {
      const spread = TICK_STEP + 4;
      const midX = CHIP_X + 2 * spread;
      return (
        <g>
          {CANON.map((s, i) => (
            <rect
              key={s.name}
              x={CHIP_X + i * spread}
              y={topY}
              width={TICK_W}
              height={TICK_H}
              rx={2}
              fill={s.color}
            />
          ))}
          <rect
            x={midX - 6}
            y={capY - 6}
            width={12}
            height={14}
            rx={2}
            fill="none"
            stroke={DASHED_STROKE}
            strokeDasharray="3 3"
          />
          <ChipCaption y={capY + 24}>{MOBILE_CAPTIONS[4]}</ChipCaption>
        </g>
      );
    }
    case 5:
      return (
        <g>
          <rect
            x={CHIP_X - 5}
            y={topY - 5}
            width={ROW_WIDTH + 10}
            height={TICK_H + 10}
            rx={7}
            fill="none"
            stroke="rgba(94,234,212,0.45)"
          />
          <TickRow y={topY} />
          <ChipCaption y={capY}>{MOBILE_CAPTIONS[5]}</ChipCaption>
        </g>
      );
    default:
      return null;
  }
}

function ChapterBadge({ y, n }: { readonly y: number; readonly n: string }) {
  return (
    <g>
      <rect
        x={RAIL_X - 12}
        y={y - 12}
        width={24}
        height={24}
        rx={6}
        fill="#0d1424"
        stroke="rgba(245,241,234,0.13)"
      />
      <text
        x={RAIL_X}
        y={y + 4}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={11}
        fill={INK_DIM}
      >
        {n}
      </text>
    </g>
  );
}

/**
 * The 1px chapter rail: a table of contents made physical. Numbered badges
 * and five-tick state chips run down x=88; a dashed break at y=4300 marks the
 * build/runtime seam, after which the ticks fan back out under the gateway.
 */
function RouteRail() {
  const fanXs = [0, 1, 2, 3, 4].map((i) => GATEWAY.x - 22 + i * TICK_STEP);

  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      aria-hidden="true"
      className="absolute inset-0 z-0 hidden h-full w-full sm:block"
    >
      <line
        x1={RAIL_X}
        x2={RAIL_X}
        y1={RAIL_TOP}
        y2={HORIZON_Y}
        stroke="rgba(245,241,234,0.14)"
        strokeWidth={1}
      />
      <line
        x1={RAIL_X}
        x2={RAIL_X}
        y1={HORIZON_Y}
        y2={HORIZON_Y + HORIZON_GAP}
        stroke="rgba(245,241,234,0.22)"
        strokeWidth={1}
        strokeDasharray="5 7"
      />
      <line
        x1={RAIL_X}
        x2={RAIL_X}
        y1={HORIZON_Y + HORIZON_GAP}
        y2={RAIL_BOTTOM}
        stroke="rgba(245,241,234,0.14)"
        strokeWidth={1}
      />
      <text
        x={110}
        y={HORIZON_Y - 16}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        BUILD TIME
      </text>
      <text
        x={GATEWAY.x + 60}
        y={GATEWAY.y}
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.2em"
        fill={INK_DIM}
        opacity={0.7}
      >
        RUNTIME
      </text>

      {CHAPTER_Y.slice(0, 6).map((y, i) => (
        <Fragment key={y}>
          <ChapterBadge y={y} n={`0${i + 1}`} />
          <StateChip beat={i} y={y} />
        </Fragment>
      ))}

      <GatewayChip x={GATEWAY.x} y={GATEWAY.y} />
      {fanXs.map((x, i) => (
        <line
          key={CANON[i].name}
          x1={x + TICK_W / 2}
          x2={x + TICK_W / 2}
          y1={GATEWAY.y + 13}
          y2={FAN_Y}
          stroke={DASHED_STROKE}
        />
      ))}
      <TickRow x={fanXs[0]} y={FAN_Y} />
      <ChipCaption x={fanXs[0]} y={FAN_Y + TICK_H + 20}>
        {MOBILE_CAPTIONS[6]}
      </ChipCaption>
    </svg>
  );
}

function MobileTicks({ beat }: { readonly beat: number }) {
  const row = (opacity = 1) => (
    <div className="flex items-center gap-1.5" style={{ opacity }}>
      {CANON.map((s) => (
        <span
          key={s.name}
          className="inline-block h-3.5 w-1"
          style={{ background: s.color, borderRadius: 2 }}
        />
      ))}
    </div>
  );

  switch (beat) {
    case 1:
      return (
        <div className="flex flex-col gap-0.5">
          {row(0.4)}
          {row(0.4)}
        </div>
      );
    case 2:
      return (
        <div className="flex items-center gap-1.5">
          <span
            className="inline-block h-3.5 w-3"
            style={{ background: "rgba(245,241,234,0.25)", borderRadius: 2 }}
          />
          {row(0.7)}
        </div>
      );
    case 3:
      return (
        <div className="flex items-center gap-1.5">
          {row()}
          <span className="font-mono text-[11px] text-[#5eead4]">{"{ }"}</span>
        </div>
      );
    case 4:
      return (
        <div className="flex items-center gap-3">
          {row()}
          <span
            className="inline-block h-3.5 w-3 rounded-[2px] border border-dashed"
            style={{ borderColor: DASHED_STROKE }}
          />
        </div>
      );
    case 5:
      return (
        <div
          className="flex items-center gap-1.5 rounded-full border px-2 py-1"
          style={{ borderColor: "rgba(94,234,212,0.45)" }}
        >
          {row()}
        </div>
      );
    case 6:
      return (
        <div className="flex items-center gap-2">
          <span className="rounded-full border border-[rgba(94,234,212,0.45)] bg-[#0d1424] px-2 py-0.5 font-mono text-[9px] tracking-[0.18em] text-[#5eead4]">
            GATEWAY
          </span>
          {row()}
        </div>
      );
    default:
      return row();
  }
}

function MobileStateRow({
  beat,
  n,
}: {
  readonly beat: number;
  readonly n: string;
}) {
  return (
    <div className="mb-3 flex flex-wrap items-center gap-3 sm:hidden">
      <span className="border-cc-card-border flex h-6 w-6 shrink-0 items-center justify-center rounded-md border bg-[#0d1424] font-mono text-[11px] text-[rgba(245,241,234,0.62)]">
        {n}
      </span>
      <MobileTicks beat={beat} />
      <MicroLabel>{MOBILE_CAPTIONS[beat]}</MicroLabel>
    </div>
  );
}

interface CopyBlockProps {
  readonly top: number;
  readonly title: string;
  readonly children: ReactNode;
  /** Narrower cap so the block clears the code-box column at x=640 instead of running under it. */
  readonly narrow?: boolean;
}

function CopyBlock({ top, title, children, narrow }: CopyBlockProps) {
  return (
    <div
      className={`relative w-full sm:absolute sm:top-(--top) sm:left-(--left) sm:z-20 sm:-translate-y-1/2 ${
        narrow ? "sm:w-[min(44%,27rem)]" : "sm:w-[min(60%,34rem)]"
      }`}
      style={{ "--top": pct(top, H), "--left": copyLeftPct } as CSSProperties}
    >
      <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
        {title}
      </h3>
      <div className="text-cc-ink mt-4 space-y-3 text-sm sm:text-base">
        {children}
      </div>
    </div>
  );
}

function BoxSlot({
  top,
  children,
}: {
  readonly top: number;
  readonly children: ReactNode;
}) {
  return (
    <div
      className="mx-auto w-[min(100%,21rem)] sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:mx-0 sm:w-[min(27%,20rem)] sm:-translate-y-1/2"
      style={{ "--top": pct(top, H), "--left": boxLeftPct } as CSSProperties}
    >
      {children}
    </div>
  );
}

/** Per-chapter vertical placement for copy and code boxes, in canvas px. */
const LAYOUT: readonly {
  readonly copy: number;
  readonly boxes: readonly number[];
  readonly narrow?: boolean;
}[] = [
  { copy: CHAPTER_Y[0], boxes: [CHAPTER_Y[0]], narrow: true },
  { copy: CHAPTER_Y[1], boxes: [CHAPTER_Y[1]], narrow: true },
  { copy: CHAPTER_Y[2], boxes: [] },
  { copy: CHAPTER_Y[3], boxes: [CHAPTER_Y[3]], narrow: true },
  {
    copy: CHAPTER_Y[4],
    boxes: [CHAPTER_Y[4] - 110, CHAPTER_Y[4] + 110],
    narrow: true,
  },
  { copy: CHAPTER_Y[5], boxes: [CHAPTER_Y[5]], narrow: true },
  { copy: CHAPTER_Y[6], boxes: [] },
];

/**
 * Prototype v10: Route Ledger. Replaces the merging-lines map with a single
 * 1px chapter rail: numbered badges and five-tick state chips whose
 * arrangement, never their count, changes per beat, so composition reads as
 * enclosure of an unchanged set rather than services fusing into one.
 */
export function RouteLedger() {
  return (
    <PageSection maxWidth="6xl">
      <div className="relative mx-auto w-full max-w-5xl border-l border-[rgba(245,241,234,0.14)] sm:aspect-[1024/4900] sm:border-l-0">
        <RouteRail />
        <div className="flex flex-col gap-14 px-5 py-16 sm:contents">
          {CHAPTERS.map((chapter, i) => {
            const layout = LAYOUT[i];
            return (
              <Fragment key={i}>
                <div className="sm:hidden">
                  <MobileStateRow beat={i} n={`0${i + 1}`} />
                </div>
                <CopyBlock
                  top={layout.copy}
                  title={chapter.title}
                  narrow={layout.narrow}
                >
                  {chapter.body}
                </CopyBlock>
                {chapter.boxes.map((box, j) => (
                  <BoxSlot key={j} top={layout.boxes[j]}>
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
