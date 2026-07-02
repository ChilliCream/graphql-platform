"use client";

import { motion, useReducedMotion } from "motion/react";
import type { CSSProperties, ReactNode } from "react";

import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  NitroDiagnose,
  NitroFusion,
  NitroMonitoring,
  NitroReel,
  NitroSchema,
  NitroTrace,
} from "@/src/nitro";

// ─── Manifest / Spec Sheet (v9) ────────────────────────────────────────────
// The Nitro control plane rendered as a printed manifest. Every capability
// is a mono-typed record (type / since / depends-on / surface), hung off a
// left hairline rail at col-1. The page reads like a spec sheet for a
// precision instrument, terse and declarative, with one fenced runtime
// dependencies block and a "$ run" footer row.

// Brand spectrum, used exactly once on this page as the title underline tape.
const SPECTRUM =
  "linear-gradient(90deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

// ─── DOTTED GRID BACKDROP ───────────────────────────────────────────────────
// A fixed faint dotted grid at 32px, radial-masked so it fades past 60vh.
// Single fixed-position layer behind everything. Pointer-events-none.
function DottedGridBackdrop() {
  const style: CSSProperties = {
    backgroundImage:
      "radial-gradient(circle at 1px 1px, rgba(245,241,234,0.10) 1px, transparent 1px)",
    backgroundSize: "32px 32px",
    backgroundPosition: "0 0",
    WebkitMaskImage:
      "radial-gradient(ellipse 70% 60% at 50% 0%, #000 0%, transparent 100%)",
    maskImage:
      "radial-gradient(ellipse 70% 60% at 50% 0%, #000 0%, transparent 100%)",
  };
  return (
    <div
      aria-hidden="true"
      className="pointer-events-none fixed inset-0 -z-10"
      style={style}
    />
  );
}

// ─── PRIMITIVES ─────────────────────────────────────────────────────────────

interface SpecLineProps {
  readonly field: string;
  readonly value: ReactNode;
  readonly delay: number;
  readonly reducedMotion: boolean;
}

/**
 * A single mono spec-list line: "field: value". On first scroll-into-view,
 * the line reveals with a clipped-width transition and small stagger. With
 * reduced motion it renders statically.
 */
function SpecLine({ field, value, delay, reducedMotion }: SpecLineProps) {
  if (reducedMotion) {
    return (
      <div className="font-mono text-[13px] leading-7">
        <span className="text-cc-nav-label">{field}:</span>{" "}
        <span className="text-cc-ink">{value}</span>
      </div>
    );
  }
  return (
    <motion.div
      className="overflow-hidden font-mono text-[13px] leading-7 whitespace-nowrap"
      initial={{ clipPath: "inset(0 100% 0 0)", opacity: 0 }}
      whileInView={{ clipPath: "inset(0 0% 0 0)", opacity: 1 }}
      viewport={{ once: true, margin: "-10% 0px -10% 0px" }}
      transition={{ duration: 0.45, delay, ease: "easeOut" }}
    >
      <span className="text-cc-nav-label">{field}:</span>{" "}
      <span className="text-cc-ink">{value}</span>
    </motion.div>
  );
}

interface DashedSeparatorProps {
  readonly label?: string;
}

function DashedSeparator({ label = "---" }: DashedSeparatorProps) {
  return (
    <div className="flex items-center gap-3 py-10" aria-hidden="true">
      <span className="text-cc-ink-dim font-mono text-[11px] tracking-[0.2em]">
        {label}
      </span>
      <span className="border-cc-card-border h-px flex-1 border-t border-dashed" />
    </div>
  );
}

interface CaptionStripProps {
  readonly children: ReactNode;
}

function CaptionStrip({ children }: CaptionStripProps) {
  return (
    <div className="border-cc-card-border bg-cc-bg/60 border-t px-3 py-2 font-mono text-[11px] tracking-[0.1em] uppercase">
      <span className="text-cc-nav-label">{children}</span>
    </div>
  );
}

interface CapabilityRecordProps {
  readonly idx: string;
  readonly slug: string;
  readonly fields: ReadonlyArray<readonly [string, ReactNode]>;
  readonly description: ReactNode;
  readonly visual: ReactNode;
  readonly captionLeft: string;
  readonly captionRight: string;
  readonly reducedMotion: boolean;
}

/**
 * A full capability spec record. Left column: 2-digit hex index + a mono
 * "# slug" heading. Right column: spec field list, one-sentence description,
 * and an embedded Nitro visual framed with a thin cc-card-border plus a mono
 * caption strip. Hovering the record lifts the border and shifts the index
 * hex to cc-accent.
 */
function CapabilityRecord({
  idx,
  slug,
  fields,
  description,
  visual,
  captionLeft,
  captionRight,
  reducedMotion,
}: CapabilityRecordProps) {
  return (
    <article className="group border-cc-card-border bg-cc-surface hover:border-cc-card-border-hover relative rounded-md border p-6 transition-colors sm:p-8">
      <div className="grid gap-8 lg:grid-cols-12 lg:gap-10">
        {/* Left: hex index + slug heading */}
        <div className="lg:col-span-4">
          <div className="flex items-baseline gap-3">
            <span className="text-cc-ink-dim group-hover:text-cc-accent font-mono text-[11px] tracking-[0.2em] tabular-nums transition-colors">
              {idx}
            </span>
            <span
              className="bg-cc-card-border h-px flex-1"
              aria-hidden="true"
            />
          </div>
          <h2 className="text-cc-heading mt-3 font-mono text-[18px] leading-tight font-medium tracking-tight">
            <span className="text-cc-ink-dim">#</span> {slug}
          </h2>
        </div>

        {/* Right: spec fields + description + framed visual */}
        <div className="lg:col-span-8">
          <dl className="border-cc-card-border space-y-0 border-l pl-4">
            {fields.map(([field, value], i) => (
              <SpecLine
                key={field}
                field={field}
                value={value}
                delay={i * 0.06}
                reducedMotion={reducedMotion}
              />
            ))}
          </dl>
          <p className="text-cc-ink mt-5 max-w-2xl text-base leading-relaxed">
            {description}
          </p>
          <div className="border-cc-card-border mt-6 overflow-hidden rounded-md border">
            <div className="border-cc-card-border bg-cc-bg/60 flex items-center justify-between border-b px-3 py-2 font-mono text-[11px] tracking-[0.1em] uppercase">
              <span className="text-cc-nav-label">{captionLeft}</span>
              <span className="text-cc-ink-dim">{captionRight}</span>
            </div>
            {visual}
            <CaptionStrip>render: live nitro preview</CaptionStrip>
          </div>
        </div>
      </div>
    </article>
  );
}

// ─── PAGE ───────────────────────────────────────────────────────────────────

export function ClientPage() {
  const prefersReducedMotion = useReducedMotion();
  const reducedMotion = prefersReducedMotion ?? false;

  return (
    <>
      <DottedGridBackdrop />

      <div className="mx-auto max-w-6xl px-6">
        {/* ─── MANIFEST HEADER ──────────────────────────────────────────── */}
        <section className="pt-10 pb-12 sm:pt-16">
          <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.25em] uppercase">
            $ cat manifest.nitro
          </p>

          <h1 className="text-cc-heading font-heading text-h1 mt-6 inline-block leading-none">
            <span className="font-mono text-[clamp(2.25rem,5vw,3.25rem)] tracking-tight">
              manifest.nitro
            </span>
            <span
              aria-hidden="true"
              className="mt-2 block h-[2px] w-full"
              style={{ background: SPECTRUM }}
            />
          </h1>

          {/* 3-row spec table */}
          <dl className="border-cc-card-border mt-10 max-w-xl border-l pl-4">
            <SpecLine
              field="name"
              value="nitro"
              delay={0}
              reducedMotion={reducedMotion}
            />
            <SpecLine
              field="kind"
              value="control-plane"
              delay={0.06}
              reducedMotion={reducedMotion}
            />
            <SpecLine
              field="version"
              value="current"
              delay={0.12}
              reducedMotion={reducedMotion}
            />
          </dl>

          <p className="text-cc-ink mt-8 max-w-2xl text-lg leading-relaxed">
            Nitro is the GraphQL control plane for{" "}
            <span className="text-cc-heading font-medium">Hot Chocolate</span>{" "}
            and <span className="text-cc-heading font-medium">Fusion</span>: one
            cockpit to author operations, observe with OpenTelemetry, trace
            requests, diagnose errors, and evolve schemas without breaking
            published clients.
          </p>

          <div className="mt-8 flex flex-wrap items-center gap-4">
            <SolidButton href="/get-started">install</SolidButton>
            <OutlineButton href="https://nitro.chillicream.com">
              open ide
            </OutlineButton>
          </div>
        </section>

        {/* ─── CAPABILITY RECORDS ───────────────────────────────────────── */}
        <section className="border-cc-card-border border-t pt-12">
          <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.25em] uppercase">
            capabilities[]
          </p>

          <div className="mt-8 flex flex-col">
            <CapabilityRecord
              idx="01"
              slug="schema-registry"
              fields={[
                ["type", "registry"],
                ["since", "v15"],
                ["depends-on", "hot-chocolate"],
                ["surface", "cli + nitro"],
              ]}
              description="Publish schemas from CI, diff every change against the registry, and see the published clients affected before you ship. Breaking changes get caught at PR time, not in production."
              visual={<NitroSchema className="w-full" />}
              captionLeft="evidence/schema-registry.tsx"
              captionRight="ui: nitro"
              reducedMotion={reducedMotion}
            />

            <DashedSeparator />

            <CapabilityRecord
              idx="02"
              slug="distributed-tracing"
              fields={[
                ["type", "telemetry"],
                ["since", "v15"],
                ["depends-on", "opentelemetry"],
                ["surface", "nitro ui"],
              ]}
              description="Per-request waterfalls that stitch a single GraphQL operation across resolvers, downstream services, and background work, so you walk the span tree down to the call that actually ran slow."
              visual={<NitroTrace className="w-full" />}
              captionLeft="evidence/trace-waterfall.tsx"
              captionRight="ui: nitro"
              reducedMotion={reducedMotion}
            />

            <DashedSeparator />

            <CapabilityRecord
              idx="03"
              slug="diagnose"
              fields={[
                ["type", "error-explorer"],
                ["since", "v15"],
                ["depends-on", "telemetry"],
                ["surface", "nitro ui"],
              ]}
              description="Production errors grouped by signature, with the stack trace, request, and variables attached. From an error spike to the exact failing operation in two clicks, no log spelunking."
              visual={<NitroDiagnose className="w-full" />}
              captionLeft="evidence/diagnose.tsx"
              captionRight="ui: nitro"
              reducedMotion={reducedMotion}
            />

            <DashedSeparator />

            <CapabilityRecord
              idx="04"
              slug="fusion-composition"
              fields={[
                ["type", "planner"],
                ["since", "v15"],
                ["depends-on", "source-schemas"],
                ["surface", "cli + nitro"],
              ]}
              description="Fusion composes source schemas into a single gateway at planning time. The composed plan ships as an artifact and the gateway is always self-run, so routing stays on your infrastructure."
              visual={<NitroFusion className="w-full" />}
              captionLeft="evidence/fusion-plan.tsx"
              captionRight="ui: nitro"
              reducedMotion={reducedMotion}
            />

            <DashedSeparator />

            <CapabilityRecord
              idx="05"
              slug="monitoring"
              fields={[
                ["type", "metrics"],
                ["since", "v15"],
                ["depends-on", "opentelemetry"],
                ["surface", "nitro ui"],
              ]}
              description="Latency, throughput, and error rate per operation, with p95 and p99, per-client usage, and an impact score. The metrics are driven by OpenTelemetry, configured by Nitro."
              visual={<NitroMonitoring className="w-full" />}
              captionLeft="evidence/monitoring.tsx"
              captionRight="ui: nitro"
              reducedMotion={reducedMotion}
            />

            <DashedSeparator />

            <CapabilityRecord
              idx="06"
              slug="graphql-ide"
              fields={[
                ["type", "ide"],
                ["since", "v15"],
                ["depends-on", "endpoint"],
                ["surface", "nitro + embed"],
              ]}
              description="Author operations against your live endpoint with schema-aware completion, save them to a shared workspace, and embed the IDE wherever your team needs it."
              visual={<NitroReel className="w-full" />}
              captionLeft="evidence/graphql-ide.tsx"
              captionRight="ui: nitro"
              reducedMotion={reducedMotion}
            />
          </div>
        </section>

        {/* ─── RUNTIME / DEPENDENCIES BLOCK ─────────────────────────────── */}
        <section className="border-cc-card-border mt-20 border-t pt-12">
          <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.25em] uppercase">
            runtime/dependencies.yaml
          </p>

          <div className="border-cc-card-border bg-cc-surface mt-6 overflow-hidden rounded-md border">
            {/* Title bar with traffic lights */}
            <div className="border-cc-card-border flex items-center gap-3 border-b px-4 py-2.5">
              <span
                aria-hidden="true"
                className="size-2.5 rounded-full bg-[#f0786a]"
              />
              <span
                aria-hidden="true"
                className="size-2.5 rounded-full bg-[#7c92c6]"
              />
              <span
                aria-hidden="true"
                className="bg-cc-accent size-2.5 rounded-full"
              />
              <span className="text-cc-nav-label ml-2 font-mono text-[11px] tracking-[0.15em] uppercase">
                runtime/dependencies.yaml
              </span>
            </div>

            {/* Code body */}
            <pre className="text-cc-ink overflow-x-auto px-5 py-5 font-mono text-[13px] leading-7">
              <code>
                <span className="text-cc-nav-label">runtime:</span>{" "}
                <span className="text-cc-heading">nitro</span>
                {"\n"}
                <span className="text-cc-nav-label">depends-on:</span>
                {"\n"}
                {"  - "}
                <span className="text-cc-heading">hot-chocolate</span>
                {"\n"}
                {"      "}
                <span className="text-cc-nav-label">kind:</span>{" "}
                <span className="text-cc-ink">source-generated-server</span>
                {"\n"}
                {"      "}
                <span className="text-cc-nav-label">role:</span>{" "}
                <span className="text-cc-ink">graphql runtime</span>
                {"\n"}
                {"  - "}
                <span className="text-cc-heading">strawberry-shake</span>
                {"\n"}
                {"      "}
                <span className="text-cc-nav-label">kind:</span>{" "}
                <span className="text-cc-ink">msbuild-codegen</span>
                {"\n"}
                {"      "}
                <span className="text-cc-nav-label">role:</span>{" "}
                <span className="text-cc-ink">typed graphql client</span>
                {"\n"}
                {"  - "}
                <span className="text-cc-heading">fusion</span>
                {"\n"}
                {"      "}
                <span className="text-cc-nav-label">kind:</span>{" "}
                <span className="text-cc-ink">planning-time-composition</span>
                {"\n"}
                {"      "}
                <span className="text-cc-nav-label">gateway:</span>{" "}
                <span className="text-cc-ink">self-run</span>
                {"\n"}
                {"  - "}
                <span className="text-cc-heading">mocha</span>
                {"\n"}
                {"      "}
                <span className="text-cc-nav-label">kind:</span>{" "}
                <span className="text-cc-ink">saga orchestrator</span>
                {"\n"}
                {"      "}
                <span className="text-cc-nav-label">processing:</span>{" "}
                <span className="text-cc-ink">exactly-once</span>
                {"\n"}
              </code>
            </pre>
            <CaptionStrip>
              note: mocha sagas are validated before traffic; mocha provides
              exactly-once processing
            </CaptionStrip>
          </div>
        </section>

        {/* ─── FOOTER: $ run ────────────────────────────────────────────── */}
        <section className="border-cc-card-border mt-20 border-t pt-12 pb-24">
          <div className="border-cc-card-border bg-cc-surface rounded-md border p-6 sm:p-8">
            <div className="flex flex-wrap items-center justify-between gap-6">
              <div>
                <p className="text-cc-nav-label font-mono text-[11px] tracking-[0.25em] uppercase">
                  manifest/footer
                </p>
                <p className="text-cc-heading mt-2 font-mono text-[18px]">
                  <span className="text-cc-accent">$</span> run
                </p>
              </div>
              <div className="flex flex-wrap items-center gap-4">
                <SolidButton href="/get-started">install</SolidButton>
                <OutlineButton href="/docs/nitro">read the docs</OutlineButton>
              </div>
            </div>

            <div className="border-cc-card-border mt-8 border-t border-dashed pt-6">
              <p className="text-cc-ink-dim font-mono text-[12px] leading-7">
                <span className="text-cc-accent">$</span> nitro --version{" "}
                <span className="text-cc-ink">
                  {"// control plane, current"}
                </span>
                <br />
                <span className="text-cc-accent">$</span> nitro init{" "}
                <span className="text-cc-ink">
                  {"// wire up the cockpit for your graphql api"}
                </span>
                <br />
                <span className="text-cc-accent">$</span>{" "}
                {reducedMotion ? (
                  <span
                    aria-hidden="true"
                    className="bg-cc-accent inline-block h-[1em] w-[0.55em] translate-y-[2px] align-baseline"
                  />
                ) : (
                  <motion.span
                    aria-hidden="true"
                    className="bg-cc-accent inline-block h-[1em] w-[0.55em] translate-y-[2px] align-baseline"
                    animate={{ opacity: [1, 0, 1] }}
                    transition={{
                      duration: 1.1,
                      repeat: Infinity,
                      ease: "linear",
                    }}
                  />
                )}
              </p>
            </div>
          </div>
        </section>
      </div>
    </>
  );
}
