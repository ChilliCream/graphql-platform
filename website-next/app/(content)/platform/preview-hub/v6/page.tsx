import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

export const metadata: Metadata = {
  title: "The ChilliCream Platform: House Menu",
  description:
    "Today's menu of the ChilliCream GraphQL platform: eight capability pours across Roast, Pour, and Refine, plus the Nitro control plane behind the bar.",
  keywords: [
    "GraphQL platform",
    "ChilliCream platform",
    "GraphQL build pipeline",
    "GraphQL observability",
    "GraphQL release safety",
    "GraphQL workflows",
    "GraphQL analytics",
    "GraphQL ecosystem",
    "Nitro control plane",
  ],
  openGraph: {
    title: "The ChilliCream Platform: House Menu",
    description:
      "Eight platform pours organized by Roast, Pour, and Refine, plus the Nitro control plane that powers schema registry, release checks, and traces.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Brand spectrum: appears AT MOST once on this page (closing CTA wash).     */
/* -------------------------------------------------------------------------- */

const SPECTRUM = "linear-gradient(100deg, #16b9e4, #7c92c6, #f0786a)";

/* -------------------------------------------------------------------------- */
/*  Stage manifest                                                            */
/*  The menu reads as three brew stages. Each stage maps to one of the        */
/*  underlying buckets (Build, Run, Evolve) but uses the barista voice.       */
/* -------------------------------------------------------------------------- */

type StageKey = "roast" | "pour" | "refine";

interface Stage {
  readonly key: StageKey;
  readonly label: string;
  readonly bucket: string;
  readonly intent: string;
}

const STAGES: readonly Stage[] = [
  {
    key: "roast",
    label: "Roast",
    bucket: "Build",
    intent: "Author the API and let agents help.",
  },
  {
    key: "pour",
    label: "Pour",
    bucket: "Run",
    intent: "Operate it in production with eyes on every cup.",
  },
  {
    key: "refine",
    label: "Refine",
    bucket: "Evolve",
    intent: "Ship change without breaking what is already brewed.",
  },
];

/* -------------------------------------------------------------------------- */
/*  Menu items                                                                */
/*  Eight platform surfaces, each rendered as a numbered menu line. Drink     */
/*  icons live in @/src/icons and act as small chrome glyphs.                 */
/* -------------------------------------------------------------------------- */

type IconKey = "drip" | "french" | "pour" | "espresso" | "tray";

interface ArtProps {
  readonly accent: string;
}

interface MenuItem {
  readonly id: string;
  readonly stage: StageKey;
  readonly number: string;
  readonly title: string;
  readonly tastingNote: string;
  readonly icon: IconKey;
  readonly href: string;
  readonly proofs: readonly string[];
  readonly art: (props: ArtProps) => ReactNode;
}

const MENU: readonly MenuItem[] = [
  {
    id: "build",
    stage: "roast",
    number: "01",
    title: "Build",
    tastingNote: "Ship from the code that runs it.",
    icon: "drip",
    href: "/platform/build",
    proofs: [
      "Implementation-first GraphQL in C#",
      "Schema, resolvers, DataLoaders from one class",
      "Typed .NET clients out of the same source",
    ],
    art: BuildArt,
  },
  {
    id: "agentic-coding",
    stage: "roast",
    number: "02",
    title: "Agentic Coding",
    tastingNote: "Give coding agents a feedback loop.",
    icon: "french",
    href: "/platform/agentic-coding",
    proofs: [
      "Typed contracts agents can read",
      "Diff and lint signal on every change",
      "Same loop a senior reviewer would run",
    ],
    art: AgenticArt,
  },
  {
    id: "observability",
    stage: "pour",
    number: "03",
    title: "Observability",
    tastingNote: "See what the API is doing, right now.",
    icon: "espresso",
    href: "/platform/observability",
    proofs: [
      "Operation-level traces and timings",
      "Field hot paths and N+1 detection",
      "OpenTelemetry export to your stack",
    ],
    art: ObservabilityArt,
  },
  {
    id: "workflows",
    stage: "pour",
    number: "04",
    title: "Workflows",
    tastingNote: "Let work continue after the request.",
    icon: "tray",
    href: "/platform/workflows",
    proofs: [
      "Durable steps with retries",
      "Background jobs in the same model",
      "Resumable on cold start",
    ],
    art: WorkflowsArt,
  },
  {
    id: "analytics",
    stage: "pour",
    number: "05",
    title: "Analytics",
    tastingNote: "Know which fields earn their keep.",
    icon: "drip",
    href: "/platform/analytics",
    proofs: [
      "Field-level usage over time",
      "Per-client adoption per type",
      "Spot dead fields before you cut",
    ],
    art: AnalyticsArt,
  },
  {
    id: "ecosystem",
    stage: "pour",
    number: "06",
    title: "Ecosystem",
    tastingNote: "An ecosystem you can trust and reuse.",
    icon: "pour",
    href: "/platform/ecosystem",
    proofs: [
      "Banana Cake Pop IDE",
      "Strawberry Shake typed clients",
      "Green Donut DataLoaders",
    ],
    art: EcosystemArt,
  },
  {
    id: "release-safety",
    stage: "refine",
    number: "07",
    title: "Release Safety",
    tastingNote: "Change contracts with a safety net.",
    icon: "french",
    href: "/platform/release-safety",
    proofs: [
      "Schema diff against published clients",
      "Breaking change flagged before merge",
      "Block, warn, or allow per rule",
    ],
    art: ReleaseSafetyArt,
  },
  {
    id: "continuous-integration",
    stage: "refine",
    number: "08",
    title: "Continuous Integration",
    tastingNote: "Innovate with confidence at merge time.",
    icon: "espresso",
    href: "/platform/continuous-integration",
    proofs: [
      "Schema check on every pull request",
      "Composition validation across services",
      "Annotated diffs in code review",
    ],
    art: CiArt,
  },
];

/* -------------------------------------------------------------------------- */
/*  Drink icon dispatch                                                       */
/* -------------------------------------------------------------------------- */

interface DrinkIconProps {
  readonly icon: IconKey;
  readonly className?: string;
}

function DrinkIcon({ icon, className }: DrinkIconProps) {
  switch (icon) {
    case "drip":
      return <DripBrewer className={className} />;
    case "french":
      return <FrenchPress className={className} />;
    case "pour":
      return <PourOver className={className} />;
    case "espresso":
      return <Espresso className={className} />;
    case "tray":
      return <CoffeeTray className={className} />;
  }
}

/* -------------------------------------------------------------------------- */
/*  Tile art                                                                  */
/*  Same compositions as v1, kept on the cc-accent ink so the menu reads as   */
/*  one consistent surface.                                                   */
/* -------------------------------------------------------------------------- */

interface ArtFrameProps {
  readonly children: ReactNode;
  readonly accent: string;
}

function ArtFrame({ children, accent }: ArtFrameProps) {
  return (
    <div
      className="border-cc-card-border bg-cc-surface/70 relative h-24 overflow-hidden rounded-md border"
      style={{ boxShadow: `inset 0 0 0 1px ${accent}14` }}
    >
      {children}
    </div>
  );
}

function BuildArt({ accent }: ArtProps) {
  return (
    <ArtFrame accent={accent}>
      <svg
        viewBox="0 0 320 96"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="9"
        >
          <text x="14" y="20" fill="currentColor" opacity="0.55">
            [QueryType]
          </text>
          <text x="14" y="34" fill="currentColor">
            public static class
            <tspan fill={accent}> ProductQueries</tspan>
          </text>
          <text x="14" y="48" fill="currentColor" opacity="0.7">
            {"{"} public static Product
            <tspan fill={accent}> GetProduct</tspan>(int id) {"}"}
          </text>
        </g>
        <path d="M14 62 H306" stroke="currentColor" strokeOpacity="0.3" />
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="9"
        >
          <text x="14" y="78" fill={accent}>
            schema.graphql
          </text>
          <text x="14" y="92" fill="currentColor" opacity="0.75">
            type Query {"{"} product(id: Int!): Product {"}"}
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

function AgenticArt({ accent }: ArtProps) {
  return (
    <ArtFrame accent={accent}>
      <svg
        viewBox="0 0 320 96"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="9"
        >
          <text x="14" y="18" fill="currentColor" opacity="0.55">
            agent &gt; propose change
          </text>
          <text x="14" y="34" fill={accent}>
            + field price: Money!
          </text>
          <text x="14" y="48" fill="currentColor" opacity="0.7">
            check: 12 published clients
          </text>
          <text x="14" y="62" fill="currentColor" opacity="0.7">
            lint: naming, nullability
          </text>
          <text x="14" y="82" fill={accent}>
            verdict: safe to merge
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

function ObservabilityArt({ accent }: ArtProps) {
  return (
    <ArtFrame accent={accent}>
      <svg
        viewBox="0 0 320 96"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g stroke="currentColor" strokeOpacity="0.2">
          <path d="M14 80 H306" />
          <path d="M14 60 H306" />
          <path d="M14 40 H306" />
          <path d="M14 20 H306" />
        </g>
        <g strokeLinecap="round" strokeWidth="2" fill="none">
          <path
            d="M14 66 L60 50 L96 56 L140 32 L186 46 L224 24 L262 36 L306 18"
            stroke={accent}
          />
          <path
            d="M14 80 L60 76 L96 72 L140 66 L186 68 L224 58 L262 60 L306 54"
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
          <text x="14" y="12">
            p95 latency
          </text>
          <text x="240" y="12">
            errors
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

function WorkflowsArt({ accent }: ArtProps) {
  return (
    <ArtFrame accent={accent}>
      <svg
        viewBox="0 0 320 96"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          stroke="currentColor"
          strokeOpacity="0.35"
          strokeLinecap="round"
          fill="none"
        >
          <path d="M30 48 H100" />
          <path d="M130 48 H200" />
          <path d="M230 48 H290" />
        </g>
        <g fill={accent}>
          <circle cx="30" cy="48" r="6" />
          <circle cx="115" cy="48" r="6" opacity="0.7" />
          <circle cx="215" cy="48" r="6" opacity="0.5" />
          <circle cx="290" cy="48" r="6" opacity="0.35" />
        </g>
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="8"
          fill="currentColor"
          opacity="0.6"
        >
          <text x="14" y="76">
            charge
          </text>
          <text x="98" y="76">
            notify
          </text>
          <text x="198" y="76">
            fulfill
          </text>
          <text x="270" y="76">
            settle
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

function AnalyticsArt({ accent }: ArtProps) {
  const bars = [38, 60, 22, 72, 50, 80, 30, 58, 66, 44];
  return (
    <ArtFrame accent={accent}>
      <svg
        viewBox="0 0 320 96"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        {bars.map((h, i) => (
          <rect
            key={i}
            x={20 + i * 28}
            y={84 - h}
            width="16"
            height={h}
            rx="2"
            fill={i === 5 ? accent : "currentColor"}
            opacity={i === 5 ? 1 : 0.4}
          />
        ))}
        <path d="M14 88 H306" stroke="currentColor" strokeOpacity="0.3" />
      </svg>
    </ArtFrame>
  );
}

function EcosystemArt({ accent }: ArtProps) {
  return (
    <ArtFrame accent={accent}>
      <svg
        viewBox="0 0 320 96"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          stroke="currentColor"
          strokeOpacity="0.3"
          fill="none"
          strokeLinecap="round"
        >
          <path d="M160 48 L70 24" />
          <path d="M160 48 L70 72" />
          <path d="M160 48 L250 24" />
          <path d="M160 48 L250 72" />
        </g>
        <circle cx="160" cy="48" r="12" fill={accent} opacity="0.85" />
        <g fill="currentColor" opacity="0.7">
          <circle cx="70" cy="24" r="7" />
          <circle cx="70" cy="72" r="7" />
          <circle cx="250" cy="24" r="7" />
          <circle cx="250" cy="72" r="7" />
        </g>
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="8"
          fill="currentColor"
          opacity="0.65"
        >
          <text x="36" y="14">
            IDE
          </text>
          <text x="34" y="92">
            client
          </text>
          <text x="232" y="14">
            loader
          </text>
          <text x="226" y="92">
            server
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

function ReleaseSafetyArt({ accent }: ArtProps) {
  return (
    <ArtFrame accent={accent}>
      <svg
        viewBox="0 0 320 96"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize="9"
        >
          <text x="14" y="20" fill="currentColor" opacity="0.65">
            schema.diff
          </text>
          <text x="14" y="38" fill={accent}>
            + field discount: Money
          </text>
          <text x="14" y="54" fill="currentColor" opacity="0.85">
            ~ rename total {">"} subtotal
          </text>
          <text x="14" y="70" fill="#f0786a">
            - field legacyId: String
          </text>
          <text x="14" y="88" fill="currentColor" opacity="0.7">
            7 published clients affected, 0 blocking
          </text>
        </g>
      </svg>
    </ArtFrame>
  );
}

function CiArt({ accent }: ArtProps) {
  return (
    <ArtFrame accent={accent}>
      <svg
        viewBox="0 0 320 96"
        className="text-cc-ink-dim h-full w-full"
        aria-hidden
      >
        <g
          stroke="currentColor"
          strokeOpacity="0.35"
          strokeLinecap="round"
          fill="none"
        >
          <path d="M40 48 H100" />
          <path d="M130 48 H190" />
          <path d="M220 48 H280" />
        </g>
        <g>
          <rect x="14" y="34" width="28" height="28" rx="6" fill={accent} />
          <rect
            x="102"
            y="34"
            width="28"
            height="28"
            rx="6"
            fill="currentColor"
            opacity="0.6"
          />
          <rect
            x="192"
            y="34"
            width="28"
            height="28"
            rx="6"
            fill="currentColor"
            opacity="0.45"
          />
          <rect
            x="282"
            y="34"
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
          <text x="14" y="82">
            push
          </text>
          <text x="100" y="82">
            check
          </text>
          <text x="188" y="82">
            compose
          </text>
          <text x="282" y="82">
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

function MenuRule() {
  return (
    <div className="flex items-center gap-3" aria-hidden>
      <span className="bg-cc-card-border h-px flex-1" />
      <span className="text-cc-ink-dim font-mono text-[0.6rem] tracking-[0.22em] uppercase">
        ·
      </span>
      <span className="bg-cc-card-border h-px flex-1" />
    </div>
  );
}

function StageIcon({ stageKey }: { readonly stageKey: StageKey }) {
  if (stageKey === "roast") {
    return <DripBrewer className="h-full w-full" />;
  }
  if (stageKey === "pour") {
    return <PourOver className="h-full w-full" />;
  }
  return <FrenchPress className="h-full w-full" />;
}

/* -------------------------------------------------------------------------- */
/*  Hero                                                                      */
/* -------------------------------------------------------------------------- */

function Hero() {
  return (
    <header className="flex flex-col gap-7">
      <Eyebrow>On the menu today</Eyebrow>
      <h1 className="font-heading text-hero text-cc-heading max-w-4xl font-semibold tracking-tight">
        Eight pours,{" "}
        <span className="text-cc-accent">one GraphQL platform</span>.
      </h1>
      <p className="text-cc-ink lead max-w-2xl">
        The ChilliCream platform covers the full life of a GraphQL API, from the
        first resolver to the next breaking change. Today&apos;s board is
        organized by brew stage. Pick a pour, or read the menu below to see how
        the bar runs.
      </p>
      <div className="flex flex-wrap items-center gap-3">
        <SolidButton href="/get-started">Start for Free</SolidButton>
        <OutlineButton href="/docs">Read the Docs</OutlineButton>
      </div>
      <dl className="border-cc-card-border mt-2 grid grid-cols-3 gap-px overflow-hidden rounded-xl border bg-[color:var(--color-cc-card-border)]">
        {STAGES.map((stage) => {
          const count = MENU.filter((m) => m.stage === stage.key).length;
          return (
            <div
              key={stage.key}
              className="bg-cc-card-bg relative flex flex-col gap-1 overflow-hidden p-4"
            >
              <span
                className="text-cc-accent pointer-events-none absolute -right-3 -bottom-3 h-16 w-16 opacity-10"
                aria-hidden
              >
                <StageIcon stageKey={stage.key} />
              </span>
              <dt className="flex items-center gap-2">
                <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
                  {stage.label}
                </span>
              </dt>
              <dd className="text-cc-heading font-heading text-h4 font-semibold tabular-nums">
                {count}
                <span className="text-cc-ink-dim font-body ml-2 text-[0.85rem] font-normal">
                  {count === 1 ? "pour" : "pours"}
                </span>
              </dd>
              <p className="text-cc-ink-dim text-[0.78rem] leading-snug">
                {stage.intent}
              </p>
            </div>
          );
        })}
      </dl>
    </header>
  );
}

/* -------------------------------------------------------------------------- */
/*  Today's Pour: a feature row that anchors the menu voice without           */
/*  inflating the page. Build sits here as the entry capability for most      */
/*  teams.                                                                    */
/* -------------------------------------------------------------------------- */

function TodaysPour() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-6 md:p-8">
      <span
        className="text-cc-accent pointer-events-none absolute -top-6 -right-6 h-40 w-40 opacity-15"
        aria-hidden
      >
        <PourOver className="h-full w-full" />
      </span>
      <div className="relative flex flex-col gap-6 md:flex-row md:items-start md:gap-10">
        <div className="flex flex-col gap-3 md:max-w-xl">
          <Eyebrow>Today&apos;s pour</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading font-semibold tracking-tight">
            House blend: Build.
          </h2>
          <p className="text-cc-ink leading-relaxed">
            Ship from the code that runs the API. Hot Chocolate is
            implementation-first GraphQL in C#: source-generated schema,
            resolvers, and DataLoaders from one class, and typed .NET clients
            out of the same source.
          </p>
          <ul className="text-cc-ink-dim mt-1 flex flex-col gap-1.5">
            {[
              "Implementation-first GraphQL in C#",
              "Schema, resolvers, DataLoaders from one class",
              "Typed .NET clients out of the same source",
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
          <Link
            href="/platform/build"
            className="text-cc-accent mt-2 text-[0.85rem] font-medium no-underline"
          >
            Order this pour →
          </Link>
        </div>
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Menu item card                                                            */
/* -------------------------------------------------------------------------- */

interface MenuCardProps {
  readonly item: MenuItem;
}

function MenuCard({ item }: MenuCardProps) {
  const Art = item.art;
  return (
    <Link
      href={item.href}
      className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover relative flex flex-col gap-4 rounded-xl border p-5 no-underline backdrop-blur-sm transition-colors"
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <span className="text-cc-nav-label font-mono text-[0.6rem] tracking-[0.22em] uppercase">
            No. {item.number} · {item.title}
          </span>
          <h3 className="font-heading text-h5 text-cc-heading group-hover:text-cc-accent mt-1 font-semibold tracking-tight transition-colors">
            {item.title}
          </h3>
        </div>
        <span
          className="text-cc-ink-dim group-hover:text-cc-accent h-9 w-9 shrink-0 transition-colors"
          aria-hidden
        >
          <DrinkIcon icon={item.icon} className="h-full w-full" />
        </span>
      </div>
      <p className="text-cc-ink text-[0.95rem] leading-relaxed italic">
        {item.tastingNote}
      </p>
      <Art accent="#5eead4" />
      <ul className="mt-1 flex flex-col gap-1.5">
        {item.proofs.map((proof) => (
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
      <span className="text-cc-accent mt-auto text-[0.82rem] font-medium">
        Open {item.title} →
      </span>
    </Link>
  );
}

/* -------------------------------------------------------------------------- */
/*  Stage section                                                             */
/* -------------------------------------------------------------------------- */

interface StageSectionProps {
  readonly stage: Stage;
  readonly index: number;
  readonly heading: string;
}

function StageSection({ stage, index, heading }: StageSectionProps) {
  const items = MENU.filter((m) => m.stage === stage.key);
  return (
    <section className="flex flex-col gap-6">
      <div className="flex flex-col gap-3">
        <MenuRule />
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <Eyebrow>
              Stage {String(index).padStart(2, "0")} · {stage.label}
            </Eyebrow>
            <h2 className="font-heading text-h3 text-cc-heading mt-2 font-semibold tracking-tight">
              {heading}
            </h2>
          </div>
          <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            {stage.bucket} bucket · {items.length} on the menu
          </span>
        </div>
      </div>
      <div className="grid auto-rows-fr gap-5 md:grid-cols-2">
        {items.map((item) => (
          <MenuCard key={item.id} item={item} />
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Behind the Bar: Nitro callout                                             */
/* -------------------------------------------------------------------------- */

function SteamLines() {
  return (
    <svg
      viewBox="0 0 160 160"
      className="text-cc-accent pointer-events-none absolute top-4 right-6 h-32 w-32 opacity-25"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.4"
      strokeLinecap="round"
      aria-hidden
    >
      <path d="M40 132 Q 36 100 52 84 Q 68 68 60 44 Q 52 22 70 8" />
      <path
        d="M80 132 Q 76 102 94 86 Q 110 70 100 46 Q 90 24 108 10"
        opacity="0.7"
      />
      <path
        d="M120 132 Q 116 104 132 88 Q 146 72 138 50 Q 130 30 146 16"
        opacity="0.45"
      />
      <rect
        x="32"
        y="132"
        width="100"
        height="14"
        rx="3"
        stroke="currentColor"
        strokeOpacity="0.4"
      />
    </svg>
  );
}

function NitroCallout() {
  return (
    <section className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-xl border p-8 md:p-10">
      <SteamLines />
      <div className="relative flex flex-col gap-6 md:flex-row md:items-center md:justify-between md:gap-10">
        <div className="flex flex-col gap-3 md:max-w-xl">
          <Eyebrow>Behind the bar</Eyebrow>
          <h2 className="font-heading text-h3 text-cc-heading font-semibold tracking-tight">
            Nitro is the espresso machine the menu runs on.
          </h2>
          <p className="text-cc-ink leading-relaxed">
            Nitro is the hosted control plane where the eight pours meet: schema
            registry, release checks, analytics, and traces all share one home.
            Connect a service, ship a change, and Nitro keeps the rest of the
            platform in sync.
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
/*  House Notes                                                               */
/* -------------------------------------------------------------------------- */

function HouseNotes() {
  const notes: readonly { readonly label: string; readonly note: string }[] = [
    {
      label: "Open source roots",
      note: "Hot Chocolate, Strawberry Shake, and Green Donut ship under MIT and run wherever .NET runs.",
    },
    {
      label: ".NET-native",
      note: "Built on top of ASP.NET Core idioms, not a framework bolted on the side.",
    },
    {
      label: "OpenTelemetry-friendly",
      note: "Export traces and metrics to your existing stack. Nitro adds the schema-aware view on top.",
    },
  ];
  return (
    <section className="flex flex-col gap-5">
      <MenuRule />
      <div className="flex items-end justify-between gap-4">
        <Eyebrow>House notes</Eyebrow>
        <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
          Three quick tasting notes
        </span>
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        {notes.map((n) => (
          <div
            key={n.label}
            className="border-cc-card-border bg-cc-card-bg flex flex-col gap-2 rounded-lg border p-4"
          >
            <span className="text-cc-accent font-mono text-[0.6rem] tracking-[0.22em] uppercase">
              {n.label}
            </span>
            <p className="text-cc-ink-dim text-[0.85rem] leading-snug">
              {n.note}
            </p>
          </div>
        ))}
      </div>
    </section>
  );
}

/* -------------------------------------------------------------------------- */
/*  Closing CTA                                                               */
/* -------------------------------------------------------------------------- */

function ClosingCta() {
  return (
    <section className="relative flex flex-col items-center gap-6 overflow-hidden rounded-xl py-10 text-center">
      <span
        className="pointer-events-none absolute -top-24 left-1/2 h-64 w-[480px] -translate-x-1/2 rounded-full opacity-20 blur-3xl"
        style={{ background: SPECTRUM }}
        aria-hidden
      />
      <div className="relative flex flex-col items-center gap-6">
        <Eyebrow>Ready to order?</Eyebrow>
        <h2 className="font-heading text-h2 text-cc-heading max-w-3xl font-semibold tracking-tight">
          Pick the pour closest to today&apos;s problem.
        </h2>
        <p className="text-cc-ink-dim max-w-2xl text-[0.95rem] leading-relaxed">
          Every menu item above is a real page. Open the one that maps to the
          work in front of you, or start a project and let the bar fold in as
          you need it.
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

export default function PlatformHouseMenuPage() {
  return (
    <div className="flex flex-col gap-16 py-6">
      <Hero />
      <TodaysPour />
      <StageSection
        stage={STAGES[0]}
        index={1}
        heading="Roast. Author the API and let agents help."
      />
      <StageSection
        stage={STAGES[1]}
        index={2}
        heading="Pour. Operate it in production with eyes on every cup."
      />
      <StageSection
        stage={STAGES[2]}
        index={3}
        heading="Refine. Ship change without breaking what is already brewed."
      />
      <NitroCallout />
      <HouseNotes />
      <ClosingCta />
    </div>
  );
}
