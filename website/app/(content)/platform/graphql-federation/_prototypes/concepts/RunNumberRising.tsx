import { Fragment } from "react";
import type { CSSProperties, ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

import {
  CANON,
  GatewayChip,
  HorizonRule,
  INK_DIM,
  MicroLabel,
} from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";

const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";
const TEAL = "#5eead4";
const AMBER = "#eabd21";
const CORAL = "#f27765";
const CARD_BG = "rgba(12,19,34,0.5)";
const CARD_STROKE = "rgba(245,241,234,0.13)";
const SEGMENT = "rgba(139,160,188,0.4)";
const DASHED = "rgba(245,241,234,0.3)";

/** The recurring pipeline's own team: Billing. Everyone else is a sibling. */
const BILLING = CANON[1];
const SIBLINGS = [CANON[0], CANON[2], CANON[3], CANON[4]];

type StageStatus = "pass" | "hold" | "broken" | "pending";

interface StageSpec {
  readonly label: string;
  readonly status: StageStatus;
  readonly dashed?: boolean;
}

const STAGE_COLOR: Record<StageStatus, string> = {
  pass: TEAL,
  hold: AMBER,
  broken: CORAL,
  pending: INK_DIM,
};

const band = "py-16 sm:py-24";
const grid = "grid grid-cols-1 gap-8 lg:grid-cols-12 lg:gap-10";

function Copy({
  children,
  className,
}: {
  readonly children: ReactNode;
  readonly className?: string;
}) {
  return (
    <div
      className={`text-cc-ink space-y-4 text-sm leading-relaxed sm:text-base ${className ?? ""}`.trim()}
    >
      {children}
    </div>
  );
}

function BeatHead({ index }: { readonly index: number }) {
  const chapter = CHAPTERS[index];
  return (
    <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
      {chapter.title}
    </h3>
  );
}

/** One check / hold / broken / pending glyph, drawn at a stage node. */
function StageGlyph({
  x,
  y,
  status,
}: {
  readonly x: number;
  readonly y: number;
  readonly status: StageStatus;
}) {
  if (status === "hold") {
    // 8px padlock: a small body with a shackle arc, so the amber hold reads
    // apart from Billing's own amber header chip even in isolation.
    return (
      <g>
        <path
          d={`M${x - 2} ${y - 1.5} v-1.8 a2 2 0 0 1 4 0 v1.8`}
          fill="none"
          stroke={AMBER}
          strokeWidth={1.1}
        />
        <rect
          x={x - 3}
          y={y - 1.5}
          width={6}
          height={4.5}
          rx={1}
          fill={AMBER}
        />
      </g>
    );
  }
  if (status === "pass") {
    return (
      <path
        d={`M${x - 2.4} ${y} l1.8 1.8 l3 -3.6`}
        fill="none"
        stroke={TEAL}
        strokeWidth={1.4}
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    );
  }
  return null;
}

interface PipelineCardProps {
  readonly runNumber: number;
  readonly stages: readonly StageSpec[];
  readonly caption?: string;
  readonly captionColor?: string;
  readonly pulse?: boolean;
  readonly fanInIndex?: number;
  readonly dropLeaderIndex?: number;
}

/**
 * The recurring object: Billing's deploy pipeline, redrawn at each beat with
 * the same chrome as `SchemaCard` (rx12, translucent fill, hairline stroke).
 * Two invariants weld the beats together across the section: the run number
 * always ticks upward, and the stage track changes by at most one stage.
 */
function PipelineCard({
  runNumber,
  stages,
  caption,
  captionColor,
  pulse = true,
  fanInIndex,
  dropLeaderIndex,
}: PipelineCardProps) {
  const step = 60;
  const width = 460;
  const height = 120;
  const trackY = 70;
  const firstX = (width - (stages.length - 1) * step) / 2;
  const lastX = firstX + (stages.length - 1) * step;
  const padTop = fanInIndex != null ? 56 : 0;
  const padBottom = dropLeaderIndex != null ? 48 : 0;
  const fx = fanInIndex != null ? firstX + fanInIndex * step : 0;
  const dx = dropLeaderIndex != null ? firstX + dropLeaderIndex * step : 0;

  return (
    <div className="w-full max-w-[460px]">
      <svg
        viewBox={`0 ${-padTop} ${width} ${height + padTop + padBottom}`}
        role="img"
        aria-label={`Run number ${runNumber}, ${stages.map((s) => `${s.label} ${s.status}`).join(", ")}`}
        className="h-auto w-full"
      >
        <rect
          x={0.75}
          y={0.75}
          width={width - 1.5}
          height={height - 1.5}
          rx={12}
          fill={CARD_BG}
          stroke={CARD_STROKE}
        />
        <rect
          x={14}
          y={12}
          width={10}
          height={10}
          rx={3}
          fill={BILLING.color}
        />
        <text
          x={30}
          y={21}
          fontFamily={MONO}
          fontSize={8.5}
          letterSpacing="0.14em"
          fill={INK_DIM}
        >
          BILLING · DEPLOY PIPELINE
        </text>
        <text
          x={width - 14}
          y={21}
          textAnchor="end"
          fontFamily={MONO}
          fontSize={8.5}
          fill={INK_DIM}
        >
          RUN #{runNumber}
        </text>
        <line
          x1={14}
          x2={width - 14}
          y1={30}
          y2={30}
          stroke="rgba(245,241,234,0.1)"
        />
        {stages.map((s, i) => {
          const x = firstX + i * step;
          return (
            <g key={i}>
              {i > 0 && (
                <line
                  x1={x - step}
                  x2={x}
                  y1={trackY}
                  y2={trackY}
                  stroke={SEGMENT}
                  strokeWidth={1.5}
                  strokeDasharray={s.dashed ? "4 4" : undefined}
                />
              )}
              <circle
                cx={x}
                cy={trackY}
                r={5}
                fill="#0d1424"
                stroke={STAGE_COLOR[s.status]}
                strokeWidth={1.5}
                strokeDasharray={s.dashed ? "2 2" : undefined}
              />
              <StageGlyph x={x} y={trackY} status={s.status} />
              <text
                x={x}
                y={trackY + 18}
                textAnchor="middle"
                fontFamily={MONO}
                fontSize={8.5}
                letterSpacing="0.05em"
                fill={INK_DIM}
              >
                {s.label}
              </text>
            </g>
          );
        })}
        {pulse && (
          <circle
            r={2.6}
            fill={TEAL}
            className="rnr-pulse motion-reduce:hidden"
            style={
              {
                offsetPath: `path("M${firstX} ${trackY} L${lastX} ${trackY}")`,
              } as CSSProperties
            }
          />
        )}
        {fanInIndex != null && (
          <>
            {CANON.map((s, i) => {
              const glyphCx = fx - 44 + 22 * i;
              return (
                <Fragment key={s.name}>
                  <line
                    x1={glyphCx}
                    y1={-30}
                    x2={fx}
                    y2={-3}
                    stroke={DASHED}
                    strokeWidth={1}
                    strokeDasharray="3 3"
                  />
                  <FileGlyphG cx={glyphCx} top={-50} color={s.color} />
                </Fragment>
              );
            })}
            <text
              x={fx}
              y={trackY + 32}
              textAnchor="middle"
              fontFamily={MONO}
              fontSize={9}
              letterSpacing="0.2em"
              fill={TEAL}
            >
              READS SCHEMAS ONLY
            </text>
          </>
        )}
        {dropLeaderIndex != null && (
          <line
            x1={dx}
            x2={dx}
            y1={height + 2}
            y2={height + 44}
            stroke={DASHED}
            strokeWidth={1}
            strokeDasharray="3 3"
          />
        )}
      </svg>
      {caption && (
        <p
          className="mt-2 font-mono text-[9px] tracking-[0.2em] uppercase"
          style={{ color: captionColor ?? INK_DIM }}
        >
          {caption}
        </p>
      )}
    </div>
  );
}

/** A dimmed 200x26-style header bar for a sibling team's own pipeline. */
function SiblingBar({
  color,
  label,
  extra,
}: {
  readonly color: string;
  readonly label: string;
  readonly extra?: ReactNode;
}) {
  return (
    <div
      className="flex items-center gap-2 rounded-lg border px-3 py-1.5 opacity-45"
      style={{ borderColor: CARD_STROKE, background: CARD_BG }}
    >
      <span
        className="h-2.5 w-2.5 shrink-0 rounded-[3px]"
        style={{ background: color }}
      />
      <span
        className="font-mono text-[9px] tracking-[0.15em] uppercase"
        style={{ color: INK_DIM }}
      >
        {label}
      </span>
      {extra}
    </div>
  );
}

/** Dashed, unfilled chip for something that does not exist yet. */
function GhostChip({ label }: { readonly label: string }) {
  return (
    <div
      className="inline-flex items-center rounded-lg border border-dashed px-3 py-1.5 font-mono text-[9px] tracking-[0.15em] uppercase"
      style={{ borderColor: DASHED, color: INK_DIM }}
    >
      {label}
    </div>
  );
}

/** A small colored client or ticket chip with a status dot. */
function DotChip({
  color,
  label,
}: {
  readonly color: string;
  readonly label: string;
}) {
  return (
    <div
      className="flex items-center gap-2 rounded-lg border px-3 py-1.5"
      style={{ borderColor: CARD_STROKE, background: CARD_BG }}
    >
      <span
        className="h-1.5 w-1.5 shrink-0 rounded-full"
        style={{ background: color }}
      />
      <span
        className="font-mono text-[9px] tracking-[0.15em] uppercase"
        style={{ color: "#c9d4e8" }}
      >
        {label}
      </span>
    </div>
  );
}

/** 14x18 folded-corner file glyph paths, positioned as a <g> inside an SVG. */
function FileGlyphG({
  cx,
  top,
  color,
}: {
  readonly cx: number;
  readonly top: number;
  readonly color: string;
}) {
  return (
    <g transform={`translate(${cx - 7} ${top})`}>
      <path
        d="M2 1 H9 L12 4 V16 A1 1 0 0 1 11 17 H2 A1 1 0 0 1 1 16 V2 A1 1 0 0 1 2 1 Z"
        fill={color}
        opacity={0.9}
      />
      <path d="M9 1 V4 H12" fill="none" stroke="#0d1424" strokeWidth={0.75} />
    </g>
  );
}

/** 14x18 folded-corner file glyph: a schema.graphql input, not a service. */
function FileGlyph({ color }: { readonly color: string }) {
  return (
    <svg
      viewBox="0 0 14 18"
      aria-hidden="true"
      className="h-[18px] w-[14px] shrink-0"
    >
      <FileGlyphG cx={7} top={0} color={color} />
    </svg>
  );
}

/** Beat 1 - RUN #481: the pipeline as a lone, working object. */
function Beat1() {
  return (
    <div className="flex flex-col gap-6">
      <PipelineCard
        runNumber={481}
        stages={[
          { label: "BUILD", status: "pass" },
          { label: "TEST", status: "pass" },
          { label: "DEPLOY", status: "pass" },
        ]}
        caption="SHIPS WHEN READY · NO ONE TO ASK"
      />
      <div>
        <MicroLabel>Five pipelines, five clocks</MicroLabel>
        <div className="mt-2 flex flex-col gap-1.5">
          {SIBLINGS.map((s) => (
            <SiblingBar
              key={s.name}
              color={s.color}
              label={`${s.name.toUpperCase()} · DEPLOY PIPELINE`}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

/** Beat 2 - RUN #492: a clean deploy the pipeline cannot see past. */
function Beat2() {
  return (
    <div className="flex flex-col gap-4">
      <PipelineCard
        runNumber={492}
        stages={[
          { label: "BUILD", status: "pass" },
          { label: "TEST", status: "pass" },
          { label: "DEPLOY", status: "pass" },
          { label: "AFTERMATH", status: "broken", dashed: true },
        ]}
        caption="THE PIPELINE CANNOT SEE THE CLIENTS"
        captionColor={CORAL}
      />
      <div
        className="inline-flex w-fit items-center rounded-lg border px-3 py-1.5 font-mono text-[10px] tracking-[0.15em] uppercase"
        style={{ borderColor: "rgba(242,119,101,0.4)", color: CORAL }}
      >
        6 client apps broken
      </div>
      <div className="flex flex-wrap gap-2">
        <DotChip color={CORAL} label="WEB · FIX MERGE CODE" />
        <DotChip color={CORAL} label="IOS · FIX MERGE CODE" />
        <DotChip color={CORAL} label="PARTNER · FIX MERGE CODE" />
      </div>
    </div>
  );
}

/** Beat 3 - RUN #495: a foreign gate wedged into someone else's pipeline. */
function Beat3() {
  return (
    <div className="flex flex-col gap-4">
      <PipelineCard
        runNumber={495}
        stages={[
          { label: "BUILD", status: "pass" },
          { label: "TEST", status: "pass" },
          { label: "REVIEW", status: "hold" },
          { label: "DEPLOY", status: "pending" },
        ]}
        caption="API TEAM REVIEW · HOLD · 9 DAYS"
        captionColor={AMBER}
      />
      <div>
        <MicroLabel>Every team&apos;s change, one gate</MicroLabel>
        <div className="mt-2 flex flex-wrap gap-2">
          {SIBLINGS.slice(0, 3).map((s) => (
            <DotChip
              key={s.name}
              color={s.color}
              label={`${s.name.toUpperCase()} · TICKET WAITING`}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

/** Beat 4 - RUN #501: an unchanged pipeline, pointed at a schema no one owns. */
function Beat4() {
  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <div className="flex flex-col items-start gap-3">
        <PipelineCard
          runNumber={501}
          stages={[
            { label: "BUILD", status: "pass" },
            { label: "TEST", status: "pass" },
            { label: "DEPLOY", status: "pass" },
          ]}
        />
        <span
          aria-hidden="true"
          className="h-6 w-px border-l border-dashed"
          style={{ borderColor: DASHED }}
        />
        <GhostChip label="Product page schema?" />
      </div>
      <ProtoCodeBox {...CHAPTERS[3].boxes[0]} />
    </div>
  );
}

/** Beat 5 - one schema, five authors: five file chips, one ghost, one zoom-in. */
function Beat5() {
  const [catalogBox, billingBox] = CHAPTERS[4].boxes;
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap justify-center gap-2">
        {CANON.map((s) => (
          <SiblingBar
            key={s.name}
            color={s.color}
            label={s.name.toUpperCase()}
            extra={<FileGlyph color={s.color} />}
          />
        ))}
      </div>
      <div className="flex justify-center">
        <GhostChip label="One schema · no single author" />
      </div>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <ProtoCodeBox {...catalogBox} />
        <ProtoCodeBox {...billingBox} />
      </div>
    </div>
  );
}

/** Beat 6 - RUN #517: one new stage, fed by files, deploying one team alone. */
function Beat6() {
  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
        <div className="flex flex-col items-start gap-2 lg:col-span-7">
          <PipelineCard
            runNumber={517}
            stages={[
              { label: "BUILD", status: "pass" },
              { label: "TEST", status: "pass" },
              { label: "COMPOSE", status: "pass" },
              { label: "DEPLOY", status: "pass" },
            ]}
            fanInIndex={2}
            dropLeaderIndex={2}
          />
          <p
            className="w-full max-w-[460px] text-right font-mono text-[9px] tracking-[0.2em] uppercase"
            style={{ color: TEAL }}
          >
            DEPLOYED: BILLING v19 · ONLY BILLING
          </p>
        </div>
        <div className="flex flex-col gap-1.5 lg:col-span-5">
          <MicroLabel>Separate clocks, intact</MicroLabel>
          <SiblingBar
            color={CANON[0].color}
            label="CATALOG · COMPOSE + DEPLOY"
            extra={
              <span
                className="ml-auto font-mono text-[8.5px]"
                style={{ color: INK_DIM }}
              >
                2H AGO
              </span>
            }
          />
          <SiblingBar
            color={CANON[2].color}
            label="ORDERING · COMPOSE + DEPLOY"
            extra={
              <span
                className="ml-auto font-mono text-[8.5px]"
                style={{ color: INK_DIM }}
              >
                3D AGO
              </span>
            }
          />
          <SiblingBar
            color={CANON[3].color}
            label="SHIPPING · COMPOSE + DEPLOY"
            extra={
              <span
                className="ml-auto font-mono text-[8.5px]"
                style={{ color: INK_DIM }}
              >
                12D AGO
              </span>
            }
          />
          <SiblingBar
            color={CANON[4].color}
            label="USER · COMPOSE + DEPLOY"
            extra={
              <span
                className="ml-auto font-mono text-[8.5px]"
                style={{ color: INK_DIM }}
              >
                6D AGO
              </span>
            }
          />
        </div>
      </div>
      <ProtoCodeBox {...CHAPTERS[5].boxes[0]} />
    </div>
  );
}

/** Beat 7 - runtime: the gateway, five services, and a ticker that never moves. */
function Beat7() {
  return (
    <div className="flex flex-col items-start gap-4">
      <svg
        viewBox="0 0 200 32"
        role="img"
        aria-label="Gateway"
        className="h-8 w-[200px]"
      >
        <GatewayChip x={100} y={16} w={184} label="SERVING COMPOSITE v9" />
      </svg>
      <div className="flex flex-wrap gap-2">
        {CANON.map((s) => (
          <span
            key={s.name}
            className="inline-block h-3.5 w-3.5 rounded-[3px]"
            style={{ background: s.color }}
          />
        ))}
      </div>
      <div
        className="rounded-lg border px-3 py-1.5 font-mono text-[9px] tracking-[0.15em] uppercase"
        style={{ borderColor: CARD_STROKE, background: CARD_BG, color: TEAL }}
      >
        RUN #520 · BILLING v20 DEPLOYED · COMPOSITE UNCHANGED · CLIENTS
        UNAFFECTED
      </div>
    </div>
  );
}

const BEATS: readonly (() => ReactNode)[] = [
  Beat1,
  Beat2,
  Beat3,
  Beat4,
  Beat5,
  Beat6,
  Beat7,
];

/**
 * Prototype v14 - "Run Number Rising": the whole section told as the
 * biography of one CI pipeline, Billing's. A single ~460px pipeline card is
 * redrawn at every beat; the run number always ticks upward and the stage
 * track changes by at most one stage, so the reader diffs states like
 * commits. Copy and artwork live in separate grid tracks with a real gap at
 * every beat - never absolute-positioned over one another - so the section
 * reflows to a stacked mobile order for free.
 */
export function RunNumberRising() {
  return (
    <PageSection maxWidth="6xl" className="pt-16 pb-20 sm:pt-20 sm:pb-28">
      <style>{`
        @keyframes rnr-pulse-travel {
          0% { offset-distance: 0%; opacity: 0; }
          8% { opacity: 0.95; }
          92% { opacity: 0.95; }
          100% { offset-distance: 100%; opacity: 0; }
        }
        .rnr-pulse { animation: rnr-pulse-travel 2.6s ease-in-out infinite; }
      `}</style>
      {CHAPTERS.map((chapter, i) => {
        const Beat = BEATS[i];
        const isRight = i % 2 === 1;
        return (
          <Fragment key={chapter.title}>
            <RevealOnScroll className={band}>
              <div>
                <BeatHead index={i} />
                {i === 3 || i === 4 || i === 5 ? (
                  <div className="mt-8 flex flex-col gap-8">
                    <div className="max-w-3xl">
                      <Copy>{chapter.body}</Copy>
                    </div>
                    <Beat />
                  </div>
                ) : (
                  <div className={`mt-8 ${grid}`}>
                    <div
                      className={`lg:col-span-5 lg:self-center ${isRight ? "lg:order-2" : "lg:order-1"}`}
                    >
                      <Beat />
                    </div>
                    <Copy
                      className={`lg:col-span-7 ${isRight ? "lg:order-1" : "lg:order-2"}`}
                    >
                      {chapter.body}
                    </Copy>
                  </div>
                )}
              </div>
            </RevealOnScroll>
            {i === 5 && <HorizonRule />}
          </Fragment>
        );
      })}
      <div className="border-cc-card-border mt-4 border-t pt-6 text-center">
        <MicroLabel>
          Run #520 · composite unchanged · clients unaffected
        </MicroLabel>
      </div>
    </PageSection>
  );
}
