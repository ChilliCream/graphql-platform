import type { ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

import { CANON, GatewayChip, INK_DIM, MicroLabel } from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";

const TEAL = "#5eead4";

/** Long form used only in the table of contents' first row. */
const TOC_TALLIES: readonly string[] = [
  "5 · 5 — the tension",
  "5 · 5",
  "1 · 1",
  "1 · ?",
  "1 · ?",
  "1 · 5",
  "1 · 5",
];

/** Short form repeated as each band's running head. */
const RUNNING_TALLIES: readonly string[] = [
  "5 · 5",
  "5 · 5",
  "1 · 1",
  "1 · ?",
  "1 · ?",
  "1 · 5",
  "1 · 5",
];

/** Beats 6-7 (index 5-6) are the payoff: APIS has converged, rendered in teal. */
const TEAL_FROM = 5;

/** Deterministic deploy stamps, one per canon service, for the beat-6 strip. */
const DEPLOY_STAMPS: readonly string[] = [
  "deploy 14:02",
  "deploy 09:41",
  "deploy 22:15",
  "deploy 03:37",
  "deploy 17:58",
];

const folio = (i: number) => String(i + 1).padStart(2, "0");

function DottedLeader({ className }: { readonly className?: string }) {
  return (
    <span
      aria-hidden="true"
      className={`border-cc-ink-faint mx-1 min-w-8 flex-1 -translate-y-1 border-b border-dotted ${className ?? ""}`.trim()}
    />
  );
}

function Tally({
  text,
  teal,
}: {
  readonly text: string;
  readonly teal?: boolean;
}) {
  return (
    <span
      className="shrink-0 font-mono text-[11px] tracking-[0.18em]"
      style={{ color: teal ? TEAL : INK_DIM }}
    >
      {text}
    </span>
  );
}

/**
 * The number/title/leader/tally row shared by the TOC and every running head.
 * Below `sm` the title needs its own full-width line to wrap normally (a
 * single flex row would starve it down to one word per line), so the row
 * splits into two stacked lines on mobile and rejoins into one row at `sm`
 * via `sm:contents` on the second line's wrapper.
 */
function ChapterRow({
  folioText,
  title,
  titleClassName,
  tally,
  teal,
  subCaption,
}: {
  readonly folioText: string;
  readonly title: ReactNode;
  readonly titleClassName: string;
  readonly tally: string;
  readonly teal: boolean;
  readonly subCaption?: string;
}) {
  return (
    <div className="flex flex-col gap-1 sm:flex-row sm:items-baseline sm:gap-3">
      <div className="flex items-baseline gap-3">
        <span className="text-cc-nav-label w-5 shrink-0 font-mono text-[11px] tracking-[0.18em]">
          {folioText}
        </span>
        <span className={`min-w-0 flex-1 ${titleClassName}`}>{title}</span>
      </div>
      <div className="flex items-baseline gap-2 pl-8 sm:contents">
        <DottedLeader className="hidden sm:block" />
        <span className="flex shrink-0 items-baseline gap-2">
          <Tally text={tally} teal={teal} />
          {subCaption && (
            <span className="text-cc-nav-label font-mono text-[9px] tracking-[0.2em] uppercase">
              {subCaption}
            </span>
          )}
        </span>
      </div>
    </div>
  );
}

/** Table-of-contents card: seven anchors, each ending in its APIS · TEAMS tally. */
function TocCard() {
  return (
    <div className="mx-auto max-w-[40rem]">
      <div className="border-cc-card-border rounded-xl border bg-[#0d1424] p-6">
        <MicroLabel>CH — APIS · TEAMS</MicroLabel>
        <ol className="border-cc-card-border mt-4 flex flex-col gap-4 border-t pt-4">
          {CHAPTERS.map((chapter, i) => (
            <li key={chapter.title}>
              <a href={`#ch-${i + 1}`} className="block no-underline">
                <ChapterRow
                  folioText={folio(i)}
                  title={chapter.title}
                  titleClassName="text-cc-ink text-sm"
                  tally={TOC_TALLIES[i]}
                  teal={i >= TEAL_FROM}
                />
              </a>
            </li>
          ))}
        </ol>
      </div>
      <p className="text-cc-ink-faint mt-4 text-center text-xs text-pretty">
        Each row counts what the screen still needs: APIS the client wants, then
        TEAMS behind it. Federation is the only answer that lands on 1 · 5.
      </p>
    </div>
  );
}

/** Running head repeated at the top of every chapter band. */
function RunningHead({
  index,
  subCaption,
}: {
  readonly index: number;
  readonly subCaption?: string;
}) {
  const chapter = CHAPTERS[index];
  const teal = index >= TEAL_FROM;

  return (
    <div
      id={`ch-${index + 1}`}
      className="border-cc-card-border scroll-mt-24 border-t pt-4"
    >
      <ChapterRow
        folioText={folio(index)}
        title={chapter.title}
        titleClassName="font-heading text-cc-heading text-sm sm:text-base"
        tally={RUNNING_TALLIES[index]}
        teal={teal}
        subCaption={subCaption}
      />
    </div>
  );
}

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

/** Small inline gateway glyph for the runtime beat: not worth a full canvas. */
function GatewayGlyph() {
  return (
    <svg
      viewBox="0 0 140 32"
      role="img"
      aria-label="Gateway"
      className="h-8 w-[140px]"
    >
      <GatewayChip x={70} y={16} w={116} />
    </svg>
  );
}

const band = "py-16 sm:py-24";
const grid = "grid grid-cols-1 gap-8 sm:grid-cols-12 sm:gap-10";

export function FrontMatter() {
  const [ch1, ch2, ch3, ch4, ch5, ch6, ch7] = CHAPTERS;
  const [catalogBox, billingBox] = ch5.boxes;

  return (
    <PageSection maxWidth="6xl" className="pt-16 pb-20 sm:pt-20 sm:pb-28">
      <RevealOnScroll>
        <TocCard />
      </RevealOnScroll>

      {/* Beat 1 - the tension: five teams, five fields, one screen. */}
      <RevealOnScroll className={band}>
        <div>
          <RunningHead index={0} />
          <div className={`mt-8 ${grid}`}>
            <Copy className="sm:col-span-7">{ch1.body}</Copy>
            <div className="sm:col-span-5 sm:self-center">
              <ProtoCodeBox {...ch1.boxes[0]} />
            </div>
          </div>
        </div>
      </RevealOnScroll>

      {/* Beat 2 - answer one: every app merges the data itself. */}
      <RevealOnScroll className={band}>
        <div>
          <RunningHead index={1} />
          <div className={`mt-8 ${grid}`}>
            <div className="sm:order-1 sm:col-span-5 sm:self-center">
              <ProtoCodeBox {...ch2.boxes[0]} />
            </div>
            <Copy className="sm:order-2 sm:col-span-7">{ch2.body}</Copy>
          </div>
        </div>
      </RevealOnScroll>

      {/* Beat 3 - answer two: one team, one queue. The tally is the graphic. */}
      <RevealOnScroll className={band}>
        <div>
          <RunningHead index={2} />
          <div className="mt-8 max-w-3xl">
            <Copy>{ch3.body}</Copy>
          </div>
        </div>
      </RevealOnScroll>

      {/* Beat 4 - GraphQL: one query. The question mark is the cliffhanger. */}
      <RevealOnScroll className={band}>
        <div>
          <RunningHead index={3} />
          <div className={`mt-8 ${grid}`}>
            <div className="sm:order-1 sm:col-span-5 sm:self-center">
              <ProtoCodeBox {...ch4.boxes[0]} />
            </div>
            <Copy className="sm:order-2 sm:col-span-7">{ch4.body}</Copy>
          </div>
        </div>
      </RevealOnScroll>

      {/* Beat 5 - one schema, no single team. Paired schemas, still 1 · ?. */}
      <RevealOnScroll className={band}>
        <div>
          <RunningHead index={4} />
          <div className="mt-8 max-w-3xl">
            <Copy>{ch5.body}</Copy>
          </div>
          <div className="mt-8 grid grid-cols-1 gap-6 sm:grid-cols-2">
            <ProtoCodeBox {...catalogBox} />
            <ProtoCodeBox {...billingBox} />
          </div>
        </div>
      </RevealOnScroll>

      {/* Beat 6 - the payoff: APIS collapses to 1, TEAMS stays 5. */}
      <RevealOnScroll className={band}>
        <div>
          <RunningHead index={5} />
          <div className="mt-8 max-w-3xl">
            <Copy>{ch6.body}</Copy>
          </div>
          <div className={`mt-8 ${grid}`}>
            <div className="sm:col-span-7">
              <ProtoCodeBox {...ch6.boxes[0]} />
            </div>
            <div className="sm:col-span-5">
              <MicroLabel>Still five services · separate deploys</MicroLabel>
              <div className="mt-3 flex flex-wrap gap-3">
                {CANON.map((service, i) => (
                  <div
                    key={service.name}
                    className="border-cc-card-border flex flex-col gap-1 rounded-lg border px-3 py-2"
                  >
                    <span className="flex items-center gap-2">
                      <span
                        className="inline-block h-2.5 w-2.5 rounded-[3px]"
                        style={{ background: service.color }}
                      />
                      <span className="font-mono text-[11px] tracking-[0.08em] text-[#c9d4e8]">
                        {service.name}
                      </span>
                    </span>
                    <span
                      className="font-mono text-[9px] tracking-[0.1em]"
                      style={{ color: INK_DIM }}
                    >
                      {DEPLOY_STAMPS[i]}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </RevealOnScroll>

      {/* Beat 7 - runtime: the gateway serves the composite schema. */}
      <RevealOnScroll className={band}>
        <div>
          <RunningHead index={6} subCaption="Runtime" />
          <div className={`mt-8 ${grid}`}>
            <Copy className="sm:col-span-7">{ch7.body}</Copy>
            <div className="flex flex-col items-start gap-3 sm:col-span-5 sm:self-center">
              <GatewayGlyph />
              <div className="flex flex-col gap-1 font-mono text-[11px] text-[#c9d4e8]">
                <span>price → Billing</span>
                <span>orders → Ordering</span>
                <span>delivery → Shipping</span>
              </div>
            </div>
          </div>
        </div>
      </RevealOnScroll>

      {/* Coda - the whole story in one line. */}
      <div className="border-cc-card-border mt-8 border-t pt-8 text-center">
        <span
          className="font-mono text-[11px] tracking-[0.2em] uppercase"
          style={{ color: TEAL }}
        >
          Apis 1 · Teams 5
        </span>
      </div>
    </PageSection>
  );
}
