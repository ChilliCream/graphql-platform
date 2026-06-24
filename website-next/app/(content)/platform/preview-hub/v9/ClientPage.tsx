"use client";

import Link from "next/link";
import { motion, useReducedMotion } from "motion/react";
import { useId, type CSSProperties, type ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

/* -------------------------------------------------------------------------- */
/*  Concept: Hatched Atlas.                                                    */
/*  45-degree diagonal hatching is the connective tissue of the platform.      */
/*  Sections are stitched by full-bleed hatch seams instead of being spaced     */
/*  by gaps. Headings carry a small hatched swatch, tiles fly a hatch          */
/*  corner-flag, and the Nitro band weaves a hatch chevron pointing inward.    */
/* -------------------------------------------------------------------------- */

/* The single brand-spectrum event: one phrase in the hero headline. */
const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Hatch fills                                                               */
/*  Every hatch surface is a repeating-linear-gradient at 45deg. Two weights:  */
/*  standard (1px stroke every 8px) and dense (1px every 5px). Border-ink uses */
/*  cc-card-border; accent-ink uses a low-opacity cc-accent so accent surfaces */
/*  read as cool blueprint ink on the flat cc-bg page.                         */
/* -------------------------------------------------------------------------- */

const HATCH_STD: CSSProperties = {
  backgroundImage:
    "repeating-linear-gradient(45deg, var(--color-cc-card-border) 0 1px, transparent 1px 8px)",
};

const HATCH_DENSE: CSSProperties = {
  backgroundImage:
    "repeating-linear-gradient(45deg, var(--color-cc-card-border) 0 1px, transparent 1px 5px)",
};

const HATCH_ACCENT: CSSProperties = {
  backgroundImage:
    "repeating-linear-gradient(45deg, rgba(94, 234, 212, 0.5) 0 1px, transparent 1px 6px)",
};

/* -------------------------------------------------------------------------- */
/*  Hatch swatch                                                              */
/*  A small accent-hatched square. Used as the eyebrow / heading marker and    */
/*  as the legend keys, replacing the canonical underline rule.                */
/* -------------------------------------------------------------------------- */

interface HatchSwatchProps {
  readonly size?: number;
  readonly className?: string;
}

function HatchSwatch({ size = 28, className }: HatchSwatchProps) {
  return (
    <span
      aria-hidden
      className={`border-cc-card-border block shrink-0 rounded-[3px] border ${className ?? ""}`}
      style={{ ...HATCH_ACCENT, width: size, height: size }}
    />
  );
}

/* -------------------------------------------------------------------------- */
/*  Hatch band: the full-bleed seam between sections.                         */
/*  Grows from 0 to full width once when entering view. The full-bleed reach   */
/*  is achieved with symmetric negative margins inside the 1100px column.      */
/* -------------------------------------------------------------------------- */

interface HatchBandProps {
  readonly height: number;
  readonly dense?: boolean;
  readonly reduced: boolean;
  readonly label?: string;
}

function HatchBand({ height, dense, reduced, label }: HatchBandProps) {
  return (
    <div
      className="relative -mx-6 md:-mx-[calc((100vw-1100px)/2)]"
      role="separator"
      aria-label={label ?? "Section seam"}
    >
      <motion.div
        className="border-cc-card-border/60 origin-left border-y"
        style={{ ...(dense ? HATCH_DENSE : HATCH_STD), height }}
        initial={reduced ? { scaleX: 1 } : { scaleX: 0 }}
        whileInView={{ scaleX: 1 }}
        viewport={{ once: true, amount: 0.6 }}
        transition={{ duration: 0.6, ease: "easeOut" }}
      />
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Chevron seam: two hatch trapezoids meeting at center, pointing inward.     */
/*  Introduces the Nitro band, where the threads meet.                         */
/* -------------------------------------------------------------------------- */

function ChevronSeam({ reduced }: { readonly reduced: boolean }) {
  return (
    <div
      className="relative -mx-6 flex h-6 items-stretch md:-mx-[calc((100vw-1100px)/2)]"
      role="separator"
      aria-label="Chevron seam pointing toward Nitro"
    >
      <motion.div
        className="border-cc-card-border/60 h-full flex-1 border-y"
        style={{
          ...HATCH_DENSE,
          clipPath: "polygon(0 0, 100% 0, calc(100% - 18px) 100%, 0 100%)",
        }}
        initial={reduced ? { opacity: 1 } : { opacity: 0 }}
        whileInView={{ opacity: 1 }}
        viewport={{ once: true, amount: 0.6 }}
        transition={{ duration: 0.6, ease: "easeOut" }}
      />
      <motion.div
        className="border-cc-card-border/60 h-full flex-1 border-y"
        style={{
          ...HATCH_DENSE,
          clipPath: "polygon(18px 0, 100% 0, 100% 100%, 0 100%)",
        }}
        initial={reduced ? { opacity: 1 } : { opacity: 0 }}
        whileInView={{ opacity: 1 }}
        viewport={{ once: true, amount: 0.6 }}
        transition={{ duration: 0.6, ease: "easeOut", delay: 0.08 }}
      />
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Eyebrow: mono caps with a 2-stroke hatch flourish to the right.           */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label flex items-center gap-3 font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      <span>{children}</span>
      <span
        aria-hidden
        className="h-2 w-6"
        style={{
          backgroundImage:
            "repeating-linear-gradient(45deg, var(--color-cc-nav-label) 0 1px, transparent 1px 4px)",
        }}
      />
    </p>
  );
}

/* -------------------------------------------------------------------------- */
/*  Section heading: a left-edge hatch swatch instead of an underline.        */
/* -------------------------------------------------------------------------- */

interface HatchHeadingProps {
  readonly children: ReactNode;
  readonly as?: "h2" | "h3";
  readonly size?: string;
}

function HatchHeading({
  children,
  as = "h3",
  size = "text-h3",
}: HatchHeadingProps) {
  const Tag = as;
  return (
    <div className="flex items-start gap-4">
      <HatchSwatch className="mt-1.5" />
      <Tag
        className={`font-heading ${size} text-cc-heading font-semibold tracking-tight`}
      >
        {children}
      </Tag>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Corner flag: a 24x24 inline-SVG hatch triangle in the tile's top-right.    */
/*  Rotates 6deg on hover via the parent's group-hover.                        */
/* -------------------------------------------------------------------------- */

function CornerFlag() {
  const id = `hatchflag-${useId()}`;
  return (
    <span className="pointer-events-none absolute top-0 right-0 origin-top-right transition-transform duration-200 group-hover:rotate-6">
      <svg width="24" height="24" viewBox="0 0 24 24" aria-hidden>
        <defs>
          <pattern
            id={id}
            patternUnits="userSpaceOnUse"
            width="6"
            height="6"
            patternTransform="rotate(45)"
          >
            <line
              x1="0"
              y1="0"
              x2="0"
              y2="6"
              stroke="#5eead4"
              strokeOpacity="0.85"
              strokeWidth="1"
            />
          </pattern>
        </defs>
        <polygon points="24,0 24,24 0,0" fill={`url(#${id})`} />
        <polygon
          points="24,0 24,24 0,0"
          fill="none"
          stroke="#5eead4"
          strokeOpacity="0.4"
          strokeWidth="0.75"
        />
      </svg>
    </span>
  );
}

/* -------------------------------------------------------------------------- */
/*  Bucket model (ground truth reused verbatim from v1).                      */
/* -------------------------------------------------------------------------- */

type BucketKey = "build" | "run" | "evolve";

interface BucketTheme {
  readonly label: string;
  readonly intent: string;
}

const BUCKETS: Record<BucketKey, BucketTheme> = {
  build: { label: "Build", intent: "Author the API and let agents help." },
  run: {
    label: "Run",
    intent: "Operate it in production with eyes on every call.",
  },
  evolve: {
    label: "Evolve",
    intent: "Ship change without breaking published clients.",
  },
};

interface Tile {
  readonly id: string;
  readonly bucket: BucketKey;
  readonly title: string;
  readonly outcome: string;
  readonly href: string;
  readonly proofs: readonly string[];
}

const TILES: readonly Tile[] = [
  {
    id: "build",
    bucket: "build",
    title: "Build",
    outcome: "Ship from the code that runs it.",
    href: "/platform/build",
    proofs: [
      "Implementation-first GraphQL in C#",
      "Schema, resolvers, DataLoaders from one class",
      "Typed .NET clients out of the same source",
    ],
  },
  {
    id: "agentic-coding",
    bucket: "build",
    title: "Agentic Coding",
    outcome: "Give coding agents a feedback loop.",
    href: "/platform/agentic-coding",
    proofs: [
      "Typed contracts agents can read",
      "Diff and lint signal on every change",
      "Same loop a senior reviewer would run",
    ],
  },
  {
    id: "observability",
    bucket: "run",
    title: "Observability",
    outcome: "See what the API is doing, right now.",
    href: "/platform/observability",
    proofs: [
      "Operation-level traces and timings",
      "Field hot paths and N+1 detection",
      "OpenTelemetry export to your stack",
    ],
  },
  {
    id: "workflows",
    bucket: "run",
    title: "Workflows",
    outcome: "Let work continue after the request.",
    href: "/platform/workflows",
    proofs: [
      "Durable steps with retries",
      "Background jobs in the same model",
      "Resumable on cold start",
    ],
  },
  {
    id: "analytics",
    bucket: "run",
    title: "Analytics",
    outcome: "Know which fields earn their keep.",
    href: "/platform/analytics",
    proofs: [
      "Field-level usage over time",
      "Per-client adoption per type",
      "Spot dead fields before you cut",
    ],
  },
  {
    id: "ecosystem",
    bucket: "run",
    title: "Ecosystem",
    outcome: "An ecosystem you can trust and reuse.",
    href: "/platform/ecosystem",
    proofs: [
      "Banana Cake Pop IDE",
      "Strawberry Shake typed clients",
      "Green Donut DataLoaders",
    ],
  },
  {
    id: "release-safety",
    bucket: "evolve",
    title: "Release Safety",
    outcome: "Change contracts with a safety net.",
    href: "/platform/release-safety",
    proofs: [
      "Schema diff against published clients",
      "Breaking change flagged before merge",
      "Block, warn, or allow per rule",
    ],
  },
  {
    id: "continuous-integration",
    bucket: "evolve",
    title: "Continuous Integration",
    outcome: "Innovate with confidence at merge time.",
    href: "/platform/continuous-integration",
    proofs: [
      "Schema check on every pull request",
      "Composition validation across services",
      "Annotated diffs in code review",
    ],
  },
];

function tileBySlug(slug: string): Tile {
  const tile = TILES.find((t) => t.href.endsWith(slug));
  if (!tile) {
    throw new Error(`Unknown tile slug: ${slug}`);
  }
  return tile;
}

/* -------------------------------------------------------------------------- */
/*  Enter-view cascade variants (view-once, never scroll-coupled).            */
/* -------------------------------------------------------------------------- */

function cascadeContainer(reduced: boolean) {
  return {
    hidden: {},
    show: {
      transition: reduced ? {} : { staggerChildren: 0.07 },
    },
  };
}

function cascadeChild(reduced: boolean) {
  return {
    hidden: reduced ? { opacity: 1, y: 0 } : { opacity: 0, y: 8 },
    show: {
      opacity: 1,
      y: 0,
      transition: { duration: 0.4, ease: "easeOut" as const },
    },
  };
}

/* -------------------------------------------------------------------------- */
/*  Capability tile: cc-card-bg rectangle with a hatch corner-flag.           */
/* -------------------------------------------------------------------------- */

interface TileCardProps {
  readonly tile: Tile;
  readonly className?: string;
}

function TileCard({ tile, className }: TileCardProps) {
  return (
    <motion.div variants={cascadeChild(false)} className={className}>
      <Link
        href={tile.href}
        className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex h-full flex-col gap-3 overflow-hidden rounded-xl border p-5 no-underline transition-colors"
      >
        <CornerFlag />
        <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          {BUCKETS[tile.bucket].label}
        </p>
        <h4 className="font-heading text-h5 text-cc-heading group-hover:text-cc-accent font-semibold tracking-tight transition-colors">
          {tile.title}
        </h4>
        <p className="text-cc-ink text-[0.95rem] leading-relaxed">
          {tile.outcome}
        </p>
        <ul className="mt-1 flex flex-col gap-1.5">
          {tile.proofs.map((proof) => (
            <li
              key={proof}
              className="text-cc-ink-dim flex items-start gap-2 text-[0.82rem] leading-snug"
            >
              <span className="text-cc-accent mt-1 flex h-3 w-3 shrink-0 items-center justify-center">
                <CheckIcon size={12} />
              </span>
              <span>{proof}</span>
            </li>
          ))}
        </ul>
        <span className="text-cc-accent mt-auto pt-2 text-[0.82rem] font-medium">
          Open {tile.title} &rarr;
        </span>
      </Link>
    </motion.div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Pull-quote filler card (hatched), used in the Build bento row.            */
/* -------------------------------------------------------------------------- */

function QuoteFiller() {
  return (
    <motion.div
      variants={cascadeChild(false)}
      className="border-cc-card-border bg-cc-surface relative flex flex-col justify-between gap-4 overflow-hidden rounded-xl border p-5"
    >
      <span
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-30"
        style={HATCH_STD}
      />
      <p className="text-cc-nav-label relative font-mono text-[0.6rem] tracking-[0.22em] uppercase">
        On the loom
      </p>
      <p className="font-heading text-cc-heading relative text-[1.25rem] leading-snug font-semibold tracking-tight">
        Implementation-first GraphQL in C#: your resolvers are the schema, so
        the contract is woven from the code that serves it.
      </p>
      <p className="text-cc-ink-dim relative text-[0.82rem] leading-snug">
        Hot Chocolate is source-generated. Strawberry Shake spins typed .NET
        clients from the same source through MSBuild codegen.
      </p>
    </motion.div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hatched sparkline: an inline SVG trend line whose baseline is hatched.     */
/*  Lives in the Observability tile.                                          */
/* -------------------------------------------------------------------------- */

function HatchedSparkline() {
  return (
    <div
      className="border-cc-card-border bg-cc-surface/60 relative mt-1 h-24 overflow-hidden rounded-lg border"
      aria-hidden
    >
      <svg viewBox="0 0 320 96" className="h-full w-full">
        <defs>
          <pattern
            id="spark-baseline"
            patternUnits="userSpaceOnUse"
            width="6"
            height="6"
            patternTransform="rotate(45)"
          >
            <line
              x1="0"
              y1="0"
              x2="0"
              y2="6"
              stroke="#5eead4"
              strokeOpacity="0.7"
              strokeWidth="1"
            />
          </pattern>
        </defs>
        <rect
          x="0"
          y="80"
          width="320"
          height="16"
          fill="url(#spark-baseline)"
        />
        <path
          d="M10 70 L48 54 L86 62 L132 34 L178 48 L222 26 L268 40 L310 20"
          fill="none"
          stroke="#5eead4"
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        />
        <path
          d="M10 80 L48 76 L86 72 L132 66 L178 68 L222 58 L268 60 L310 52"
          fill="none"
          stroke="var(--color-cc-ink-dim)"
          strokeOpacity="0.5"
          strokeWidth="1.5"
          strokeLinecap="round"
        />
        <text
          x="10"
          y="14"
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="9"
          fill="var(--color-cc-ink-dim)"
        >
          p95 latency
        </text>
      </svg>
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                      */
/* -------------------------------------------------------------------------- */

interface HeroProps {
  readonly reduced: boolean;
}

function Hero({ reduced }: HeroProps) {
  return (
    <header className="grid items-start gap-10 md:grid-cols-[1fr_auto]">
      <div className="flex flex-col gap-6">
        <Eyebrow>GraphQL Platform // Atlas</Eyebrow>
        <h1 className="font-heading text-hero text-cc-heading max-w-4xl font-semibold tracking-tight">
          Eight surfaces,{" "}
          <span
            className="bg-clip-text text-transparent"
            style={{ backgroundImage: SPECTRUM }}
          >
            woven into one
          </span>{" "}
          GraphQL platform.
        </h1>
        <p className="text-cc-ink lead max-w-2xl">
          The ChilliCream platform covers the full life of a GraphQL API, from
          the first resolver to the next breaking change. Each surface is a
          thread; the seams below show how they are stitched together.
        </p>
        <div className="flex flex-wrap items-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs">Read the Docs</OutlineButton>
        </div>
      </div>

      {/* Tall hatched legend strip: the three bucket weights as hatch keys. */}
      <motion.aside
        className="border-cc-card-border bg-cc-surface/60 relative hidden w-64 flex-col gap-5 overflow-hidden rounded-xl border p-5 md:flex"
        initial={reduced ? { opacity: 1 } : { opacity: 0, y: 8 }}
        whileInView={{ opacity: 1, y: 0 }}
        viewport={{ once: true, amount: 0.4 }}
        transition={{ duration: 0.5, ease: "easeOut" }}
      >
        <span
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-20"
          style={HATCH_STD}
        />
        <p className="text-cc-nav-label relative font-mono text-[0.6rem] tracking-[0.22em] uppercase">
          Legend // weights
        </p>
        {(Object.keys(BUCKETS) as BucketKey[]).map((key) => {
          const theme = BUCKETS[key];
          const count = TILES.filter((t) => t.bucket === key).length;
          return (
            <div key={key} className="relative flex items-start gap-3">
              <HatchSwatch size={24} className="mt-0.5" />
              <div className="flex flex-col">
                <span className="text-cc-heading font-mono text-[0.7rem] tracking-[0.1em] uppercase">
                  {theme.label}
                </span>
                <span className="text-cc-ink-dim text-[0.75rem] leading-snug">
                  {count} {count === 1 ? "surface" : "surfaces"}
                </span>
              </div>
            </div>
          );
        })}
      </motion.aside>
    </header>
  );
}

/* -------------------------------------------------------------------------- */
/*  Legend row: three inline cards keying the bento below.                    */
/* -------------------------------------------------------------------------- */

interface LegendRowProps {
  readonly reduced: boolean;
}

function LegendRow({ reduced }: LegendRowProps) {
  return (
    <motion.div
      className="grid gap-4 md:grid-cols-3"
      variants={cascadeContainer(reduced)}
      initial="hidden"
      whileInView="show"
      viewport={{ once: true, amount: 0.3 }}
    >
      {(Object.keys(BUCKETS) as BucketKey[]).map((key) => {
        const theme = BUCKETS[key];
        const count = TILES.filter((t) => t.bucket === key).length;
        return (
          <motion.div
            key={key}
            variants={cascadeChild(reduced)}
            className="border-cc-card-border bg-cc-card-bg flex items-start gap-4 rounded-xl border p-5"
          >
            <HatchSwatch className="mt-0.5" />
            <div className="flex flex-col gap-1">
              <span className="text-cc-heading font-mono text-[0.7rem] tracking-[0.12em] uppercase">
                {theme.label}{" "}
                <span className="text-cc-ink-dim">
                  &middot; {count} {count === 1 ? "capability" : "capabilities"}
                </span>
              </span>
              <span className="text-cc-ink-dim text-[0.82rem] leading-snug">
                {theme.intent}
              </span>
            </div>
          </motion.div>
        );
      })}
    </motion.div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Build bucket: bento row 4-3-5 (two tiles + a hatched pull-quote filler).   */
/* -------------------------------------------------------------------------- */

interface BucketProps {
  readonly reduced: boolean;
}

function BuildBucket({ reduced }: BucketProps) {
  return (
    <section className="flex flex-col gap-6">
      <Eyebrow>Bucket 01 // Build</Eyebrow>
      <HatchHeading>Author the API and let agents help.</HatchHeading>
      <motion.div
        className="grid auto-rows-fr gap-4 md:grid-cols-12"
        variants={cascadeContainer(reduced)}
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.2 }}
      >
        <TileCard
          tile={tileBySlug("/platform/build")}
          className="md:col-span-4"
        />
        <TileCard
          tile={tileBySlug("/platform/agentic-coding")}
          className="md:col-span-3"
        />
        <div className="md:col-span-5">
          <QuoteFiller />
        </div>
      </motion.div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Run bucket: 2x2 bento, Observability spans 2 columns with a sparkline.     */
/* -------------------------------------------------------------------------- */

function RunBucket({ reduced }: BucketProps) {
  return (
    <section className="flex flex-col gap-6">
      <Eyebrow>Bucket 02 // Run</Eyebrow>
      <HatchHeading>
        Operate it in production with eyes on every call.
      </HatchHeading>
      <motion.div
        className="grid auto-rows-fr gap-4 md:grid-cols-2"
        variants={cascadeContainer(reduced)}
        initial="hidden"
        whileInView="show"
        viewport={{ once: true, amount: 0.2 }}
      >
        <motion.div variants={cascadeChild(reduced)} className="md:col-span-2">
          <Link
            href={tileBySlug("/platform/observability").href}
            className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex h-full flex-col gap-3 overflow-hidden rounded-xl border p-5 no-underline transition-colors"
          >
            <CornerFlag />
            <div className="grid gap-5 md:grid-cols-[1fr_1.2fr] md:items-center">
              <div className="flex flex-col gap-3">
                <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
                  Run
                </p>
                <h4 className="font-heading text-h5 text-cc-heading group-hover:text-cc-accent font-semibold tracking-tight transition-colors">
                  Observability
                </h4>
                <p className="text-cc-ink text-[0.95rem] leading-relaxed">
                  See what the API is doing, right now.
                </p>
                <ul className="flex flex-col gap-1.5">
                  {tileBySlug("/platform/observability").proofs.map((proof) => (
                    <li
                      key={proof}
                      className="text-cc-ink-dim flex items-start gap-2 text-[0.82rem] leading-snug"
                    >
                      <span className="text-cc-accent mt-1 flex h-3 w-3 shrink-0 items-center justify-center">
                        <CheckIcon size={12} />
                      </span>
                      <span>{proof}</span>
                    </li>
                  ))}
                </ul>
                <span className="text-cc-accent text-[0.82rem] font-medium">
                  Open Observability &rarr;
                </span>
              </div>
              <HatchedSparkline />
            </div>
          </Link>
        </motion.div>
        <TileCard tile={tileBySlug("/platform/workflows")} />
        <TileCard tile={tileBySlug("/platform/analytics")} />
        <TileCard
          tile={tileBySlug("/platform/ecosystem")}
          className="md:col-span-2"
        />
      </motion.div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Evolve + Nitro composite band.                                            */
/*  Left: evolve tiles. Right: full-bleed Nitro callout on cc-surface with a   */
/*  hatched chevron weave pointing at the headline.                            */
/* -------------------------------------------------------------------------- */

function EvolveNitroBand({ reduced }: BucketProps) {
  return (
    <section className="flex flex-col gap-6">
      <Eyebrow>Bucket 03 // Evolve + Nitro</Eyebrow>
      <HatchHeading>
        Ship change without breaking published clients.
      </HatchHeading>
      <div className="grid gap-4 md:grid-cols-2">
        {/* Evolve tiles */}
        <motion.div
          className="flex flex-col gap-4"
          variants={cascadeContainer(reduced)}
          initial="hidden"
          whileInView="show"
          viewport={{ once: true, amount: 0.2 }}
        >
          <TileCard tile={tileBySlug("/platform/release-safety")} />
          <TileCard tile={tileBySlug("/platform/continuous-integration")} />
        </motion.div>

        {/* Nitro callout: cc-surface, chevron weave pointing inward. */}
        <motion.div
          className="border-cc-card-border bg-cc-surface relative flex flex-col gap-5 overflow-hidden rounded-xl border p-6 md:p-8"
          initial={reduced ? { opacity: 1 } : { opacity: 0, y: 8 }}
          whileInView={{ opacity: 1, y: 0 }}
          viewport={{ once: true, amount: 0.25 }}
          transition={{ duration: 0.5, ease: "easeOut" }}
        >
          <span
            aria-hidden
            className="pointer-events-none absolute top-0 left-0 h-full w-24 opacity-40"
            style={{
              ...HATCH_DENSE,
              clipPath: "polygon(0 0, 100% 50%, 0 100%)",
            }}
          />
          <div className="relative flex flex-col gap-4">
            <Eyebrow>Where the threads meet</Eyebrow>
            <h3 className="font-heading text-h4 text-cc-heading font-semibold tracking-tight">
              Nitro is the control plane that powers the platform.
            </h3>
            <p className="text-cc-ink leading-relaxed">
              Nitro is the hosted surface where the eight capabilities meet:
              schema registry, release checks, analytics, and traces all share
              one home. Connect a service, ship a change, and Nitro keeps the
              rest of the platform in sync.
            </p>
            <ul className="text-cc-ink-dim flex flex-col gap-1.5">
              {[
                "Schema registry for every environment",
                "Release checks against published clients",
                "Field usage and traces in one timeline",
              ].map((line) => (
                <li
                  key={line}
                  className="flex items-start gap-2 text-[0.88rem] leading-snug"
                >
                  <span className="text-cc-accent mt-1 flex h-3 w-3 shrink-0 items-center justify-center">
                    <CheckIcon size={12} />
                  </span>
                  <span>{line}</span>
                </li>
              ))}
            </ul>
            <div className="mt-1 flex flex-wrap items-center gap-3">
              <SolidButton href="https://nitro.chillicream.com">
                Open Nitro
              </SolidButton>
              <OutlineButton href="/products/nitro">About Nitro</OutlineButton>
            </div>
          </div>
        </motion.div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA inside a hatched frame (hatch border on all 4 sides).         */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="relative">
      {/* Hatched frame: four hatch edges, content inset. */}
      <span
        aria-hidden
        className="pointer-events-none absolute inset-x-0 top-0 h-2"
        style={HATCH_STD}
      />
      <span
        aria-hidden
        className="pointer-events-none absolute inset-x-0 bottom-0 h-2"
        style={HATCH_STD}
      />
      <span
        aria-hidden
        className="pointer-events-none absolute inset-y-0 left-0 w-2"
        style={HATCH_STD}
      />
      <span
        aria-hidden
        className="pointer-events-none absolute inset-y-0 right-0 w-2"
        style={HATCH_STD}
      />
      <div className="flex flex-col items-center gap-6 px-6 py-14 text-center md:px-12">
        <Eyebrow>Pick a surface</Eyebrow>
        <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
          Open the surface closest to today&apos;s problem.
        </h2>
        <p className="text-cc-ink-dim max-w-2xl text-[0.95rem] leading-relaxed">
          Every tile above is a real page, one thread in the cloth. Open the one
          that maps to the work in front of you, or start a project and let the
          platform fold in as you need it.
        </p>
        <div className="flex flex-wrap items-center justify-center gap-3">
          <SolidButton href="/get-started">Start for Free</SolidButton>
          <OutlineButton href="/docs">Read the Docs</OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export function ClientPage() {
  const reduced = useReducedMotion() ?? false;

  return (
    <div className="flex flex-col gap-12 py-6">
      <Hero reduced={reduced} />

      <HatchBand height={10} reduced={reduced} label="Legend seam" />
      <LegendRow reduced={reduced} />

      <HatchBand height={8} reduced={reduced} label="Build seam" />
      <BuildBucket reduced={reduced} />

      <HatchBand height={6} dense reduced={reduced} label="Run seam" />
      <RunBucket reduced={reduced} />

      <ChevronSeam reduced={reduced} />
      <EvolveNitroBand reduced={reduced} />

      <HatchBand height={4} reduced={reduced} label="Closing seam" />
      <ClosingCta />
    </div>
  );
}
