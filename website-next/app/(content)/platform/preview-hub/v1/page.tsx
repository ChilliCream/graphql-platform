import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "The ChilliCream Platform: Capability Map",
  description:
    "Map the ChilliCream GraphQL platform across Build, Run, and Evolve: eight capabilities and one Nitro control plane that route every GraphQL API forward.",
  keywords: [
    "ChilliCream platform",
    "GraphQL platform overview",
    "GraphQL build pipeline",
    "GraphQL observability",
    "GraphQL workflows",
    "GraphQL release safety",
    "GraphQL analytics",
    "continuous integration",
    "GraphQL ecosystem",
    "Nitro control plane",
  ],
  openGraph: {
    title: "The ChilliCream Platform: Capability Map",
    description:
      "Eight capabilities grouped under Build, Run, and Evolve, plus the Nitro control plane that ties them together for every API in your organization.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: the single gradient event on this screen.                 */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Bucket palette: each of the three buckets gets one of the brand stops,    */
/*  used only as a hairline accent so the spectrum itself stays single-use.   */
/* -------------------------------------------------------------------------- */

type BucketKey = "build" | "run" | "evolve";

interface BucketTheme {
  readonly label: string;
  readonly intent: string;
  readonly stop: string;
}

const BUCKETS: Record<BucketKey, BucketTheme> = {
  build: {
    label: "Build",
    intent: "Author the API and let agents help.",
    stop: "#16b9e4",
  },
  run: {
    label: "Run",
    intent: "Operate it in production with eyes on every call.",
    stop: "#7c92c6",
  },
  evolve: {
    label: "Evolve",
    intent: "Ship change without breaking published clients.",
    stop: "#f0786a",
  },
};

/* -------------------------------------------------------------------------- */
/*  Tile manifest                                                             */
/*  Eight platform surfaces, each routed to its real page. The bento span     */
/*  values let the grid breathe without a stock 3-up stack.                   */
/* -------------------------------------------------------------------------- */

interface Tile {
  readonly id: string;
  readonly bucket: BucketKey;
  readonly title: string;
  readonly outcome: string;
  readonly href: string;
  readonly span: "wide" | "tall" | "regular";
  readonly proofs: readonly string[];
  readonly art: (props: { readonly stop: string }) => ReactNode;
}

const TILES: readonly Tile[] = [
  {
    id: "build",
    bucket: "build",
    title: "Build",
    outcome: "Ship from the code that runs it.",
    href: "/platform/build",
    span: "wide",
    proofs: [
      "Implementation-first GraphQL in C#",
      "Schema, resolvers, DataLoaders from one class",
      "Typed .NET clients out of the same source",
    ],
    art: BuildArt,
  },
  {
    id: "agentic-coding",
    bucket: "build",
    title: "Agentic Coding",
    outcome: "Give coding agents a feedback loop.",
    href: "/platform/agentic-coding",
    span: "regular",
    proofs: [
      "Typed contracts agents can read",
      "Diff and lint signal on every change",
      "Same loop a senior reviewer would run",
    ],
    art: AgenticArt,
  },
  {
    id: "observability",
    bucket: "run",
    title: "Observability",
    outcome: "See what the API is doing, right now.",
    href: "/platform/observability",
    span: "tall",
    proofs: [
      "Operation-level traces and timings",
      "Field hot paths and N+1 detection",
      "OpenTelemetry export to your stack",
    ],
    art: ObservabilityArt,
  },
  {
    id: "workflows",
    bucket: "run",
    title: "Workflows",
    outcome: "Let work continue after the request.",
    href: "/platform/workflows",
    span: "regular",
    proofs: [
      "Durable steps with retries",
      "Background jobs in the same model",
      "Resumable on cold start",
    ],
    art: WorkflowsArt,
  },
  {
    id: "analytics",
    bucket: "run",
    title: "Analytics",
    outcome: "Know which fields earn their keep.",
    href: "/platform/analytics",
    span: "regular",
    proofs: [
      "Field-level usage over time",
      "Per-client adoption per type",
      "Spot dead fields before you cut",
    ],
    art: AnalyticsArt,
  },
  {
    id: "ecosystem",
    bucket: "run",
    title: "Ecosystem",
    outcome: "An ecosystem you can trust and reuse.",
    href: "/platform/ecosystem",
    span: "regular",
    proofs: [
      "Banana Cake Pop IDE",
      "Strawberry Shake typed clients",
      "Green Donut DataLoaders",
    ],
    art: EcosystemArt,
  },
  {
    id: "release-safety",
    bucket: "evolve",
    title: "Release Safety",
    outcome: "Change contracts with a safety net.",
    href: "/platform/release-safety",
    span: "wide",
    proofs: [
      "Schema diff against published clients",
      "Breaking change flagged before merge",
      "Block, warn, or allow per rule",
    ],
    art: ReleaseSafetyArt,
  },
  {
    id: "continuous-integration",
    bucket: "evolve",
    title: "Continuous Integration",
    outcome: "Innovate with confidence at merge time.",
    href: "/platform/continuous-integration",
    span: "regular",
    proofs: [
      "Schema check on every pull request",
      "Composition validation across services",
      "Annotated diffs in code review",
    ],
    art: CiArt,
  },
];

/* -------------------------------------------------------------------------- */
/*  Inline tile art                                                           */
/*  Each piece is a small, distinct SVG so the bento reads as a map, not as   */
/*  a wall of identical cards. Single accent color, currentColor for ink.    */
/* -------------------------------------------------------------------------- */

interface ArtFrameProps {
  readonly children: ReactNode;
  readonly stop: string;
}

function ArtFrame({ children, stop }: ArtFrameProps) {
  return (
    <div
      className="border-cc-card-border bg-cc-surface/60 relative h-28 overflow-hidden rounded-lg border"
      style={{ boxShadow: `inset 0 0 0 1px ${stop}14` }}
    >
      {children}
    </div>
  );
}

function BuildArt({ stop }: { readonly stop: string }) {
  return (
    <ArtFrame stop={stop}>
      <svg
        viewBox="0 0 320 112"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="9"
        >
          <text x="14" y="22" fill="currentColor" opacity="0.55">
            [QueryType]
          </text>
          <text x="14" y="36" fill="currentColor">
            public static class
            <tspan fill={stop}> ProductQueries</tspan>
          </text>
          <text x="14" y="50" fill="currentColor" opacity="0.7">
            {"{"} public static Product
            <tspan fill={stop}> GetProduct</tspan>(int id) {"}"}
          </text>
        </g>
        <g stroke="currentColor" strokeOpacity="0.35" strokeLinecap="round">
          <path d="M14 70 H306" />
        </g>
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="9"
        >
          <text x="14" y="88" fill={stop}>
            schema.graphql
          </text>
          <text x="14" y="102" fill="currentColor" opacity="0.75">
            type Query {"{"} product(id: Int!): Product {"}"}
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

function AgenticArt({ stop }: { readonly stop: string }) {
  return (
    <ArtFrame stop={stop}>
      <svg
        viewBox="0 0 320 112"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="9"
        >
          <text x="14" y="22" fill="currentColor" opacity="0.55">
            agent &gt; propose change
          </text>
          <text x="14" y="40" fill={stop}>
            + field price: Money!
          </text>
          <text x="14" y="54" fill="currentColor" opacity="0.7">
            check: 12 published clients
          </text>
          <text x="14" y="72" fill="currentColor" opacity="0.7">
            lint: naming, nullability
          </text>
          <text x="14" y="92" fill={stop}>
            verdict: safe to merge
          </text>
        </g>
        <rect
          x="6"
          y="6"
          width="308"
          height="100"
          rx="6"
          fill="none"
          stroke="currentColor"
          strokeOpacity="0.15"
        />
      </svg>
    </ArtFrame>
  );
}

function ObservabilityArt({ stop }: { readonly stop: string }) {
  return (
    <ArtFrame stop={stop}>
      <svg
        viewBox="0 0 320 112"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g stroke="currentColor" strokeOpacity="0.2">
          <path d="M14 92 H306" />
          <path d="M14 70 H306" />
          <path d="M14 48 H306" />
          <path d="M14 26 H306" />
        </g>
        <g strokeLinecap="round" strokeWidth="2" fill="none">
          <path
            d="M14 78 L60 62 L96 70 L140 40 L186 56 L224 30 L262 44 L306 22"
            stroke={stop}
          />
          <path
            d="M14 92 L60 88 L96 84 L140 78 L186 80 L224 70 L262 72 L306 64"
            stroke="currentColor"
            strokeOpacity="0.45"
          />
        </g>
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="8"
          fill="currentColor"
          opacity="0.55"
        >
          <text x="14" y="16">
            p95 latency
          </text>
          <text x="240" y="16">
            errors
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

function WorkflowsArt({ stop }: { readonly stop: string }) {
  return (
    <ArtFrame stop={stop}>
      <svg
        viewBox="0 0 320 112"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          stroke="currentColor"
          strokeOpacity="0.35"
          strokeLinecap="round"
          fill="none"
        >
          <path d="M30 56 H100" />
          <path d="M130 56 H200" />
          <path d="M230 56 H290" />
        </g>
        <g fill={stop}>
          <circle cx="30" cy="56" r="6" />
          <circle cx="115" cy="56" r="6" opacity="0.7" />
          <circle cx="215" cy="56" r="6" opacity="0.5" />
          <circle cx="290" cy="56" r="6" opacity="0.35" />
        </g>
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="8"
          fill="currentColor"
          opacity="0.6"
        >
          <text x="14" y="86">
            charge
          </text>
          <text x="98" y="86">
            notify
          </text>
          <text x="198" y="86">
            fulfill
          </text>
          <text x="270" y="86">
            settle
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

function AnalyticsArt({ stop }: { readonly stop: string }) {
  const bars = [38, 64, 22, 78, 50, 88, 30, 58, 70, 44];
  return (
    <ArtFrame stop={stop}>
      <svg
        viewBox="0 0 320 112"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        {bars.map((h, i) => (
          <rect
            key={i}
            x={20 + i * 28}
            y={96 - h}
            width="16"
            height={h}
            rx="2"
            fill={i === 5 ? stop : "currentColor"}
            opacity={i === 5 ? 1 : 0.4}
          />
        ))}
        <path d="M14 100 H306" stroke="currentColor" strokeOpacity="0.3" />
      </svg>
    </ArtFrame>
  );
}

function EcosystemArt({ stop }: { readonly stop: string }) {
  return (
    <ArtFrame stop={stop}>
      <svg
        viewBox="0 0 320 112"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          stroke="currentColor"
          strokeOpacity="0.3"
          fill="none"
          strokeLinecap="round"
        >
          <path d="M160 56 L70 30" />
          <path d="M160 56 L70 82" />
          <path d="M160 56 L250 30" />
          <path d="M160 56 L250 82" />
        </g>
        <circle cx="160" cy="56" r="14" fill={stop} opacity="0.85" />
        <g fill="currentColor" opacity="0.7">
          <circle cx="70" cy="30" r="8" />
          <circle cx="70" cy="82" r="8" />
          <circle cx="250" cy="30" r="8" />
          <circle cx="250" cy="82" r="8" />
        </g>
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="8"
          fill="currentColor"
          opacity="0.65"
        >
          <text x="36" y="18">
            IDE
          </text>
          <text x="34" y="104">
            client
          </text>
          <text x="232" y="18">
            loader
          </text>
          <text x="226" y="104">
            server
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

function ReleaseSafetyArt({ stop }: { readonly stop: string }) {
  return (
    <ArtFrame stop={stop}>
      <svg
        viewBox="0 0 320 112"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="9"
        >
          <text x="14" y="24" fill="currentColor" opacity="0.65">
            schema.diff
          </text>
          <text x="14" y="44" fill={stop}>
            + field discount: Money
          </text>
          <text x="14" y="60" fill="currentColor" opacity="0.85">
            ~ rename total {">"} subtotal
          </text>
          <text x="14" y="76" fill="#f0786a">
            - field legacyId: String
          </text>
          <text x="14" y="96" fill="currentColor" opacity="0.7">
            7 clients affected, 0 blocking
          </text>
        </g>
        <rect
          x="6"
          y="6"
          width="308"
          height="100"
          rx="6"
          fill="none"
          stroke="currentColor"
          strokeOpacity="0.15"
        />
      </svg>
    </ArtFrame>
  );
}

function CiArt({ stop }: { readonly stop: string }) {
  return (
    <ArtFrame stop={stop}>
      <svg
        viewBox="0 0 320 112"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          stroke="currentColor"
          strokeOpacity="0.35"
          strokeLinecap="round"
          fill="none"
        >
          <path d="M40 56 H100" />
          <path d="M130 56 H190" />
          <path d="M220 56 H280" />
        </g>
        <g>
          <rect x="14" y="42" width="28" height="28" rx="6" fill={stop} />
          <rect
            x="102"
            y="42"
            width="28"
            height="28"
            rx="6"
            fill="currentColor"
            opacity="0.6"
          />
          <rect
            x="192"
            y="42"
            width="28"
            height="28"
            rx="6"
            fill="currentColor"
            opacity="0.45"
          />
          <rect
            x="282"
            y="42"
            width="28"
            height="28"
            rx="6"
            fill="currentColor"
            opacity="0.3"
          />
        </g>
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="8"
          fill="currentColor"
          opacity="0.6"
        >
          <text x="14" y="92">
            push
          </text>
          <text x="100" y="92">
            check
          </text>
          <text x="188" y="92">
            compose
          </text>
          <text x="282" y="92">
            ship
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

/* -------------------------------------------------------------------------- */
/*  Chrome primitives                                                         */
/* -------------------------------------------------------------------------- */

interface EyebrowProps {
  readonly children: ReactNode;
}

function Eyebrow({ children }: EyebrowProps) {
  return (
    <p className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
      {children}
    </p>
  );
}

interface BucketHeaderProps {
  readonly bucket: BucketKey;
  readonly index: number;
}

function BucketHeader({ bucket, index }: BucketHeaderProps) {
  const theme = BUCKETS[bucket];
  return (
    <div className="flex items-end justify-between gap-6">
      <div>
        <Eyebrow>
          Bucket {String(index).padStart(2, "0")} · {theme.label}
        </Eyebrow>
        <h2 className="font-heading text-h3 text-cc-heading mt-2 font-semibold tracking-tight">
          {theme.intent}
        </h2>
      </div>
      <span
        className="hidden h-px w-32 shrink-0 md:block"
        style={{
          background: `linear-gradient(90deg, ${theme.stop}, transparent)`,
        }}
        aria-hidden
      />
    </div>
  );
}

/* -------------------------------------------------------------------------- */
/*  Tile                                                                      */
/* -------------------------------------------------------------------------- */

interface TileCardProps {
  readonly tile: Tile;
}

const SPAN_CLASSES: Record<Tile["span"], string> = {
  wide: "md:col-span-2",
  tall: "md:row-span-2",
  regular: "",
};

function TileCard({ tile }: TileCardProps) {
  const theme = BUCKETS[tile.bucket];
  const Art = tile.art;
  return (
    <Link
      href={tile.href}
      className={`group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex flex-col gap-4 rounded-xl border p-5 no-underline backdrop-blur-sm transition-colors ${SPAN_CLASSES[tile.span]}`}
    >
      <span
        className="absolute inset-x-5 top-0 h-px opacity-60"
        style={{
          background: `linear-gradient(90deg, transparent, ${theme.stop}, transparent)`,
        }}
        aria-hidden
      />
      <div className="flex items-center justify-between">
        <Eyebrow>{theme.label}</Eyebrow>
        <span
          className="font-mono text-[0.6rem] tracking-tight"
          style={{ color: theme.stop }}
        >
          {tile.href.split("/").pop()}
        </span>
      </div>
      <h3 className="font-heading text-h5 text-cc-heading group-hover:text-cc-accent font-semibold tracking-tight transition-colors">
        {tile.title}
      </h3>
      <p className="text-cc-ink text-[0.95rem] leading-relaxed">
        {tile.outcome}
      </p>
      <Art stop={theme.stop} />
      <ul className="mt-1 flex flex-col gap-1.5">
        {tile.proofs.map((proof) => (
          <li
            key={proof}
            className="text-cc-ink-dim flex items-start gap-2 text-[0.82rem] leading-snug"
          >
            <span
              className="mt-1 flex h-3 w-3 shrink-0 items-center justify-center"
              style={{ color: theme.stop }}
            >
              <CheckIcon size={12} />
            </span>
            <span>{proof}</span>
          </li>
        ))}
      </ul>
      <span className="text-cc-accent mt-auto text-[0.82rem] font-medium">
        Open {tile.title} →
      </span>
    </Link>
  );
}

/* -------------------------------------------------------------------------- */
/*  Bucket section                                                            */
/* -------------------------------------------------------------------------- */

interface BucketSectionProps {
  readonly bucket: BucketKey;
  readonly index: number;
}

function BucketSection({ bucket, index }: BucketSectionProps) {
  const tiles = TILES.filter((tile) => tile.bucket === bucket);
  return (
    <section className="flex flex-col gap-7">
      <BucketHeader bucket={bucket} index={index} />
      <div className="grid auto-rows-fr gap-5 md:grid-cols-3">
        {tiles.map((tile) => (
          <TileCard key={tile.id} tile={tile} />
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                      */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <header className="flex flex-col gap-7">
      <Eyebrow>The ChilliCream Platform</Eyebrow>
      <h1 className="font-heading text-hero text-cc-heading max-w-4xl font-semibold tracking-tight">
        Eight capabilities,{" "}
        <span
          className="bg-clip-text text-transparent"
          style={{ backgroundImage: SPECTRUM }}
        >
          one platform
        </span>{" "}
        for every API.
      </h1>
      <p className="text-cc-ink lead max-w-2xl">
        The ChilliCream platform covers the full life of a GraphQL API, from the
        first resolver to the next breaking change. Pick a capability to dive
        in, or read the map below to see how the pieces fit.
      </p>
      <div className="flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
      <dl className="border-cc-card-border mt-2 grid grid-cols-3 gap-px overflow-hidden rounded-xl border bg-[color:var(--color-cc-card-border)]">
        {(Object.keys(BUCKETS) as BucketKey[]).map((key) => {
          const theme = BUCKETS[key];
          const count = TILES.filter((t) => t.bucket === key).length;
          return (
            <div key={key} className="bg-cc-card-bg flex flex-col gap-1 p-4">
              <dt className="flex items-center gap-2">
                <span
                  className="h-2 w-2 rounded-full"
                  style={{ backgroundColor: theme.stop }}
                  aria-hidden
                />
                <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
                  {theme.label}
                </span>
              </dt>
              <dd className="text-cc-heading font-heading text-h4 font-semibold tabular-nums">
                {count}
                <span className="text-cc-ink-dim font-body ml-2 text-[0.85rem] font-normal">
                  {count === 1 ? "capability" : "capabilities"}
                </span>
              </dd>
              <p className="text-cc-ink-dim text-[0.78rem] leading-snug">
                {theme.intent}
              </p>
            </div>
          );
        })}
      </dl>
    </header>
  );
}

/* -------------------------------------------------------------------------- */
/*  Nitro callout                                                             */
/*  No embed for this variant. The bento is the centerpiece, so Nitro is      */
/*  positioned as the control plane that ties everything together.            */
/* -------------------------------------------------------------------------- */

function NitroCallout() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-8 md:p-10">
      <span
        className="pointer-events-none absolute -top-24 -right-24 h-64 w-64 rounded-full opacity-30 blur-3xl"
        style={{ background: SPECTRUM }}
        aria-hidden
      />
      <div className="relative flex flex-col gap-6 md:flex-row md:items-center md:justify-between md:gap-10">
        <div className="flex flex-col gap-3 md:max-w-xl">
          <Eyebrow>And there is Nitro</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading font-semibold tracking-tight">
            The control plane that powers the platform.
          </h2>
          <p className="text-cc-ink leading-relaxed">
            Nitro is the hosted surface where the eight capabilities meet:
            schema registry, release checks, analytics, and traces all share one
            home. Connect a service, ship a change, and Nitro keeps the rest of
            the platform in sync.
          </p>
          <ul className="text-cc-ink-dim mt-2 flex flex-col gap-1.5">
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
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href="https://nitro.chillicream.com">
            Open Nitro
          </SolidButton>
          <OutlineButton href="/products/nitro">About Nitro</OutlineButton>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA                                                               */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="flex flex-col items-center gap-6 py-6 text-center">
      <Eyebrow>Pick a capability</Eyebrow>
      <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
        Start with the surface closest to today&apos;s problem.
      </h2>
      <p className="text-cc-ink-dim max-w-2xl text-[0.95rem] leading-relaxed">
        Every tile above is a real page. Open the one that maps to the work in
        front of you, or start a project and let the platform fold in as you
        need it.
      </p>
      <div className="flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function PlatformCapabilityMapPage() {
  return (
    <div className="flex flex-col gap-20 py-6">
      <Hero />
      <BucketSection bucket="build" index={1} />
      <BucketSection bucket="run" index={2} />
      <BucketSection bucket="evolve" index={3} />
      <NitroCallout />
      <ClosingCta />
    </div>
  );
}
