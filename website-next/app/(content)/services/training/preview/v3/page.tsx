import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

const META_DESCRIPTION =
  "Book GraphQL training for your team. Hot Chocolate, ASP.NET Core, React, and Relay curriculum that flexes for beginner, mixed, and advanced engineering teams.";

export const metadata: Metadata = {
  title: "GraphQL Training for Your Team | ChilliCream",
  description: META_DESCRIPTION,
  keywords: [
    "GraphQL training",
    "Hot Chocolate training",
    "corporate GraphQL workshop",
    "team GraphQL training",
    "Relay training",
    "ASP.NET Core GraphQL training",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    type: "website",
    siteName: "ChilliCream",
    title: "GraphQL Training for Your Team | ChilliCream",
    description: META_DESCRIPTION,
  },
};

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function TrainingPreviewV3Page() {
  return (
    <>
      <FriendlyHero />
      <LevelsSection />
      <OffersSection />
      <OutcomesSection />
      <DeliveryFormatsSection />
      <FunBand />
      <FaqSection />
      <ClosingCta />
    </>
  );
}

// ---------------------------------------------------------------------------
// Hero
// ---------------------------------------------------------------------------

function FriendlyHero() {
  return (
    <section className="pt-16 pb-12 sm:pt-24 sm:pb-16">
      <div className="text-cc-nav-label mb-4 text-center font-mono text-xs font-semibold tracking-widest uppercase">
        ChilliCream Training
      </div>
      <h1 className="font-heading text-cc-heading text-hero mx-auto max-w-3xl text-center leading-tight font-semibold tracking-tight">
        Beginner team. Advanced team. Mixed team. Don&apos;t panic.
      </h1>
      <p className="lead text-cc-ink-dim mx-auto mt-6 max-w-2xl text-center">
        Our GraphQL curriculum is designed to teach in depth and works really
        well. It also isn&apos;t set in stone, so we shape every engagement to
        the team in the room.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="mailto:contact@chillicream.com?subject=Training">
          Talk to a trainer
        </SolidButton>
        <OutlineButton href="#levels">Where is your team today?</OutlineButton>
      </div>
      <p className="text-cc-nav-label mt-5 text-center font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Beginner, mixed, or advanced. Same trainers, different starting line.
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// "Where is your team today?" tri-column
// ---------------------------------------------------------------------------

interface LevelCard {
  readonly tag: string;
  readonly title: string;
  readonly hint: string;
  readonly intro: string;
  readonly covers: readonly string[];
  readonly Icon: () => React.ReactElement;
  readonly accent: "cyan" | "violet" | "coral";
}

const LEVELS: readonly LevelCard[] = [
  {
    tag: "Level 1",
    title: "Beginner team",
    hint: "Heard of GraphQL. Maybe shipped a toy server.",
    intro:
      "We start from REST instincts and rebuild them. By the end of week one your team can read a schema, write resolvers with confidence, and stop confusing fields with arguments.",
    covers: [
      "Schema-first thinking and the type system",
      "Queries, mutations, variables, and fragments",
      "Hot Chocolate basics on ASP.NET Core",
      "Wiring up a real Relay or Apollo client",
      "Pagination, errors, and the everyday traps",
    ],
    Icon: SeedIcon,
    accent: "cyan",
  },
  {
    tag: "Level 2",
    title: "Mixed team",
    hint: "Half the team has shipped. Half the team is bluffing.",
    intro:
      "The most common shape we see. We split sessions into shared foundations plus parallel tracks, so nobody is bored and nobody is lost. Everyone leaves on the same page.",
    covers: [
      "Shared foundations to align vocabulary",
      "Parallel tracks for newcomers and veterans",
      "Pair exercises that mix the two groups",
      "A real schema review on your codebase",
      "Working sessions on the bugs you brought with you",
    ],
    Icon: MixIcon,
    accent: "violet",
  },
  {
    tag: "Level 3",
    title: "Advanced team",
    hint: "Schemas in production. Now the corners get sharp.",
    intro:
      "For teams already shipping GraphQL who want to go deeper. We focus on the parts that hurt at scale: schema design, performance, federation with Fusion, and operating Hot Chocolate in anger.",
    covers: [
      "Schema design at scale and review patterns",
      "Data loaders, batching, and query plans",
      "Federation with Hot Chocolate Fusion",
      "Observability and Nitro in production",
      "Versioning and breaking-change workflows",
    ],
    Icon: SummitIcon,
    accent: "coral",
  },
];

function LevelsSection() {
  return (
    <section id="levels" className="py-16 sm:py-20">
      <div className="mb-10 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Where is your team today?
        </div>
        <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
          Pick the row that sounds like your standup.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base">
          The curriculum is the same set of building blocks. The order, the
          depth, and the exercises change for the room.
        </p>
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        {LEVELS.map((level) => (
          <LevelCardView key={level.title} level={level} />
        ))}
      </div>
    </section>
  );
}

function LevelCardView({ level }: { readonly level: LevelCard }) {
  const { Icon } = level;
  const accentText =
    level.accent === "cyan"
      ? "text-cc-accent"
      : level.accent === "violet"
        ? "text-[#7c92c6]"
        : "text-[#f0786a]";
  return (
    <article className="border-cc-card-border bg-cc-card-bg/60 flex h-full flex-col gap-5 rounded-3xl border p-6 sm:p-7">
      <header className="flex items-start justify-between gap-4">
        <span
          className={`font-mono text-xs font-semibold tracking-[0.2em] ${accentText}`}
        >
          {level.tag}
        </span>
        <span className={`${accentText}`}>
          <Icon />
        </span>
      </header>
      <div className="flex flex-col gap-2">
        <h3 className="font-heading text-cc-heading text-h4 font-semibold">
          {level.title}
        </h3>
        <p className="text-cc-ink-dim text-sm leading-relaxed">{level.hint}</p>
      </div>
      <p className="text-cc-ink text-sm leading-relaxed">{level.intro}</p>
      <div className="text-cc-nav-label mt-1 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        What we cover
      </div>
      <ul className="flex flex-1 flex-col gap-2">
        {level.covers.map((item) => (
          <li key={item} className="text-cc-ink flex items-start gap-2 text-sm">
            <span className={`mt-1 flex-none ${accentText}`}>
              <CheckIcon />
            </span>
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

// ---------------------------------------------------------------------------
// The two real offers as delivery options
// ---------------------------------------------------------------------------

interface CorporateOffer {
  readonly kind: string;
  readonly tagline: string;
  readonly description: string;
  readonly perks: readonly string[];
  readonly ctaLabel: string;
  readonly ctaHref: string;
  readonly Icon: () => React.ReactElement;
  readonly highlight?: boolean;
}

const OFFERS: readonly CorporateOffer[] = [
  {
    kind: "Corporate Training",
    tagline: "Flexible curriculum, shaped to your team",
    description:
      "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
    perks: [
      "Level up their proficiency",
      "Catered to different skills",
      "Overcome challenges they have been wrestling with",
      "Get everybody on the same technical page",
    ],
    ctaLabel: "Book Corporate Training",
    ctaHref: "mailto:contact@chillicream.com?subject=Corporate%20Training",
    Icon: TeamIcon,
  },
  {
    kind: "Corporate Workshop",
    tagline: "Hands on, with a real project at the end",
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
    ctaLabel: "Book Corporate Workshop",
    ctaHref: "mailto:contact@chillicream.com?subject=Corporate%20Workshop",
    Icon: WorkshopIcon,
    highlight: true,
  },
];

function OffersSection() {
  return (
    <section id="offers" className="py-16 sm:py-20">
      <div className="mb-10 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Two ways to run it
        </div>
        <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
          Training to align, or a workshop to ship.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base">
          Both engagements use the same curriculum and the same trainers. They
          differ in how much hands-on project work sits at the end of the week.
        </p>
      </div>
      <div className="grid gap-4 md:grid-cols-2">
        {OFFERS.map((offer) => (
          <OfferCard key={offer.kind} offer={offer} />
        ))}
      </div>
    </section>
  );
}

function OfferCard({ offer }: { readonly offer: CorporateOffer }) {
  const { Icon } = offer;
  const Cta = offer.highlight ? SolidButton : OutlineButton;
  return (
    <article
      className={`relative flex h-full flex-col gap-5 rounded-3xl border p-6 sm:p-7 ${
        offer.highlight
          ? "border-cc-accent bg-cc-card-bg"
          : "border-cc-card-border bg-cc-card-bg/60"
      }`}
    >
      {offer.highlight && (
        <span className="bg-cc-accent text-cc-surface absolute -top-3 left-6 rounded-full px-3 py-1 font-mono text-[0.6rem] font-semibold tracking-[0.18em] uppercase">
          Most popular
        </span>
      )}
      <header className="flex items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h3 className="font-heading text-cc-heading text-h4 font-semibold">
            {offer.kind}
          </h3>
          <p className="text-cc-ink-dim text-sm leading-relaxed">
            {offer.tagline}
          </p>
        </div>
        <span className="text-cc-accent">
          <Icon />
        </span>
      </header>
      <p className="text-cc-ink text-sm leading-relaxed">{offer.description}</p>
      <div className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        What is in the box
      </div>
      <ul className="flex flex-1 flex-col gap-2">
        {offer.perks.map((perk) => (
          <li key={perk} className="text-cc-ink flex items-start gap-2 text-sm">
            <span className="text-cc-accent mt-1 flex-none">
              <CheckIcon />
            </span>
            <span>{perk}</span>
          </li>
        ))}
      </ul>
      <Cta href={offer.ctaHref} className="w-full">
        {offer.ctaLabel}
      </Cta>
    </article>
  );
}

// ---------------------------------------------------------------------------
// Outcomes: what your team will know after
// ---------------------------------------------------------------------------

interface Outcome {
  readonly title: string;
  readonly copy: string;
  readonly Icon: () => React.ReactElement;
}

const OUTCOMES: readonly Outcome[] = [
  {
    title: "Read a schema like a map",
    copy: "Your team can navigate a large GraphQL schema, recognise the common shapes, and explain why a type is modelled the way it is.",
    Icon: MapIcon,
  },
  {
    title: "Write resolvers without surprises",
    copy: "From simple fields to data loaders and pagination, with the patterns that scale instead of the snippets that bite later.",
    Icon: WrenchIcon,
  },
  {
    title: "Plan a client they can live with",
    copy: "Fragments, variables, error handling, and a Relay or Apollo setup that the next person on the team can actually maintain.",
    Icon: PlugIcon,
  },
  {
    title: "Diagnose the slow query",
    copy: "Open a trace, read the plan, find the N+1, and know which knobs to turn in Hot Chocolate before reaching for hacks.",
    Icon: GraphIcon,
  },
  {
    title: "Have an opinion on federation",
    copy: "When to split a schema, when not to, and how Hot Chocolate Fusion fits with the platform they already run.",
    Icon: BranchIcon,
  },
  {
    title: "Speak the same language",
    copy: "Backend, frontend, and platform engineers leave with one shared vocabulary, so the next design review is faster and friendlier.",
    Icon: ChatIcon,
  },
];

function OutcomesSection() {
  return (
    <section className="py-16 sm:py-20">
      <div className="mb-10 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          By the end of the week
        </div>
        <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
          What your team will actually know.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base">
          No certificate-printer outcomes. These are the things we expect every
          team to walk away able to do, regardless of where they started.
        </p>
      </div>
      <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {OUTCOMES.map((outcome) => (
          <OutcomeCard key={outcome.title} outcome={outcome} />
        ))}
      </ul>
    </section>
  );
}

function OutcomeCard({ outcome }: { readonly outcome: Outcome }) {
  const { Icon } = outcome;
  return (
    <li className="border-cc-card-border bg-cc-card-bg/60 flex flex-col gap-3 rounded-2xl border p-5">
      <span className="border-cc-accent/40 bg-cc-accent/10 text-cc-accent inline-flex h-9 w-9 items-center justify-center rounded-full border">
        <Icon />
      </span>
      <h3 className="font-heading text-cc-heading text-base font-semibold">
        {outcome.title}
      </h3>
      <p className="text-cc-ink text-sm leading-relaxed">{outcome.copy}</p>
    </li>
  );
}

// ---------------------------------------------------------------------------
// Delivery formats
// ---------------------------------------------------------------------------

interface DeliveryFormat {
  readonly name: string;
  readonly subtitle: string;
  readonly description: string;
  readonly bestFor: string;
  readonly Icon: () => React.ReactElement;
}

const FORMATS: readonly DeliveryFormat[] = [
  {
    name: "On site",
    subtitle: "We come to you",
    description:
      "A trainer joins your team in a room with a whiteboard and proper coffee. Best when you want the focused energy of being out of inboxes for a week.",
    bestFor: "Best for a single co-located team that can clear the calendar.",
    Icon: OnsiteIcon,
  },
  {
    name: "Remote",
    subtitle: "Live, distributed",
    description:
      "Live sessions over your call tool of choice, with shared notebooks, breakout rooms, and homework between days so timezones do not become a wall.",
    bestFor:
      "Best for distributed teams or when travel does not make business sense.",
    Icon: RemoteIcon,
  },
  {
    name: "Hybrid",
    subtitle: "Some in the room, some on the call",
    description:
      "Deliberate breakout design and exercises that work for the people in the room and the people on the call, with a good A/V setup so nobody is half-present.",
    bestFor: "Best when part of the team can fly in and part cannot.",
    Icon: HybridIcon,
  },
];

function DeliveryFormatsSection() {
  return (
    <section className="py-16 sm:py-20">
      <div className="mb-10 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Delivery format
        </div>
        <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
          On site, remote, or a sensible hybrid.
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-2xl text-base">
          We have run training in all three formats. Pick the one that fits your
          calendar and your office, not the other way around.
        </p>
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        {FORMATS.map((format) => (
          <FormatCard key={format.name} format={format} />
        ))}
      </div>
    </section>
  );
}

function FormatCard({ format }: { readonly format: DeliveryFormat }) {
  const { Icon } = format;
  return (
    <article className="border-cc-card-border bg-cc-card-bg/60 flex h-full flex-col gap-4 rounded-3xl border p-6">
      <div className="flex items-center gap-3">
        <span className="border-cc-card-border bg-cc-surface/60 text-cc-accent inline-flex h-10 w-10 items-center justify-center rounded-full border">
          <Icon />
        </span>
        <div className="flex flex-col">
          <h3 className="font-heading text-cc-heading text-lg font-semibold">
            {format.name}
          </h3>
          <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            {format.subtitle}
          </span>
        </div>
      </div>
      <p className="text-cc-ink text-sm leading-relaxed">
        {format.description}
      </p>
      <p className="text-cc-ink-dim mt-auto text-xs leading-relaxed italic">
        {format.bestFor}
      </p>
    </article>
  );
}

// ---------------------------------------------------------------------------
// "Have lots of fun" honesty band
// ---------------------------------------------------------------------------

function FunBand() {
  return (
    <section className="py-16 sm:py-20">
      <div className="border-cc-accent/40 bg-cc-card-bg/70 relative overflow-hidden rounded-3xl border p-8 sm:p-12">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute inset-0"
          style={{
            background:
              "radial-gradient(60% 80% at 100% 0%, rgba(94,234,212,0.12), transparent 60%), radial-gradient(50% 70% at 0% 100%, rgba(240,120,106,0.10), transparent 60%)",
          }}
        />
        <div className="relative grid gap-8 lg:grid-cols-[1.3fr_1fr] lg:items-center">
          <div>
            <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
              The warm version
            </div>
            <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
              And yes, have lots of fun.
            </h2>
            <p className="text-cc-ink mt-4 max-w-xl text-base leading-relaxed">
              Training that nobody enjoys does not stick. We run sessions like
              the workshops we wish we had been to: hands on, slightly informal,
              no slide marathons, room for questions that start with &ldquo;this
              is probably stupid but...&rdquo; (it is not).
            </p>
            <ul className="text-cc-ink mt-6 grid gap-2 text-sm sm:grid-cols-2">
              {[
                "Plenty of breaks, by design",
                "Pair and group exercises",
                "Real schemas, not lorem ipsum",
                "Questions welcome, including the basic ones",
                "Working sessions on your real codebase",
                "Optional recap doc after the week",
              ].map((item) => (
                <li key={item} className="flex items-start gap-2">
                  <span className="text-cc-accent mt-1 flex-none">
                    <CheckIcon />
                  </span>
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </div>
          <div className="border-cc-card-border bg-cc-surface/60 rounded-2xl border p-6">
            <div className="text-cc-nav-label mb-3 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              What we will not do
            </div>
            <ul className="text-cc-ink flex flex-col gap-3 text-sm leading-relaxed">
              {[
                "No slide marathons",
                "No certificate factory",
                "No copy-paste exercises",
                "No graded tests at the end of the week",
                "No vendor pitch dressed up as training",
              ].map((item) => (
                <li key={item} className="flex items-start gap-2">
                  <span className="text-cc-accent mt-1 flex-none">
                    <CheckIcon />
                  </span>
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// FAQ
// ---------------------------------------------------------------------------

interface FaqItem {
  readonly q: string;
  readonly a: string;
}

const FAQS: readonly FaqItem[] = [
  {
    q: "How long does a typical engagement take?",
    a: "Typically a focused few days up to a full week. Short engagements suit a team that already ships GraphQL and wants depth on one topic. Longer engagements suit foundations plus a small project at the end. The exact shape is set per engagement once we know the team.",
  },
  {
    q: "What team size works best?",
    a: "We are most comfortable with a single engineering team in one cohort. Larger groups are usually split into parallel tracks with the same trainer rotating between them, or run as two cohorts back to back. We will recommend the shape that fits once we know the headcount.",
  },
  {
    q: "What should the team know before day one?",
    a: "For the beginner track, working knowledge of one server-side language (typically C# or TypeScript) and any web framework is enough. For the advanced track we expect existing GraphQL exposure, ideally a schema in production. There is no certification gate.",
  },
  {
    q: "How much does it cost?",
    a: "Pricing is on request, because the right answer depends on team size, format (on site, remote, or hybrid), duration, and whether we are bundling a workshop project. Send us a short note and we will come back with a concrete proposal.",
  },
  {
    q: "How far ahead do we need to book?",
    a: "A few weeks of lead time is typical. We sometimes have shorter slots, and we will tell you honestly if your dates are tight. For on-site engagements travel logistics tend to be the long pole, not curriculum prep.",
  },
  {
    q: "Can the curriculum cover our actual codebase?",
    a: "Yes, and we encourage it. We can review a schema you share ahead of time, design exercises around shapes from your domain, and dedicate part of the week to bugs or design questions your team is wrestling with right now.",
  },
];

function FaqSection() {
  return (
    <section className="py-16 sm:py-20">
      <div className="mb-10 text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
          Common questions
        </div>
        <h2 className="font-heading text-cc-heading text-h2 font-semibold tracking-tight">
          Before you book.
        </h2>
      </div>
      <div className="mx-auto max-w-3xl">
        <ul className="border-cc-card-border bg-cc-card-bg/60 divide-cc-card-border divide-y rounded-3xl border">
          {FAQS.map((item) => (
            <li key={item.q} className="p-6">
              <h3 className="text-cc-heading text-base font-semibold">
                {item.q}
              </h3>
              <p className="text-cc-ink mt-2 text-sm leading-relaxed">
                {item.a}
              </p>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Closing CTA band
// ---------------------------------------------------------------------------

function ClosingCta() {
  return (
    <section className="py-20 text-center sm:py-24">
      <div className="text-cc-nav-label mb-3 font-mono text-xs font-semibold tracking-widest uppercase">
        Ready when you are
      </div>
      <h2 className="font-heading text-cc-heading text-h2 mx-auto max-w-2xl font-semibold tracking-tight">
        Tell us about your team and we will shape the week around it.
      </h2>
      <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base">
        Send a short note with the rough team size, current GraphQL level, and a
        couple of dates that work. We will reply with a concrete proposal, not a
        form to fill in.
      </p>
      <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href="mailto:contact@chillicream.com?subject=Training">
          Email a trainer
        </SolidButton>
        <OutlineButton href="#offers">See the two offers again</OutlineButton>
      </div>
      <p className="text-cc-ink-dim mt-5 font-mono text-xs">
        contact@chillicream.com
      </p>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Inline SVG icons (decorative, currentColor)
// ---------------------------------------------------------------------------

function SeedIcon() {
  return (
    <svg
      width="32"
      height="32"
      viewBox="0 0 32 32"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M16 26V14"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
      />
      <path
        d="M16 14c0-4 3-7 7-7-1 4-3 7-7 7Z"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinejoin="round"
      />
      <path
        d="M16 18c0-3-2-5-5-5 0 3 2 5 5 5Z"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinejoin="round"
      />
      <path
        d="M9 26h14"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
        opacity="0.6"
      />
    </svg>
  );
}

function MixIcon() {
  return (
    <svg
      width="32"
      height="32"
      viewBox="0 0 32 32"
      fill="none"
      aria-hidden="true"
    >
      <circle
        cx="12"
        cy="16"
        r="7"
        stroke="currentColor"
        strokeWidth="1.6"
        opacity="0.8"
      />
      <circle
        cx="20"
        cy="16"
        r="7"
        stroke="currentColor"
        strokeWidth="1.6"
        opacity="0.8"
      />
    </svg>
  );
}

function SummitIcon() {
  return (
    <svg
      width="32"
      height="32"
      viewBox="0 0 32 32"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M4 26 L12 12 L18 20 L22 14 L28 26 Z"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinejoin="round"
        fill="none"
      />
      <path
        d="M10 16l2-2 2 2"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
        opacity="0.6"
      />
    </svg>
  );
}

function TeamIcon() {
  return (
    <svg
      width="28"
      height="28"
      viewBox="0 0 32 32"
      fill="none"
      aria-hidden="true"
    >
      <circle cx="11" cy="13" r="3.5" stroke="currentColor" strokeWidth="1.6" />
      <circle cx="21" cy="13" r="3.5" stroke="currentColor" strokeWidth="1.6" />
      <path
        d="M5 25c1-3 3.5-5 6-5s5 2 6 5"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
      />
      <path
        d="M15 25c1-3 3.5-5 6-5s5 2 6 5"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
      />
    </svg>
  );
}

function WorkshopIcon() {
  return (
    <svg
      width="28"
      height="28"
      viewBox="0 0 32 32"
      fill="none"
      aria-hidden="true"
    >
      <rect
        x="5"
        y="8"
        width="22"
        height="16"
        rx="2"
        stroke="currentColor"
        strokeWidth="1.6"
      />
      <path
        d="M5 13h22"
        stroke="currentColor"
        strokeWidth="1.6"
        opacity="0.6"
      />
      <path
        d="M11 18l2 2 4-4"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path
        d="M19 19h5"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
        opacity="0.6"
      />
    </svg>
  );
}

function MapIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 18 18"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M2 4l4-1 6 2 4-1v10l-4 1-6-2-4 1z"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinejoin="round"
      />
      <path
        d="M6 3v12M12 5v12"
        stroke="currentColor"
        strokeWidth="1.4"
        opacity="0.6"
      />
    </svg>
  );
}

function WrenchIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 18 18"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M13 3a3.5 3.5 0 0 0-3 5l-6 6 2 2 6-6a3.5 3.5 0 0 0 5-3l-2 2-2-2 2-2A3.5 3.5 0 0 0 13 3Z"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function PlugIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 18 18"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M6 2v3M12 2v3"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
      <rect
        x="4"
        y="5"
        width="10"
        height="6"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1.4"
      />
      <path
        d="M9 11v3"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
      <path
        d="M7 14h4"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
    </svg>
  );
}

function GraphIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 18 18"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M2 15h14"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
      <path
        d="M3 12l3-4 3 2 5-6"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function BranchIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 18 18"
      fill="none"
      aria-hidden="true"
    >
      <circle cx="5" cy="4" r="1.6" stroke="currentColor" strokeWidth="1.4" />
      <circle cx="5" cy="14" r="1.6" stroke="currentColor" strokeWidth="1.4" />
      <circle cx="13" cy="6" r="1.6" stroke="currentColor" strokeWidth="1.4" />
      <path
        d="M5 5.5v7"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
      <path
        d="M5 9c0-2 2-3 4-3h2.5"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinecap="round"
      />
    </svg>
  );
}

function ChatIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 18 18"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M2 4h10v7H5l-3 3z"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinejoin="round"
      />
      <path
        d="M6 8h10v5l-2-2"
        stroke="currentColor"
        strokeWidth="1.4"
        strokeLinejoin="round"
        opacity="0.6"
      />
    </svg>
  );
}

function OnsiteIcon() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 20 20"
      fill="none"
      aria-hidden="true"
    >
      <path
        d="M3 17V9l7-5 7 5v8"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinejoin="round"
      />
      <path
        d="M8 17v-5h4v5"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinejoin="round"
      />
    </svg>
  );
}

function RemoteIcon() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 20 20"
      fill="none"
      aria-hidden="true"
    >
      <rect
        x="3"
        y="4"
        width="14"
        height="10"
        rx="1.5"
        stroke="currentColor"
        strokeWidth="1.6"
      />
      <path
        d="M7 17h6"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
      />
      <path
        d="M10 14v3"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinecap="round"
      />
    </svg>
  );
}

function HybridIcon() {
  return (
    <svg
      width="20"
      height="20"
      viewBox="0 0 20 20"
      fill="none"
      aria-hidden="true"
    >
      <circle cx="7" cy="10" r="3" stroke="currentColor" strokeWidth="1.6" />
      <circle
        cx="13"
        cy="10"
        r="3"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeDasharray="2 1.5"
      />
    </svg>
  );
}
