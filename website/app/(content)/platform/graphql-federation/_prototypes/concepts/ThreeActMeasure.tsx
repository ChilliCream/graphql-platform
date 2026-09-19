import type { ReactNode } from "react";

import { PageSection } from "@/src/components/PageSection";
import { RevealOnScroll } from "@/src/components/RevealOnScroll";

import { CANON, GatewayChip, INK_DIM, MicroLabel } from "../Primitives";
import { CHAPTERS, ProtoCodeBox } from "../story";

/** Deterministic deploy stamps, one per canon service, for the beat-6 panel. */
const DEPLOY_STAMPS: readonly string[] = [
  "deploy 14:02",
  "deploy 09:41",
  "deploy 22:15",
  "deploy 03:37",
  "deploy 17:58",
];

const beat = "py-16 sm:py-24";
const grid = "grid grid-cols-1 gap-10 sm:grid-cols-12";

/**
 * The one repeated device that carries this concept: two full-width 1px
 * rules ~72px apart (via the py-10/py-5 padding), a mono eyebrow, and a
 * font-heading title between them. Reused, unchanged, for every act break so
 * the reader learns "this device means structural boundary" - which is why
 * the build/runtime horizon can inherit its weight without any drawn
 * geometry.
 */
function ActSlug({
  eyebrow,
  title,
}: {
  readonly eyebrow: string;
  readonly title: string;
}) {
  return (
    <div className="py-10">
      <div className="border-cc-card-border border-t" />
      <div className="flex flex-col gap-2 py-5 sm:flex-row sm:items-baseline sm:justify-between">
        <span className="text-cc-nav-label font-mono text-[10px] tracking-[0.2em] uppercase">
          {eyebrow}
        </span>
        <span className="font-heading text-cc-heading text-h6">{title}</span>
      </div>
      <div className="border-cc-card-border border-b" />
    </div>
  );
}

/**
 * Same two-rule device as `ActSlug`, but for the one moment that needs two
 * captions instead of an eyebrow/title pair: the build-time / runtime
 * horizon between beats 6 and 7.
 */
function HorizonSlug({
  left,
  right,
}: {
  readonly left: string;
  readonly right: string;
}) {
  return (
    <div className="py-10">
      <div className="border-cc-card-border border-t" />
      <div className="flex flex-col gap-2 py-5 sm:flex-row sm:items-baseline sm:justify-between">
        <span
          className="font-mono text-[10px] tracking-[0.2em] uppercase"
          style={{ color: INK_DIM }}
        >
          {left}
        </span>
        <span
          className="font-mono text-[10px] tracking-[0.2em] uppercase sm:text-right"
          style={{ color: INK_DIM }}
        >
          {right}
        </span>
      </div>
      <div className="border-cc-card-border border-b" />
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
      viewBox="0 0 200 30"
      role="img"
      aria-label="Gateway"
      className="h-8 w-[200px] max-w-full"
    >
      <GatewayChip x={100} y={15} w={140} />
    </svg>
  );
}

/**
 * Prototype v17 - Three-Act Measure: the seven story beats typeset as a
 * three-act essay whose column width narrates the argument. Act I alternates
 * wide left/right bands (the chaos of two bad trades), Act II collapses to a
 * single narrow centered measure (the schema as the story's subject), and
 * Act III opens back to full width (composition, then runtime). Five plain
 * act-break slugs - three act breaks, the build/runtime horizon, and a coda -
 * are the only full-width marks; every other colored pixel stays inside a
 * small box, in normal flow, in its own grid track next to (never under) its
 * copy.
 */
export function ThreeActMeasure() {
  const [ch1, ch2, ch3, ch4, ch5, ch6, ch7] = CHAPTERS;
  const [catalogBox, billingBox] = ch5.boxes;

  return (
    <PageSection maxWidth="6xl" className="pt-16 pb-20 sm:pt-20 sm:pb-28">
      <RevealOnScroll>
        <ActSlug eyebrow="Act I" title="TWO BAD TRADES" />
      </RevealOnScroll>

      {/* Beat 1 - wide: copy left, the five-team product page right. */}
      <div className={beat}>
        <div className={grid}>
          <Copy className="sm:col-span-7 sm:self-center">
            <h3 className="font-heading text-cc-heading text-h5 text-balance">
              {ch1.title}
            </h3>
            {ch1.body}
          </Copy>
          <div className="sm:col-span-5 sm:self-center">
            <ProtoCodeBox {...ch1.boxes[0]} />
          </div>
        </div>
      </div>

      {/* Beat 2 - mirrored: the five-call screen left, copy right. The
          alternation itself performs the every-app-repeats-the-work chaos. */}
      <div className={beat}>
        <div className={grid}>
          <div className="sm:order-1 sm:col-span-5 sm:self-center">
            <ProtoCodeBox {...ch2.boxes[0]} />
          </div>
          <Copy className="sm:order-2 sm:col-span-7 sm:self-center">
            <h3 className="font-heading text-cc-heading text-h5 text-balance">
              {ch2.title}
            </h3>
            {ch2.body}
          </Copy>
        </div>
      </div>

      {/* Beat 3 - full-measure copy, then the act's one pull-quote. */}
      <div className={beat}>
        <Copy className="mx-auto max-w-3xl">
          <h3 className="font-heading text-cc-heading text-h5 text-balance">
            {ch3.title}
          </h3>
          {ch3.body}
        </Copy>
        <p className="border-cc-card-border font-heading text-cc-heading text-h4 mx-auto mt-10 max-w-2xl border-y py-8 text-center text-balance">
          The queue becomes the bottleneck the services were split to avoid.
        </p>
      </div>

      <RevealOnScroll>
        <ActSlug eyebrow="Act II" title="ONE SCHEMA" />
      </RevealOnScroll>

      {/* Beat 4 - the narrow measure: copy, then the one query directly
          below, centered. The sudden width change reads as the story
          finding its subject. */}
      <div className={beat}>
        <div className="mx-auto max-w-xl space-y-8">
          <Copy>
            <h3 className="font-heading text-cc-heading text-h5 text-balance">
              {ch4.title}
            </h3>
            {ch4.body}
          </Copy>
          <ProtoCodeBox {...ch4.boxes[0]} />
        </div>
      </div>

      {/* Beat 5 - still narrow: copy, then the act's only two-up moment,
          quietly foreshadowing composition. */}
      <div className={beat}>
        <Copy className="mx-auto max-w-xl text-center">
          <h3 className="font-heading text-cc-heading text-h5 text-balance">
            {ch5.title}
          </h3>
          {ch5.body}
        </Copy>
        <div className="mt-8 flex flex-wrap justify-center gap-6">
          <div className="w-[min(100%,19rem)]">
            <ProtoCodeBox {...catalogBox} />
          </div>
          <div className="w-[min(100%,19rem)]">
            <ProtoCodeBox {...billingBox} />
          </div>
        </div>
      </div>

      <RevealOnScroll>
        <ActSlug eyebrow="Act III" title="COMPOSITION" />
      </RevealOnScroll>

      {/* Beat 6 - full width again: centered copy, then a wide two-panel
          row. Left, the composite schema at its largest, with per-row
          ownership dots already baked into the chapter data. Right, the
          five services, visibly untouched, each with its own deploy stamp -
          nothing here can be misread as services merging. */}
      <div className={beat}>
        <Copy className="mx-auto max-w-3xl text-center">
          <h3 className="font-heading text-cc-heading text-h5 text-balance">
            {ch6.title}
          </h3>
          {ch6.body}
        </Copy>
        <div className="mt-10 grid grid-cols-1 items-start gap-8 sm:grid-cols-2">
          <div className="sm:max-w-[26rem]">
            <ProtoCodeBox {...ch6.boxes[0]} />
          </div>
          <div className="border-cc-card-border rounded-xl border p-5">
            <MicroLabel>Services — unchanged</MicroLabel>
            <div className="border-cc-card-border mt-4 flex flex-col gap-3 border-t pt-4">
              {CANON.map((service, i) => (
                <div key={service.name} className="flex items-center gap-2">
                  <span
                    className="inline-block h-2.5 w-2.5 shrink-0 rounded-[3px]"
                    style={{ background: service.color }}
                  />
                  <span className="font-mono text-[11px] tracking-[0.08em] text-[#c9d4e8]">
                    {service.name}
                  </span>
                  <span
                    className="ml-auto font-mono text-[9px] tracking-[0.1em]"
                    style={{ color: INK_DIM }}
                  >
                    {DEPLOY_STAMPS[i]}
                  </span>
                </div>
              ))}
            </div>
            <p
              className="mt-4 font-mono text-[11px] leading-relaxed"
              style={{ color: INK_DIM }}
            >
              composition read their schemas. it never called them.
            </p>
          </div>
        </div>
      </div>

      <RevealOnScroll>
        <HorizonSlug
          left="Everything above this line: build time"
          right="Everything below: runtime"
        />
      </RevealOnScroll>

      {/* Beat 7 - runtime: an ordinary gateway, ordinary calls, no merging
          pipes. */}
      <div className={beat}>
        <div className={grid}>
          <Copy className="sm:col-span-7 sm:self-center">
            <h3 className="font-heading text-cc-heading text-h5 text-balance">
              {ch7.title}
            </h3>
            {ch7.body}
          </Copy>
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

      <RevealOnScroll>
        <ActSlug eyebrow="Curtain" title="ONE API · FIVE TEAMS" />
      </RevealOnScroll>
    </PageSection>
  );
}
