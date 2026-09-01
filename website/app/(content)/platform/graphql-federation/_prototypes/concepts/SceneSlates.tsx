import { Fragment } from "react";
import type { ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

import { CANON, GatewayChip, INK_DIM, MicroLabel } from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";
import type { Chapter } from "../story";

const MONO = "ui-monospace, SFMono-Regular, Menlo, monospace";

type Mode = "RUNTIME" | "BUILD TIME";

/** Which of the five canonical services are lit ("on set") per beat, in
 * CANON order. This row is the section's argument: its truth value changes
 * beat to beat, culminating in beat 6's all-dim row (build time, no
 * services in the scene) and beat 7's all-lit callback. */
const ON_SET: readonly (readonly boolean[])[] = [
  [true, true, true, true, true],
  [true, true, true, true, true],
  [true, true, true, true, true],
  [true, true, false, true, false],
  [true, true, false, false, false],
  [false, false, false, false, false],
  [true, true, true, true, true],
];

const MODES: readonly Mode[] = [
  "RUNTIME",
  "RUNTIME",
  "RUNTIME",
  "RUNTIME",
  "RUNTIME",
  "BUILD TIME",
  "RUNTIME",
];

const CAPTIONS: readonly string[] = [
  "One screen · five owners · nothing between",
  "Five calls · five formats · five ways to fail",
  "One API · one team · one line",
  "Three fields asked · three fields back",
  "The whole schema · author: —",
  "Schemas on set · services off set",
  "One gateway · five services · still separate",
];

/** Copy sits left of the scene card on odd counts and right on even, so the
 * section never settles into a single fixed layout down its length. */
const SIDES: readonly ("left" | "right")[] = [
  "left",
  "right",
  "left",
  "right",
  "left",
  "right",
  "left",
];

/* ------------------------------------------------------------------ */
/* Slate chrome: header, ON SET row, caption plate                     */
/* ------------------------------------------------------------------ */

function OnSetRow({ lit }: { readonly lit: readonly boolean[] }) {
  const size = 8;
  const gap = 6;
  const w = CANON.length * size + (CANON.length - 1) * gap;

  return (
    <svg
      viewBox={`0 0 ${w} ${size}`}
      width={w}
      height={size}
      aria-hidden="true"
      className="block"
    >
      {CANON.map((service, i) => (
        <rect
          key={service.name}
          x={i * (size + gap)}
          y={0}
          width={size}
          height={size}
          rx={2}
          fill={lit[i] ? service.color : "none"}
          stroke={lit[i] ? "none" : service.color}
          strokeOpacity={lit[i] ? 1 : 0.4}
          strokeWidth={lit[i] ? 0 : 1.2}
        />
      ))}
    </svg>
  );
}

function SlateHeader({
  index,
  mode,
}: {
  readonly index: number;
  readonly mode: Mode;
}) {
  const counter = `0${index + 1}/07`;

  return (
    <div className="border-cc-card-border flex h-10 shrink-0 items-center justify-between gap-3 border-b px-4">
      <MicroLabel>{counter}</MicroLabel>
      <MicroLabel className="opacity-80">{mode}</MicroLabel>
      <span className="flex items-center gap-2">
        <MicroLabel className="hidden opacity-70 min-[420px]:inline">
          On set
        </MicroLabel>
        <OnSetRow lit={ON_SET[index]} />
      </span>
    </div>
  );
}

function CaptionPlate({ children }: { readonly children: ReactNode }) {
  return (
    <div className="border-cc-card-border border-t px-4 py-2 text-center">
      <MicroLabel className="opacity-80">{children}</MicroLabel>
    </div>
  );
}

interface SceneCardProps {
  readonly index: number;
  readonly children: ReactNode;
}

function SceneCard({ index, children }: SceneCardProps) {
  return (
    <div className="border-cc-card-border mx-auto w-full max-w-[42rem] overflow-hidden rounded-xl border bg-[#0d1424]">
      <SlateHeader index={index} mode={MODES[index]} />
      <div className="p-4">{children}</div>
      <CaptionPlate>{CAPTIONS[index]}</CaptionPlate>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Beat layout: copy in a 5-col track, scene card in a 7-col track      */
/* ------------------------------------------------------------------ */

interface BeatProps {
  readonly index: number;
  readonly title: string;
  readonly body: ReactNode;
  readonly children: ReactNode;
}

function Beat({ index, title, body, children }: BeatProps) {
  const isRight = SIDES[index] === "right";

  return (
    <RevealOnScroll>
      <div className="grid gap-8 py-14 sm:py-20 md:grid-cols-12 md:items-center md:gap-10">
        <div
          className={`max-w-xl md:col-span-5 ${isRight ? "md:order-last" : ""}`}
        >
          <h3 className="font-heading text-cc-heading text-h4 sm:text-h3 text-balance">
            {title}
          </h3>
          <div className="text-cc-ink mt-4 space-y-3 text-sm sm:text-base">
            {body}
          </div>
        </div>
        <div className="md:col-span-7">
          <SceneCard index={index}>{children}</SceneCard>
        </div>
      </div>
    </RevealOnScroll>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 1: one screen, five owners, no strokes between them             */
/* ------------------------------------------------------------------ */

const SCATTER_OFFSET = [0, 20, 6, 26, 12] as const;

function Beat1Body() {
  const box = CHAPTERS[0].boxes[0];

  return (
    <div className="flex flex-col gap-6 sm:flex-row sm:items-start sm:gap-10">
      <div className="sm:w-56">
        <ProtoCodeBox label={box.label} lines={box.lines} />
      </div>
      <div className="flex flex-1 flex-wrap items-start gap-x-6 gap-y-4">
        {CANON.map((service, i) => (
          <span
            key={service.name}
            style={{ marginTop: SCATTER_OFFSET[i] }}
            className="flex items-center gap-1.5"
          >
            <span
              className="inline-block h-2 w-2 rounded-full"
              style={{ background: service.color }}
            />
            <span className="text-cc-nav-label font-mono text-[9px] tracking-[0.12em] uppercase">
              {service.name}
            </span>
          </span>
        ))}
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 2: five calls, a client, short stubs into each row              */
/* ------------------------------------------------------------------ */

function FiveCallsCard({ box }: { readonly box: Chapter["boxes"][number] }) {
  return (
    <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
      <div className="flex items-center gap-2">
        <span className="rounded-full border border-[rgba(94,234,212,0.5)] px-2 py-0.5 font-mono text-[9px] tracking-[0.14em] text-[#5eead4] uppercase">
          Client
        </span>
        <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
          {box.label}
        </span>
      </div>
      <div className="border-cc-card-border mt-2 space-y-1 border-t pt-2 font-mono text-[12px] leading-6">
        {box.lines.map((line, i) => (
          <div key={i} className="flex items-center gap-2">
            <span
              aria-hidden="true"
              className="block h-px w-3 shrink-0 border-t border-dashed md:w-6"
              style={{ borderColor: line.dots?.[0] ?? "rgba(245,241,234,0.3)" }}
            />
            <span className="whitespace-pre text-[#c9d4e8]">{line.text}</span>
            {line.dots && line.dots.length > 0 && (
              <span className="ml-auto flex items-center gap-1">
                {line.dots.map((d, k) => (
                  <span
                    key={k}
                    className="inline-block h-2 w-2 rounded-full"
                    style={{ background: d }}
                  />
                ))}
              </span>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}

function Beat2Body() {
  return <FiveCallsCard box={CHAPTERS[1].boxes[0]} />;
}

/* ------------------------------------------------------------------ */
/* Beat 3: single-file queue behind one API team                       */
/* ------------------------------------------------------------------ */

const QUEUE_X = [70, 150, 230, 310, 390] as const;
const QUEUE_Y = 150;
const TEAM_BOX = { x: 460, y: 105, w: 200, h: 90 } as const;

function Beat3Art() {
  return (
    <svg
      viewBox="0 0 720 300"
      aria-hidden="true"
      className="mx-auto h-auto max-h-[320px] w-full"
    >
      {CANON.map((service, i) => (
        <rect
          key={service.name}
          x={QUEUE_X[i] - 14}
          y={QUEUE_Y - 14}
          width={28}
          height={28}
          rx={5}
          fill={service.color}
          opacity={0.85}
        />
      ))}
      <text
        x={QUEUE_X[4]}
        y={QUEUE_Y - 30}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.14em"
        fill="#f27765"
      >
        TICKET 047
      </text>
      <line
        x1={QUEUE_X[4] + 14}
        x2={TEAM_BOX.x}
        y1={QUEUE_Y}
        y2={TEAM_BOX.y + TEAM_BOX.h / 2}
        stroke="rgba(245,241,234,0.3)"
        strokeDasharray="3 5"
      />
      <rect
        x={TEAM_BOX.x}
        y={TEAM_BOX.y}
        width={TEAM_BOX.w}
        height={TEAM_BOX.h}
        rx={12}
        fill="rgba(12,19,34,0.6)"
        stroke="rgba(245,241,234,0.25)"
      />
      <text
        x={TEAM_BOX.x + TEAM_BOX.w / 2}
        y={TEAM_BOX.y + TEAM_BOX.h / 2 + 4}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={11}
        letterSpacing="0.16em"
        fill={INK_DIM}
      >
        ONE API TEAM
      </text>
    </svg>
  );
}

function Beat3Mobile() {
  return (
    <div className="space-y-3">
      {CANON.map((service, i) => (
        <div
          key={service.name}
          className="border-cc-card-border flex items-center gap-2 rounded-lg border px-3 py-2"
        >
          <span
            className="inline-block h-2.5 w-2.5 rounded-[3px]"
            style={{ background: service.color }}
          />
          <span className="text-cc-ink font-mono text-[10px] tracking-[0.15em] uppercase">
            {service.name}
          </span>
          {i === CANON.length - 1 && (
            <span className="ml-auto font-mono text-[9px] tracking-[0.1em] text-[#f27765] uppercase">
              Ticket 047
            </span>
          )}
        </div>
      ))}
      <div className="border-cc-card-border rounded-lg border px-3 py-2 text-center">
        <MicroLabel>One API team</MicroLabel>
      </div>
    </div>
  );
}

function Beat3Body() {
  return (
    <>
      <div className="hidden lg:block">
        <Beat3Art />
      </div>
      <div className="lg:hidden">
        <Beat3Mobile />
      </div>
    </>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 4: one query, exactly the fields it named                      */
/* ------------------------------------------------------------------ */

function ResponseCard() {
  const rows: readonly { readonly text: string; readonly color: string }[] = [
    { text: 'name: "Trail Runner"', color: CANON[0].color },
    { text: "price: 129.00", color: CANON[1].color },
    { text: 'delivery: "2 days"', color: CANON[3].color },
  ];

  return (
    <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-4">
      <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
        Response
      </span>
      <div className="border-cc-card-border mt-2 space-y-1 border-t pt-2 font-mono text-[12px] leading-6">
        {rows.map((row) => (
          <div key={row.text} className="flex items-center gap-2">
            <span className="whitespace-pre text-[#c9d4e8]">{row.text}</span>
            <span
              className="ml-auto inline-block h-2 w-2 rounded-full"
              style={{ background: row.color }}
            />
          </div>
        ))}
      </div>
    </div>
  );
}

function Beat4Body() {
  const box = CHAPTERS[3].boxes[0];

  return (
    <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:gap-6">
      <div className="flex-1">
        <ProtoCodeBox label={box.label} lines={box.lines} />
      </div>
      <div className="flex-1">
        <ResponseCard />
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 5: two schemas, one dashed and empty between them               */
/* ------------------------------------------------------------------ */

function DashedSchemaCard() {
  return (
    <div className="flex min-h-[7rem] min-w-0 flex-1 flex-col items-center justify-center rounded-xl border border-dashed border-[rgba(245,241,234,0.3)] p-4 text-center">
      <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase opacity-70">
        The whole schema
      </span>
      <span className="text-cc-nav-label mt-1 font-mono text-[10px] tracking-[0.2em] uppercase opacity-50">
        Author: —
      </span>
    </div>
  );
}

function Beat5Body() {
  const [catalog, billing] = CHAPTERS[4].boxes;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex-1">
        <ProtoCodeBox
          label={catalog.label}
          color={catalog.color}
          lines={catalog.lines}
        />
      </div>
      <DashedSchemaCard />
      <div className="flex-1">
        <ProtoCodeBox
          label={billing.label}
          color={billing.color}
          lines={billing.lines}
        />
      </div>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 6: schemas on set, services off set                            */
/* ------------------------------------------------------------------ */

const SHEET_X = [20, 130, 240, 350, 460] as const;
const SHEET_Y = 18;
const COMPOSE = { x: 245, y: 150 } as const;
const COMPOSITE_SHEET = { x: 470, y: 90, w: 220 } as const;

function SheetGlyph({
  x,
  color,
}: {
  readonly x: number;
  readonly color: string;
}) {
  return (
    <g transform={`translate(${x} ${SHEET_Y})`}>
      <path
        d="M6 0 H34 L46 12 V52 a6 6 0 0 1 -6 6 H6 a6 6 0 0 1 -6 -6 V6 a6 6 0 0 1 6 -6 Z"
        fill="rgba(12,19,34,0.65)"
        stroke="rgba(245,241,234,0.2)"
        strokeWidth={1}
      />
      <path d="M34 0 V12 H46 Z" fill="rgba(245,241,234,0.12)" />
      <rect x={0} y={0} width={8} height={8} rx={2} fill={color} />
      <line
        x1={8}
        y1={26}
        x2={38}
        y2={26}
        stroke={INK_DIM}
        strokeOpacity={0.5}
      />
      <line
        x1={8}
        y1={36}
        x2={38}
        y2={36}
        stroke={INK_DIM}
        strokeOpacity={0.5}
      />
      <line
        x1={8}
        y1={46}
        x2={38}
        y2={46}
        stroke={INK_DIM}
        strokeOpacity={0.5}
      />
    </g>
  );
}

function CompositeSheet() {
  const { x, y, w } = COMPOSITE_SHEET;
  const lines = CHAPTERS[5].boxes[0].lines;
  const rowH = 18;
  const h = 36 + lines.length * rowH;

  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height={h}
        rx={10}
        fill="rgba(12,19,34,0.6)"
        stroke="rgba(94,234,212,0.35)"
      />
      <text
        x={x + 14}
        y={y + 20}
        fontFamily={MONO}
        fontSize={9.5}
        letterSpacing="0.16em"
        fill="#5eead4"
      >
        COMPOSITE SCHEMA
      </text>
      {lines.map((line, i) => (
        <g key={i} transform={`translate(${x + 14} ${y + 36 + i * rowH})`}>
          <text
            fontFamily={MONO}
            fontSize={10.5}
            fill="#c9d4e8"
            xmlSpace="preserve"
          >
            {line.text}
          </text>
          {line.dots?.map((d, k) => (
            <circle key={k} cx={w - 22 - k * 12} cy={-3.5} r={3} fill={d} />
          ))}
        </g>
      ))}
    </g>
  );
}

function Beat6Art() {
  return (
    <svg
      viewBox="0 0 720 300"
      aria-hidden="true"
      className="mx-auto h-auto max-h-[320px] w-full"
    >
      {CANON.map((service, i) => (
        <SheetGlyph key={service.name} x={SHEET_X[i]} color={service.color} />
      ))}
      {SHEET_X.map((sx, i) => (
        <line
          key={CANON[i].name}
          x1={sx + 23}
          y1={SHEET_Y + 58}
          x2={COMPOSE.x}
          y2={COMPOSE.y - 13}
          stroke="rgba(245,241,234,0.22)"
          strokeDasharray="3 5"
        />
      ))}
      <GatewayChip x={COMPOSE.x} y={COMPOSE.y} label="COMPOSITION" w={140} />
      <line
        x1={COMPOSE.x + 70}
        y1={COMPOSE.y}
        x2={COMPOSITE_SHEET.x}
        y2={COMPOSE.y}
        stroke="rgba(94,234,212,0.4)"
        strokeDasharray="3 5"
      />
      <CompositeSheet />
    </svg>
  );
}

function Beat6Mobile() {
  const lines = CHAPTERS[5].boxes[0].lines;

  return (
    <div className="space-y-3">
      {CANON.map((service) => (
        <div
          key={service.name}
          className="border-cc-card-border flex items-center gap-2 rounded-lg border px-3 py-2"
        >
          <span
            className="inline-block h-2 w-2 rounded-full"
            style={{ background: service.color }}
          />
          <span className="text-cc-ink font-mono text-[10px] tracking-[0.15em] uppercase">
            {service.name} · schema.graphql
          </span>
        </div>
      ))}
      <div className="text-center">
        <MicroLabel className="opacity-70">Composition</MicroLabel>
      </div>
      <div className="rounded-lg border border-[rgba(94,234,212,0.35)] px-3 py-3">
        <MicroLabel className="text-[#5eead4]">Composite schema</MicroLabel>
        <div className="mt-2 space-y-1 font-mono text-[11px] leading-6 text-[#c9d4e8]">
          {lines.map((line, i) => (
            <div key={i} className="flex items-center gap-2">
              <span className="whitespace-pre">{line.text}</span>
              {line.dots && line.dots.length > 0 && (
                <span className="ml-auto flex items-center gap-1">
                  {line.dots.map((d, k) => (
                    <span
                      key={k}
                      className="inline-block h-2 w-2 rounded-full"
                      style={{ background: d }}
                    />
                  ))}
                </span>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function Beat6Body() {
  return (
    <>
      <div className="hidden lg:block">
        <Beat6Art />
      </div>
      <div className="lg:hidden">
        <Beat6Mobile />
      </div>
    </>
  );
}

/* ------------------------------------------------------------------ */
/* Beat 7: the gateway, five services relit behind it                  */
/* ------------------------------------------------------------------ */

const GATEWAY_POS = { x: 360, y: 60 } as const;
const SCATTER_POS = [
  { x: 120, y: 200 },
  { x: 230, y: 232 },
  { x: 340, y: 202 },
  { x: 460, y: 236 },
  { x: 570, y: 204 },
] as const;
/** Sample query from beat 4 asks for Catalog/Billing/Shipping only. */
const NEEDED = [0, 1, 3] as const;

function Beat7Art() {
  return (
    <svg
      viewBox="0 0 720 300"
      aria-hidden="true"
      className="mx-auto h-auto max-h-[320px] w-full"
    >
      {SCATTER_POS.map((p, i) => (
        <rect
          key={CANON[i].name}
          x={p.x - 8}
          y={p.y - 8}
          width={16}
          height={16}
          rx={3}
          fill={CANON[i].color}
        />
      ))}
      {SCATTER_POS.map((p, i) => (
        <text
          key={CANON[i].name}
          x={p.x}
          y={p.y + 26}
          textAnchor="middle"
          fontFamily={MONO}
          fontSize={8.5}
          letterSpacing="0.1em"
          fill={INK_DIM}
          opacity={0.85}
        >
          {CANON[i].name.toUpperCase()}
        </text>
      ))}
      {NEEDED.map((i) => (
        <line
          key={CANON[i].name}
          x1={GATEWAY_POS.x}
          y1={GATEWAY_POS.y + 30}
          x2={SCATTER_POS[i].x}
          y2={SCATTER_POS[i].y - 12}
          stroke={CANON[i].color}
          strokeOpacity={0.7}
          strokeDasharray="3 4"
        />
      ))}
      <rect
        x={GATEWAY_POS.x - 60}
        y={GATEWAY_POS.y + 20}
        width={120}
        height={26}
        rx={8}
        fill="rgba(12,19,34,0.7)"
        stroke="rgba(94,234,212,0.35)"
      />
      <text
        x={GATEWAY_POS.x}
        y={GATEWAY_POS.y + 37}
        textAnchor="middle"
        fontFamily={MONO}
        fontSize={9}
        letterSpacing="0.14em"
        fill="#5eead4"
      >
        COMPOSITE
      </text>
      <GatewayChip x={GATEWAY_POS.x} y={GATEWAY_POS.y} />
    </svg>
  );
}

function Beat7Mobile() {
  return (
    <div className="space-y-3">
      <div className="rounded-lg border border-[rgba(94,234,212,0.35)] px-3 py-3 text-center">
        <MicroLabel className="text-[#5eead4]">
          Gateway · composite schema
        </MicroLabel>
      </div>
      <div className="flex flex-wrap justify-center gap-3">
        {CANON.map((service) => (
          <span key={service.name} className="flex items-center gap-1.5">
            <span
              className="inline-block h-2 w-2 rounded-full"
              style={{ background: service.color }}
            />
            <span className="text-cc-ink font-mono text-[9px] tracking-[0.12em] uppercase">
              {service.name}
            </span>
          </span>
        ))}
      </div>
      <p className="text-cc-ink-dim text-center font-mono text-[9px] tracking-[0.12em] uppercase opacity-70">
        Calls: catalog · billing · shipping
      </p>
    </div>
  );
}

function Beat7Body() {
  return (
    <>
      <div className="hidden lg:block">
        <Beat7Art />
      </div>
      <div className="lg:hidden">
        <Beat7Mobile />
      </div>
    </>
  );
}

/* ------------------------------------------------------------------ */
/* Section                                                              */
/* ------------------------------------------------------------------ */

const BEAT_BODIES: readonly (() => ReactNode)[] = [
  Beat1Body,
  Beat2Body,
  Beat3Body,
  Beat4Body,
  Beat5Body,
  Beat6Body,
  Beat7Body,
];

/**
 * Prototype v15 - "Scene Slates": seven self-contained storyboard frames,
 * each topped by an identical production-slate header - `0N/07`, RUNTIME or
 * BUILD TIME, and an ON SET row of five service squares. The row's truth
 * value carries the argument: every square is lit in beats 1-3 and 7, a
 * subset in beats 4-5, and all five sit dimmed in beat 6, the only beat
 * that happens at build time - the frame itself states that services are
 * absent from composition. Beat 7 relights all five in beat 1's scatter
 * positions, the section's visual callback. Copy and artwork live in
 * separate 12-col grid tracks (never overlapping), alternating sides beat
 * to beat.
 */
export function SceneSlates() {
  return (
    <PageSection maxWidth="6xl">
      <div className="pb-6 text-center">
        <MicroLabel className="opacity-70">
          On set — which services take part in the scene.
        </MicroLabel>
      </div>
      {CHAPTERS.map((chapter, i) => {
        const Body = BEAT_BODIES[i];
        return (
          <Fragment key={i}>
            <Beat index={i} title={chapter.title} body={chapter.body}>
              <Body />
            </Beat>
          </Fragment>
        );
      })}
    </PageSection>
  );
}
