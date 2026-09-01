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
/** Taller than the shared 4900 column: the call sheet needs room below the
 * chapter 7 paragraph, which the other prototypes don't have to clear. */
const H = 5220;
const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

const FILE_CHIP_X = 60;
const FILE_CHIP_W = 208;
const FILE_CHIP_H = 30;
const FILE_CHIP_GAP = 44;
const FILE_CHIP_START_Y = 2500;
const CHIP_RIGHT_X = FILE_CHIP_X + FILE_CHIP_W;

const BOX_CENTER_X = 512;
const BOX_W = 584;
const BOX_LEFT_X = BOX_CENTER_X - BOX_W / 2;
const BOX_TOP_Y = 4020;
const BOX_BOTTOM_Y = 4260;
const BOX_CENTER_Y = (BOX_TOP_Y + BOX_BOTTOM_Y) / 2;

/** Five entry points along the box's gutter, 50px apart (spec floor: 18px). */
const HAIRLINE_ENTRY_Y = [4045, 4095, 4145, 4195, 4245] as const;

const HORIZON_Y = 4300;
const GATEWAY = { x: 512, y: 4380 } as const;

const FILES = CANON.map((service) => ({
  name: service.name,
  color: service.color,
  file: `${service.name.toLowerCase()}/schema.graphql`,
}));

const CALL_SHEET = CANON.map((service) => ({
  name: service.name.toLowerCase(),
  color: service.color,
}));

interface PositionedCopy {
  readonly top: number;
  readonly left: number;
  readonly side?: boolean;
}

interface PositionedBox {
  readonly top: number;
  readonly left: number;
  readonly paired?: boolean;
  readonly wide?: boolean;
}

interface ChapterLayout {
  readonly copy: PositionedCopy;
  readonly boxes: readonly PositionedBox[];
}

interface Badge {
  readonly n: string;
  readonly caption: string;
}

/**
 * Chapters 1-4 keep the transit map's original copy/box placement (per the
 * task spec); chapter 5 keeps the paired schema-card placement too. Chapter
 * 6's single box widens and recenters to host the composite-schema gutter;
 * chapter 7 drops its box (the gateway and call sheet render separately).
 */
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
    copy: { top: 3725, left: 50 },
    boxes: [{ top: BOX_CENTER_Y, left: 50, wide: true }],
  },
  { copy: { top: 4600, left: 50 }, boxes: [] },
];

/** Chapter badges: only chapters 1-4 get a numeral anchor + micro-caption. */
const BADGES: readonly (Badge | undefined)[] = [
  { n: "01", caption: "Five teams" },
  { n: "02", caption: "N×M calls" },
  { n: "03", caption: "One queue" },
  { n: "04", caption: "One query" },
  undefined,
  undefined,
  undefined,
];

const pct = (v: number, total: number) => `${(v / total) * 100}%`;

function placement(top: number, left: number): CSSProperties {
  return { "--top": pct(top, H), "--left": `${left}%` } as CSSProperties;
}

interface CopyBlockProps extends PositionedCopy {
  readonly title: string;
  readonly badge?: Badge;
  readonly children: ReactNode;
}

function CopyBlock({
  top,
  left,
  side,
  title,
  badge,
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
        style={{
          background:
            "radial-gradient(ellipse 62% 58% at 50% 50%, rgba(11,15,26,0.98) 0%, rgba(11,15,26,0.94) 50%, rgba(11,15,26,0.6) 76%, rgba(11,15,26,0) 93%)",
        }}
      />
      <div className="relative">
        {badge && (
          <div className="mb-2 flex items-center justify-center gap-2 sm:justify-start">
            <span
              className="flex h-6 w-6 shrink-0 items-center justify-center rounded-[6px] bg-[#0d1424] font-mono text-[10px]"
              style={{ color: "#c9d4e8" }}
            >
              {badge.n}
            </span>
            <MicroLabel>{badge.caption}</MicroLabel>
          </div>
        )}
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

function BoxSlot({ top, left, paired, wide, children }: BoxSlotProps) {
  const widthClass = wide
    ? "sm:w-[36.5rem]"
    : paired
      ? "sm:w-[min(43%,21rem)]"
      : "sm:w-[min(88%,21rem)]";
  return (
    <div
      className={`mx-auto w-[min(100%,21rem)] sm:absolute sm:top-(--top) sm:left-(--left) sm:z-30 sm:mx-0 sm:-translate-x-1/2 sm:-translate-y-1/2 ${widthClass}`}
      style={placement(top, left)}
    >
      {children}
    </div>
  );
}

interface FileChipProps {
  readonly color: string;
  readonly file: string;
}

/** A single subgraph's schema file, the recurring visual unit of this concept. */
function FileChip({ color, file }: FileChipProps) {
  return (
    <div
      className="flex items-center gap-2 rounded-lg border px-3 py-1.5"
      style={{
        background: "rgba(12,19,34,0.5)",
        borderColor: "rgba(245,241,234,0.13)",
      }}
    >
      <span
        aria-hidden="true"
        className="h-2.5 w-2.5 shrink-0 rounded-[3px]"
        style={{ background: color }}
      />
      <span className="font-mono text-[10px]" style={{ color: INK_DIM }}>
        {file}
      </span>
    </div>
  );
}

interface GutterLine {
  readonly text: string;
  readonly dots?: readonly string[];
}

interface BlameCodeBoxProps {
  readonly label: string;
  readonly lines: readonly GutterLine[];
}

/**
 * The composite schema, rendered with a git-blame-style colored gutter per
 * field: single-owner fields carry one 3px bar, the shared key field carries
 * one bar per owner, stacked. This box is the concept's centerpiece; the
 * lines are the same `CHAPTERS` code sample every other prototype shows.
 */
function BlameCodeBox({ label, lines }: BlameCodeBoxProps) {
  return (
    <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
      <div className="flex items-center justify-between gap-2">
        <MicroLabel>{label}</MicroLabel>
        <MicroLabel className="opacity-70">5 sources</MicroLabel>
      </div>
      <div className="border-cc-card-border mt-2 border-t pt-2 font-mono text-[12px] leading-6">
        {lines.map((line, i) => (
          <div key={i} className="flex items-center gap-2">
            <span
              aria-hidden="true"
              className="flex h-[14px] w-3 shrink-0 items-center gap-px"
            >
              {line.dots?.map((color, k) => (
                <span
                  key={k}
                  className="h-full w-[3px]"
                  style={{ background: color }}
                />
              ))}
            </span>
            <span className="whitespace-pre text-[#c9d4e8]">{line.text}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

/** Runtime fan-out as a typeset list of requests instead of drawn lines. */
function CallSheet() {
  return (
    <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
      <MicroLabel>Runtime · call sheet</MicroLabel>
      <div className="border-cc-card-border mt-2 flex flex-col gap-2 border-t pt-3 font-mono text-xs">
        {CALL_SHEET.map((service) => (
          <div key={service.name} className="flex items-center gap-2">
            <span
              aria-hidden="true"
              className="h-2 w-2 shrink-0 rounded-full"
              style={{ background: service.color }}
            />
            <span style={{ color: "#c9d4e8" }}>
              {service.name}
              <span className="mx-2" style={{ color: "#5eead4" }}>
                -&gt;
              </span>
              POST /graphql
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

/**
 * Desktop-only decorative layer: the five margin file chips, the parallel
 * dashed hairlines running from each into the composite box's gutter, the
 * build/runtime horizon, and the gateway chip. No merging lines - the
 * document itself is the artifact everything points at.
 */
function AuthorshipMap() {
  return (
    <svg
      viewBox={`0 0 ${W} ${H}`}
      aria-hidden="true"
      className="absolute inset-0 z-0 hidden h-full w-full sm:block"
    >
      {FILES.map((service, i) => {
        const y = FILE_CHIP_START_Y + i * FILE_CHIP_GAP;
        return (
          <g key={service.name}>
            <rect
              x={FILE_CHIP_X}
              y={y}
              width={FILE_CHIP_W}
              height={FILE_CHIP_H}
              rx={8}
              fill="rgba(12,19,34,0.5)"
              stroke="rgba(245,241,234,0.13)"
            />
            <rect
              x={FILE_CHIP_X + 10}
              y={y + 10}
              width={10}
              height={10}
              rx={3}
              fill={service.color}
            />
            <text
              x={FILE_CHIP_X + 28}
              y={y + 19}
              fontFamily={MONO}
              fontSize={10}
              fill={INK_DIM}
            >
              {service.file}
            </text>
          </g>
        );
      })}

      {FILES.map((service, i) => {
        const chipY = FILE_CHIP_START_Y + i * FILE_CHIP_GAP + FILE_CHIP_H / 2;
        const midX = 300 + i * 12;
        const entryY = HAIRLINE_ENTRY_Y[i];
        return (
          <polyline
            key={service.name}
            points={`${CHIP_RIGHT_X},${chipY} ${midX},${chipY} ${midX},${entryY} ${BOX_LEFT_X},${entryY}`}
            fill="none"
            stroke={service.color}
            strokeWidth={1}
            strokeOpacity={0.55}
            strokeDasharray="1 3"
          />
        );
      })}

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
    </svg>
  );
}

/**
 * Prototype v6 "Authorship Column": the schema document is the hero. Five
 * persistent file chips sit in the margin, the composite schema carries a
 * git-blame-style colored gutter per field instead of trailing dots, and
 * runtime is a typeset call sheet under the gateway - fan-out as a list of
 * distinct requests, never a converging bundle of lines.
 */
export function AuthorshipColumn() {
  return (
    <PageSection maxWidth="6xl">
      <div className="relative mx-auto w-full max-w-5xl sm:aspect-[1024/5220]">
        <AuthorshipMap />
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
                  badge={BADGES[i]}
                >
                  {chapter.body}
                </CopyBlock>

                {i === 5 && (
                  <div className="mx-auto flex w-full max-w-2xl flex-wrap justify-center gap-2 sm:hidden">
                    {FILES.map((service) => (
                      <FileChip key={service.name} {...service} />
                    ))}
                  </div>
                )}

                {i === 5
                  ? chapter.boxes.map((box, j) => (
                      <BoxSlot
                        key={j}
                        top={layout.boxes[j].top}
                        left={layout.boxes[j].left}
                        wide={layout.boxes[j].wide}
                      >
                        <BlameCodeBox
                          label="Composite schema · composed from 5 sources"
                          lines={box.lines}
                        />
                      </BoxSlot>
                    ))
                  : chapter.boxes.map((box, j) => (
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

                {i === 6 && (
                  <>
                    <div className="flex justify-center sm:hidden">
                      <svg
                        viewBox="0 0 176 32"
                        width={140}
                        height={26}
                        aria-hidden="true"
                      >
                        <GatewayChip x={88} y={16} />
                      </svg>
                    </div>
                    <BoxSlot top={5080} left={50}>
                      <CallSheet />
                    </BoxSlot>
                  </>
                )}
              </Fragment>
            );
          })}
        </div>
      </div>
    </PageSection>
  );
}
