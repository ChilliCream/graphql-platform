"use client";

import { motion, useReducedMotion } from "motion/react";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

// The page's single accent is coral. `cc-accent` resolves to teal, so coral is
// applied via this hex (matching the brand coral value) on inline styles and
// arbitrary Tailwind values.
const CORAL = "#f0786a";
const TRAINING_MAILTO = "mailto:contact@chillicream.com?subject=Training";

// ---------------------------------------------------------------------------
// Data (facts reused verbatim from the v1 ground-truth page)
// ---------------------------------------------------------------------------

type TrackLevel = "Foundations" | "Core" | "Advanced" | "Production";
type Vignette = "schema" | "server" | "fusion" | "telemetry" | "relay" | "diff";

interface CurriculumTrack {
  readonly code: string;
  readonly title: string;
  readonly level: TrackLevel;
  readonly summary: string;
  readonly topics: readonly string[];
  readonly vignette: Vignette;
}

const CURRICULUM: readonly CurriculumTrack[] = [
  {
    code: "GQL-101",
    title: "GraphQL Fundamentals",
    level: "Foundations",
    summary:
      "The mental model: types, fields, resolvers, and how a query maps onto your data.",
    topics: [
      "Schema, types, and the request lifecycle",
      "Queries, mutations, subscriptions",
      "Fragments, variables, directives",
      "Error shapes, nullability, and pagination patterns",
    ],
    vignette: "schema",
  },
  {
    code: "HC-201",
    title: "Hot Chocolate Server",
    level: "Core",
    summary:
      "Build a production GraphQL server on ASP.NET Core with Hot Chocolate, end to end.",
    topics: [
      "Code-first types, resolvers, and DI",
      "Data loaders, projections, and EF Core integration",
      "Authorization, filtering, sorting, paging",
      "Testing, instrumentation, and configuration",
    ],
    vignette: "server",
  },
  {
    code: "FUS-301",
    title: "Fusion Federation",
    level: "Advanced",
    summary:
      "Compose multiple Hot Chocolate services into one Fusion graph without leaking the seams.",
    topics: [
      "Subgraph design and ownership boundaries",
      "Composition, source schemas, and lookup keys",
      "Cross-subgraph entities and shared types",
      "Gateway configuration and rollout strategy",
    ],
    vignette: "fusion",
  },
  {
    code: "NIT-401",
    title: "Production Observability with Nitro",
    level: "Production",
    summary:
      "Wire Hot Chocolate and Fusion into Nitro to see what production is actually doing.",
    topics: [
      "Schema registry and published client tracking",
      "Operation telemetry and slow-resolver triage",
      "Persisted operations and safe schema evolution",
      "Reading traces when an incident is live",
    ],
    vignette: "telemetry",
  },
  {
    code: "REL-301",
    title: "React + Relay Client",
    level: "Advanced",
    summary:
      "Drive a React UI with Relay so data flows match component boundaries by default.",
    topics: [
      "Fragments, connections, and colocation",
      "Mutations, optimistic updates, and store consistency",
      "Suspense, streaming, and refetch patterns",
      "Working with persisted queries from Nitro",
    ],
    vignette: "relay",
  },
  {
    code: "DSN-301",
    title: "Schema Design & Evolution",
    level: "Advanced",
    summary:
      "Design a schema your team can change safely once real clients depend on it.",
    topics: [
      "Naming, nullability, and shaping for change",
      "Errors as data, mutations, and result unions",
      "Versionless evolution and deprecation",
      "Reviewing diffs against published clients",
    ],
    vignette: "diff",
  },
];

interface DeliveryFormat {
  readonly slate: string;
  readonly name: string;
  readonly tagline: string;
  readonly notes: readonly string[];
}

const FORMATS: readonly DeliveryFormat[] = [
  {
    slate: "On-set",
    name: "On-site",
    tagline: "We come to you.",
    notes: [
      "Instructor on location with your team",
      "Best for hands-on labs and whiteboard design",
      "Travel quoted with the engagement",
    ],
  },
  {
    slate: "Remote dial-in",
    name: "Remote",
    tagline: "Live, distributed cohorts.",
    notes: [
      "Live sessions across time zones",
      "Shared repo, recordings, and Q&A channel",
      "Easiest to schedule across multiple offices",
    ],
  },
  {
    slate: "Hybrid splice",
    name: "Hybrid",
    tagline: "Half in the room, half on Zoom.",
    notes: [
      "Anchor cohort in one location, others dial in",
      "Workshops can splice on-site labs with remote review",
      "Useful when seniors are co-located and juniors are not",
    ],
  },
];

interface Offer {
  readonly cut: string;
  readonly name: "Corporate Training" | "Corporate Workshop";
  readonly tagline: string;
  readonly description: string;
  readonly perks: readonly string[];
  readonly highlight?: boolean;
}

const OFFERS: readonly Offer[] = [
  {
    cut: "Director's cut",
    name: "Corporate Training",
    tagline: "Flexible curriculum, shaped to your team.",
    description:
      "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
    perks: [
      "Level up their proficiency",
      "Catered to different skills",
      "Overcome challenges they have been wrestling with",
      "Get everybody on the same technical page",
    ],
  },
  {
    cut: "Short film",
    name: "Corporate Workshop",
    tagline: "Focused, hands-on, project-shaped.",
    description:
      "We will look at how to build a GraphQL server with ASP.NET Core 7 and Hot Chocolate. You will learn how to explore and manage large schemas. Further, we will dive into React and explore how to efficiently build fast and fluent web interfaces using Relay.",
    perks: [
      "Core concepts and advanced",
      "Deepen knowledge of GraphQL API",
      "Work on a real project",
      "Scale and production quirks",
      "Level up your entire team at once",
      "Have Lots of Fun!",
    ],
    highlight: true,
  },
];

interface CrewFact {
  readonly title: string;
  readonly body: string;
}

const CREW: readonly CrewFact[] = [
  {
    title: "Product maintainers",
    body: "Trainers ship on Hot Chocolate, Fusion, and Nitro.",
  },
  {
    title: "Real engagements",
    body: "Years of paid work shaping production GraphQL on .NET.",
  },
  {
    title: "Public speakers",
    body: "Regulars at GraphQL Conf and .NET community events.",
  },
  {
    title: "Honest answers",
    body: "We will tell you what the product does and what it does not.",
  },
];

interface FaqItem {
  readonly q: string;
  readonly a: string;
}

const FAQ: readonly FaqItem[] = [
  {
    q: "How long is a typical engagement?",
    a: "Most workshops run two to five days. A Corporate Training that spans several tracks is usually split into multiple weeks so people keep shipping in between. We size duration to the topics you pick and the seniority of the room, then put it in writing before we start.",
  },
  {
    q: "How many people can attend?",
    a: "We have run sessions from a single team of five up to a few dozen engineers across offices. For workshops with live labs we keep cohorts small enough that everyone gets feedback. For larger groups we either split into cohorts or lean on the lecture-and-clinic format.",
  },
  {
    q: "What do attendees need to know beforehand?",
    a: "For the Hot Chocolate and Fusion tracks, comfort with C# and ASP.NET Core. For the Relay track, comfort with React and TypeScript. No prior GraphQL is required for the foundations track. We ask a few questions before we start and adjust the depth of each module to the room.",
  },
  {
    q: "How is pricing handled?",
    a: "Pricing is on request because the right number depends on tracks, duration, headcount, format, and travel. Tell us what you want covered and we send back a written quote.",
  },
  {
    q: "How soon can we book?",
    a: "Lead time is typically a few weeks so we can tailor the curriculum and line up the instructor. Urgent engagements are possible when we have a slot open. Get in touch early if your delivery date is fixed.",
  },
  {
    q: "Do you sign NDAs and work on our code?",
    a: "Yes. We routinely sign NDAs and tailor workshop projects against your own schema or service. If you would rather keep the workshop on a neutral codebase we bring one.",
  },
];

function shotId(code: string) {
  return `shot-${code.toLowerCase()}`;
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export function ClientPage() {
  return (
    <>
      <CatalogHero />
      <FilmstripRail />
      <ShotList />
      <TwoCuts />
      <DeliveryFormat />
      <Crew />
      <CallSheetFaq />
      <BookTheShoot />
    </>
  );
}

// ---------------------------------------------------------------------------
// Hero
// ---------------------------------------------------------------------------

function CatalogHero() {
  return (
    <section className="py-20 sm:py-24">
      <div className="grid gap-10 lg:grid-cols-[1.3fr_1fr] lg:items-end">
        <div>
          <div className="text-cc-nav-label mb-5 font-mono text-xs font-semibold tracking-widest uppercase">
            GraphQL training workshops
          </div>
          <h1 className="font-heading text-cc-heading text-5xl leading-tight font-semibold tracking-tight sm:text-6xl lg:text-7xl">
            Six tracks, one{" "}
            <span
              className="bg-clip-text text-transparent"
              style={{
                backgroundImage:
                  "linear-gradient(90deg, #16b9e4, #7c92c6, #f0786a)",
              }}
            >
              reel
            </span>
            .
          </h1>
          <p className="text-cc-prose mt-6 max-w-2xl text-base sm:text-lg">
            A reel of GraphQL training built around Hot Chocolate, Fusion,
            Nitro, and Relay. Scrub through the frames, pick the tracks your
            team needs, then run them as a tailored Corporate Training or as a
            focused Corporate Workshop on a real project.
          </p>
          <div className="mt-8 flex flex-col gap-3 sm:flex-row sm:gap-4">
            <SolidButton href="#reel">Browse the reel</SolidButton>
            <OutlineButton href={TRAINING_MAILTO}>Talk to us</OutlineButton>
          </div>
        </div>
        <CallSheetCard />
      </div>
    </section>
  );
}

function CallSheetCard() {
  return (
    <aside
      aria-label="Call sheet"
      className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-6 sm:p-7"
    >
      <div
        className="mb-4 font-mono text-[11px] font-semibold tracking-widest uppercase"
        style={{ color: CORAL }}
      >
        Call sheet
      </div>
      <dl className="divide-cc-card-border divide-y">
        <CallSheetRow label="Tracks" value="6" />
        <CallSheetRow label="Levels" value="Foundations to Production" />
        <CallSheetRow label="Formats" value="On-site, Remote, Hybrid" />
        <CallSheetRow label="Cadence" value="Workshop or multi-week" />
        <CallSheetRow label="Pricing" value="On request" />
      </dl>
    </aside>
  );
}

function CallSheetRow({
  label,
  value,
}: {
  readonly label: string;
  readonly value: string;
}) {
  return (
    <div className="flex items-baseline justify-between gap-4 py-3 first:pt-0 last:pb-0">
      <dt className="text-cc-ink-dim font-mono text-xs tracking-wider uppercase">
        {label}
      </dt>
      <dd className="text-cc-heading text-right text-sm font-medium">
        {value}
      </dd>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Filmstrip rail
// ---------------------------------------------------------------------------

function FilmstripRail() {
  return (
    <section
      id="reel"
      aria-labelledby="reel-heading"
      className="py-12 sm:py-16"
    >
      <div className="mb-8 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div className="max-w-2xl">
          <div
            className="mb-3 font-mono text-xs font-semibold tracking-widest uppercase"
            style={{ color: CORAL }}
          >
            The reel
          </div>
          <h2
            id="reel-heading"
            className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl"
          >
            One frame per track. Scrub the strip.
          </h2>
          <p className="text-cc-prose mt-3 text-base sm:text-lg">
            Each frame is a chapter. Drag the strip sideways, then tap a frame
            to jump to its shot below.
          </p>
        </div>
        <FrameCounter />
      </div>

      <div className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-2xl border">
        <FilmGrain />
        <SprocketStrip />
        <ol
          className="relative flex snap-x snap-mandatory gap-4 overflow-x-auto px-4 py-6 sm:px-6"
          style={{ scrollbarWidth: "thin" }}
        >
          {CURRICULUM.map((track, i) => (
            <FilmFrame key={track.code} track={track} index={i} />
          ))}
        </ol>
        <SprocketStrip />
      </div>
    </section>
  );
}

function FrameCounter() {
  const reduceMotion = useReducedMotion();
  const total = CURRICULUM.length;

  if (reduceMotion) {
    return (
      <div className="border-cc-card-border text-cc-ink-dim shrink-0 rounded-full border px-3 py-1.5 font-mono text-[11px] tracking-widest uppercase">
        Reel of {total}
      </div>
    );
  }

  return (
    <div
      aria-hidden
      className="border-cc-card-border text-cc-ink-dim relative shrink-0 overflow-hidden rounded-full border px-3 py-1.5 font-mono text-[11px] tracking-widest uppercase"
    >
      <span className="inline-flex items-center gap-1">
        <span className="relative inline-grid h-[1.1em] w-[1.1em] place-items-center overflow-hidden">
          {Array.from({ length: total }, (_, i) => (
            <motion.span
              key={i}
              className="col-start-1 row-start-1"
              style={{ color: CORAL }}
              initial={{ opacity: 0 }}
              animate={{ opacity: [0, 1, 1, 0] }}
              transition={{
                duration: total * 0.7,
                times: [
                  i / total,
                  (i + 0.05) / total,
                  (i + 0.95) / total,
                  (i + 1) / total,
                ],
                repeat: Infinity,
                ease: "linear",
              }}
            >
              {i + 1}
            </motion.span>
          ))}
        </span>
        <span>of {total}</span>
      </span>
    </div>
  );
}

function SprocketStrip() {
  // Repeating sprocket holes as a thin celluloid band, edge to edge.
  return (
    <div
      aria-hidden
      className="border-cc-card-border bg-cc-bg/60 relative h-7 w-full border-y"
    >
      <svg
        className="h-full w-full"
        preserveAspectRatio="xMinYMid"
        viewBox="0 0 240 28"
        role="presentation"
      >
        <defs>
          <pattern
            id="cc-v9-sprockets"
            x="0"
            y="0"
            width="24"
            height="28"
            patternUnits="userSpaceOnUse"
          >
            <rect
              x="6"
              y="9"
              width="12"
              height="10"
              rx="2"
              fill="none"
              stroke="rgba(245,241,234,0.12)"
              strokeWidth="1.5"
            />
          </pattern>
        </defs>
        <rect width="240" height="28" fill="url(#cc-v9-sprockets)" />
      </svg>
    </div>
  );
}

function FilmGrain() {
  // Faint celluloid film-grain texture masked to the rail area.
  return (
    <svg
      aria-hidden
      className="pointer-events-none absolute inset-0 h-full w-full opacity-[0.04]"
      preserveAspectRatio="none"
    >
      <defs>
        <filter id="cc-v9-grain">
          <feTurbulence
            type="fractalNoise"
            baseFrequency="0.8"
            numOctaves="2"
            stitchTiles="stitch"
          />
        </filter>
      </defs>
      <rect width="100%" height="100%" filter="url(#cc-v9-grain)" />
    </svg>
  );
}

function FilmFrame({
  track,
  index,
}: {
  readonly track: CurriculumTrack;
  readonly index: number;
}) {
  return (
    <li className="snap-start">
      <a
        href={`#${shotId(track.code)}`}
        className="group border-cc-card-border bg-cc-card-bg hover:border-cc-card-border-hover flex h-[360px] w-[280px] flex-col rounded-xl border p-5 no-underline transition-[transform,border-color] duration-200 hover:-translate-y-0.5"
      >
        <header className="border-cc-card-border flex items-baseline justify-between gap-3 border-b pb-3 transition-colors">
          <span
            className="font-mono text-xs font-semibold tracking-widest uppercase transition-colors"
            style={{ color: CORAL }}
          >
            {track.code}
          </span>
          <span className="text-cc-ink-dim font-mono text-[10px] tracking-widest uppercase">
            Frame {index + 1}
          </span>
        </header>
        <div className="mt-4 flex items-center justify-between gap-3">
          <LevelChip level={track.level} />
        </div>
        <div className="border-cc-card-border bg-cc-bg/40 mt-4 grid h-[120px] place-items-center overflow-hidden rounded-lg border">
          <TrackVignette kind={track.vignette} />
        </div>
        <h3 className="font-heading text-cc-heading mt-4 text-lg font-semibold tracking-tight">
          {track.title}
        </h3>
        <p className="text-cc-ink-dim mt-2 line-clamp-2 text-sm leading-relaxed">
          {track.summary}
        </p>
        <span
          className="mt-auto inline-flex items-center gap-1.5 pt-3 font-mono text-[11px] tracking-widest uppercase"
          style={{ color: CORAL }}
        >
          Jump to shot
          <svg
            aria-hidden
            width="14"
            height="14"
            viewBox="0 0 16 16"
            fill="none"
            className="transition-transform group-hover:translate-y-0.5"
          >
            <path
              d="M8 3v10M4 9l4 4 4-4"
              stroke="currentColor"
              strokeWidth="1.5"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </span>
      </a>
    </li>
  );
}

function LevelChip({ level }: { readonly level: TrackLevel }) {
  return (
    <span className="border-cc-card-border text-cc-ink-dim rounded-full border px-2.5 py-0.5 font-mono text-[10px] font-semibold tracking-widest uppercase">
      {level}
    </span>
  );
}

// ---------------------------------------------------------------------------
// Per-track vignettes (inline SVG, decorative)
// ---------------------------------------------------------------------------

function TrackVignette({ kind }: { readonly kind: Vignette }) {
  const stroke = "rgba(245,241,234,0.45)";
  const common = {
    width: 132,
    height: 84,
    viewBox: "0 0 132 84",
    fill: "none",
    "aria-hidden": true,
  } as const;

  switch (kind) {
    case "schema":
      return (
        <svg {...common}>
          <rect
            x="50"
            y="14"
            width="32"
            height="22"
            rx="3"
            stroke={CORAL}
            strokeWidth="1.5"
          />
          <rect
            x="18"
            y="52"
            width="32"
            height="20"
            rx="3"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <rect
            x="82"
            y="52"
            width="32"
            height="20"
            rx="3"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <path
            d="M60 36 L34 52M72 36 L98 52"
            stroke={stroke}
            strokeWidth="1.5"
          />
        </svg>
      );
    case "server":
      return (
        <svg {...common}>
          <rect
            x="30"
            y="12"
            width="72"
            height="16"
            rx="3"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <rect
            x="30"
            y="34"
            width="72"
            height="16"
            rx="3"
            stroke={CORAL}
            strokeWidth="1.5"
          />
          <rect
            x="30"
            y="56"
            width="72"
            height="16"
            rx="3"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <circle cx="40" cy="20" r="2.2" fill={CORAL} />
          <circle cx="40" cy="42" r="2.2" fill={CORAL} />
          <circle cx="40" cy="64" r="2.2" fill={CORAL} />
        </svg>
      );
    case "fusion":
      return (
        <svg {...common}>
          <circle cx="40" cy="42" r="13" stroke={stroke} strokeWidth="1.5" />
          <circle cx="92" cy="42" r="13" stroke={stroke} strokeWidth="1.5" />
          <path
            d="M53 42 H79"
            stroke={CORAL}
            strokeWidth="1.5"
            strokeDasharray="3 3"
          />
          <path
            d="M66 30 V54"
            stroke={CORAL}
            strokeWidth="1.5"
            strokeLinecap="round"
          />
        </svg>
      );
    case "telemetry":
      return (
        <svg {...common}>
          <path
            d="M14 64 L34 48 L48 56 L66 28 L82 44 L98 22 L118 36"
            stroke={CORAL}
            strokeWidth="1.5"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          <path d="M14 72 H118" stroke={stroke} strokeWidth="1.5" />
          <circle cx="66" cy="28" r="2.4" fill={CORAL} />
        </svg>
      );
    case "relay":
      return (
        <svg {...common}>
          <circle cx="26" cy="42" r="5" stroke={CORAL} strokeWidth="1.5" />
          <circle cx="66" cy="22" r="5" stroke={stroke} strokeWidth="1.5" />
          <circle cx="66" cy="62" r="5" stroke={stroke} strokeWidth="1.5" />
          <circle cx="106" cy="42" r="5" stroke={stroke} strokeWidth="1.5" />
          <path
            d="M31 42 L61 24M31 42 L61 60M71 24 L101 42M71 62 L101 44"
            stroke={stroke}
            strokeWidth="1.5"
          />
        </svg>
      );
    case "diff":
      return (
        <svg {...common}>
          <rect
            x="26"
            y="16"
            width="80"
            height="14"
            rx="2"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <rect
            x="26"
            y="36"
            width="80"
            height="14"
            rx="2"
            stroke={CORAL}
            strokeWidth="1.5"
          />
          <rect
            x="26"
            y="56"
            width="80"
            height="14"
            rx="2"
            stroke={stroke}
            strokeWidth="1.5"
          />
          <path d="M32 43 H40M36 39 V47" stroke={CORAL} strokeWidth="1.5" />
        </svg>
      );
  }
}

// ---------------------------------------------------------------------------
// Shot list (expanded tracks, alternating layout)
// ---------------------------------------------------------------------------

function ShotList() {
  return (
    <section
      id="shot-list"
      aria-labelledby="shot-list-heading"
      className="py-12 sm:py-16"
    >
      <div className="mb-10 max-w-2xl">
        <div
          className="mb-3 font-mono text-xs font-semibold tracking-widest uppercase"
          style={{ color: CORAL }}
        >
          Shot list
        </div>
        <h2
          id="shot-list-heading"
          className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl"
        >
          Six tracks, mix and match.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          Each track is modular. A Corporate Training stitches several tracks
          together. A Corporate Workshop usually goes deep on one or two.
        </p>
      </div>

      <div className="divide-cc-card-border border-cc-card-border divide-y border-y">
        {CURRICULUM.map((track, i) => (
          <Shot key={track.code} track={track} flip={i % 2 === 1} />
        ))}
      </div>
    </section>
  );
}

function Shot({
  track,
  flip,
}: {
  readonly track: CurriculumTrack;
  readonly flip: boolean;
}) {
  const reduceMotion = useReducedMotion();
  const panel = (
    <div className="border-cc-card-border bg-cc-card-bg grid h-full min-h-[180px] place-items-center rounded-2xl border p-6">
      <TrackVignette kind={track.vignette} />
    </div>
  );
  const body = (
    <div>
      <div className="flex items-center gap-3">
        <span
          className="font-mono text-xs font-semibold tracking-widest uppercase"
          style={{ color: CORAL }}
        >
          {track.code}
        </span>
        <LevelChip level={track.level} />
      </div>
      <h3 className="font-heading text-cc-heading mt-4 text-2xl font-semibold tracking-tight">
        {track.title}
      </h3>
      <p className="text-cc-prose mt-3 text-base leading-relaxed">
        {track.summary}
      </p>
      <ul className="text-cc-prose mt-5 space-y-2 text-sm">
        {track.topics.map((topic) => (
          <li key={topic} className="flex items-start gap-3">
            <span
              className="mt-[3px] inline-flex shrink-0"
              style={{ color: CORAL }}
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span>{topic}</span>
          </li>
        ))}
      </ul>
    </div>
  );

  return (
    <motion.div
      id={shotId(track.code)}
      className="scroll-mt-24 py-12 first:pt-0 last:pb-0"
      initial={reduceMotion ? false : { opacity: 0, y: 16 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, margin: "-80px" }}
      transition={{ duration: 0.4 }}
    >
      <div className="grid items-center gap-8 lg:grid-cols-2">
        {flip ? (
          <>
            <div className="order-2 lg:order-1">{panel}</div>
            <div className="order-1 lg:order-2">{body}</div>
          </>
        ) : (
          <>
            {body}
            {panel}
          </>
        )}
      </div>
    </motion.div>
  );
}

// ---------------------------------------------------------------------------
// Two cuts (offers)
// ---------------------------------------------------------------------------

function TwoCuts() {
  return (
    <section aria-labelledby="cuts-heading" className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div
          className="mb-3 font-mono text-xs font-semibold tracking-widest uppercase"
          style={{ color: CORAL }}
        >
          Two cuts
        </div>
        <h2
          id="cuts-heading"
          className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl"
        >
          Two ways to run the reel.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          Pick the shape that fits how your team learns. The curriculum carries
          across both.
        </p>
      </div>
      <div className="grid gap-6 lg:grid-cols-2">
        {OFFERS.map((offer) => (
          <OfferCard key={offer.name} offer={offer} />
        ))}
      </div>
    </section>
  );
}

function OfferCard({ offer }: { readonly offer: Offer }) {
  const CtaButton = offer.highlight ? SolidButton : OutlineButton;
  return (
    <article
      className="bg-cc-card-bg relative flex h-full flex-col rounded-2xl border p-7"
      style={
        offer.highlight
          ? {
              borderColor: "rgba(240,120,106,0.6)",
              boxShadow: "0 0 0 1px rgba(240,120,106,0.25)",
            }
          : { borderColor: "var(--color-cc-card-border)" }
      }
    >
      <span
        className="text-cc-surface absolute -top-3 left-6 rounded-full px-3 py-1 font-mono text-[10px] font-semibold tracking-widest uppercase"
        style={{
          backgroundColor: offer.highlight
            ? CORAL
            : "var(--color-cc-card-border-hover)",
        }}
      >
        {offer.cut}
      </span>
      <header>
        <h3 className="font-heading text-cc-heading text-2xl font-semibold tracking-tight">
          {offer.name}
        </h3>
        <p className="text-cc-ink-dim mt-1 text-sm">{offer.tagline}</p>
      </header>
      <p className="text-cc-prose mt-5 text-sm leading-relaxed sm:text-base">
        {offer.description}
      </p>
      <ul className="mt-6 flex-1 space-y-3">
        {offer.perks.map((perk) => (
          <li key={perk} className="flex items-start gap-3 text-sm">
            <span
              className="mt-[3px] inline-flex shrink-0"
              style={{ color: CORAL }}
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span className="text-cc-prose">{perk}</span>
          </li>
        ))}
      </ul>
      <div className="mt-7">
        <CtaButton
          href={`mailto:contact@chillicream.com?subject=${offer.name}`}
          className="w-full"
        >
          Talk to us about {offer.name}
        </CtaButton>
      </div>
    </article>
  );
}

// ---------------------------------------------------------------------------
// Delivery format
// ---------------------------------------------------------------------------

function DeliveryFormat() {
  return (
    <section aria-labelledby="format-heading" className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div
          className="mb-3 font-mono text-xs font-semibold tracking-widest uppercase"
          style={{ color: CORAL }}
        >
          Delivery format
        </div>
        <h2
          id="format-heading"
          className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl"
        >
          On-site, remote, or hybrid.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          The reel is the same. The room is up to you.
        </p>
      </div>
      <div className="grid gap-5 md:grid-cols-3">
        {FORMATS.map((format) => (
          <FormatCard key={format.name} format={format} />
        ))}
      </div>
    </section>
  );
}

function FormatCard({ format }: { readonly format: DeliveryFormat }) {
  return (
    <article className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-2xl border p-6">
      <div
        className="mb-3 font-mono text-[11px] font-semibold tracking-widest uppercase"
        style={{ color: CORAL }}
      >
        {format.slate}
      </div>
      <h3 className="font-heading text-cc-heading text-xl font-semibold tracking-tight">
        {format.name}
      </h3>
      <p className="text-cc-ink-dim mt-1 text-sm">{format.tagline}</p>
      <ul className="text-cc-prose mt-5 space-y-3 text-sm">
        {format.notes.map((note) => (
          <li key={note} className="flex items-start gap-3">
            <span
              className="mt-[3px] inline-flex shrink-0"
              style={{ color: CORAL }}
              aria-hidden
            >
              <CheckIcon />
            </span>
            <span>{note}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

// ---------------------------------------------------------------------------
// Crew
// ---------------------------------------------------------------------------

function Crew() {
  return (
    <section aria-labelledby="crew-heading" className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg relative overflow-hidden rounded-2xl border p-8 sm:p-12">
        <CrewGlow />
        <div className="relative grid gap-8 lg:grid-cols-[1.2fr_1fr] lg:items-center">
          <div>
            <div
              className="mb-3 font-mono text-xs font-semibold tracking-widest uppercase"
              style={{ color: CORAL }}
            >
              Crew
            </div>
            <h2
              id="crew-heading"
              className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl"
            >
              The team behind Hot Chocolate, Fusion, and Nitro.
            </h2>
            <p className="text-cc-prose mt-4 max-w-xl text-base leading-relaxed sm:text-lg">
              Every session is led by ChilliCream engineers who write and
              maintain the products you are training on. When a question slides
              past the slide deck, you get an answer from the people who decided
              how it works.
            </p>
          </div>
          <ul className="grid gap-4 sm:grid-cols-2">
            {CREW.map((fact) => (
              <CrewFactItem key={fact.title} title={fact.title}>
                {fact.body}
              </CrewFactItem>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}

function CrewGlow() {
  return (
    <svg
      className="pointer-events-none absolute -top-24 -right-24 h-[420px] w-[420px] opacity-60"
      viewBox="0 0 400 400"
      aria-hidden
    >
      <defs>
        <radialGradient id="cc-v9-crew-glow" cx="50%" cy="50%" r="50%">
          <stop offset="0%" stopColor={CORAL} stopOpacity="0.35" />
          <stop offset="60%" stopColor={CORAL} stopOpacity="0.05" />
          <stop offset="100%" stopColor={CORAL} stopOpacity="0" />
        </radialGradient>
      </defs>
      <circle cx="200" cy="200" r="200" fill="url(#cc-v9-crew-glow)" />
    </svg>
  );
}

function CrewFactItem({
  title,
  children,
}: {
  readonly title: string;
  readonly children: ReactNode;
}) {
  return (
    <li className="border-cc-card-border rounded-xl border p-4">
      <div className="flex items-start gap-3">
        <span
          className="mt-[3px] inline-flex shrink-0"
          style={{ color: CORAL }}
          aria-hidden
        >
          <CheckIcon size={16} />
        </span>
        <div>
          <div className="text-cc-heading font-medium">{title}</div>
          <p className="text-cc-ink-dim mt-1 text-sm leading-relaxed">
            {children}
          </p>
        </div>
      </div>
    </li>
  );
}

// ---------------------------------------------------------------------------
// Call sheet FAQ
// ---------------------------------------------------------------------------

function CallSheetFaq() {
  return (
    <section aria-labelledby="faq-heading" className="py-16">
      <div className="mx-auto mb-10 max-w-2xl text-center">
        <div
          className="mb-3 font-mono text-xs font-semibold tracking-widest uppercase"
          style={{ color: CORAL }}
        >
          Call sheet
        </div>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-3xl font-semibold tracking-tight sm:text-4xl"
        >
          The questions managers ask first.
        </h2>
        <p className="text-cc-prose mt-3 text-base sm:text-lg">
          Straight answers, no hedging.
        </p>
      </div>
      <div className="border-cc-card-border bg-cc-card-bg divide-cc-card-border divide-y rounded-2xl border">
        {FAQ.map((item) => (
          <details
            key={item.q}
            className="group px-5 py-5 sm:px-6"
            name="training-faq-v9"
          >
            <summary className="flex cursor-pointer list-none items-start justify-between gap-6">
              <span className="text-cc-heading text-base font-medium sm:text-lg">
                {item.q}
              </span>
              <span
                className="mt-1 inline-flex shrink-0 transition-transform group-open:rotate-45"
                style={{ color: CORAL }}
                aria-hidden
              >
                <PlusGlyph />
              </span>
            </summary>
            <p className="text-cc-prose mt-3 pr-10 text-sm leading-relaxed sm:text-base">
              {item.a}
            </p>
          </details>
        ))}
      </div>
    </section>
  );
}

function PlusGlyph() {
  return (
    <svg viewBox="0 0 16 16" width={16} height={16} aria-hidden>
      <path
        d="M8 3v10M3 8h10"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
    </svg>
  );
}

// ---------------------------------------------------------------------------
// Book the shoot
// ---------------------------------------------------------------------------

function BookTheShoot() {
  return (
    <section aria-labelledby="book-heading" className="py-20">
      <div className="border-cc-card-border bg-cc-card-bg rounded-2xl border p-8 text-center sm:p-12">
        <div
          className="mb-3 font-mono text-xs font-semibold tracking-widest uppercase"
          style={{ color: CORAL }}
        >
          Book the shoot
        </div>
        <h2
          id="book-heading"
          className="font-heading text-cc-heading mx-auto max-w-2xl text-3xl font-semibold tracking-tight sm:text-4xl"
        >
          Tell us which tracks, we will quote the rest.
        </h2>
        <p className="text-cc-prose mx-auto mt-4 max-w-xl text-base sm:text-lg">
          Send a short note with the tracks you are interested in, headcount,
          and your preferred format. We come back with a written proposal and a
          date.
        </p>
        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row sm:gap-4">
          <SolidButton href={TRAINING_MAILTO}>Email the team</SolidButton>
          <OutlineButton href="/services/advisory">
            Pair with Advisory
          </OutlineButton>
        </div>
      </div>
    </section>
  );
}
