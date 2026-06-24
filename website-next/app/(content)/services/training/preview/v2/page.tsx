import type { Metadata } from "next";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "GraphQL Training for Teams",
  description:
    "Outcome-led GraphQL training for engineering teams. Your team leaves able to ship a federated GraphQL server in C#, evolve schemas safely, and triage incidents.",
  keywords: [
    "GraphQL training",
    "HotChocolate training",
    "Fusion training",
    "team workshop",
    "corporate workshop",
    "GraphQL federation",
    "ChilliCream training",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    type: "website",
    title: "GraphQL Training for Teams",
    description:
      "What your team can do after training: ship a federated GraphQL server in C#, evolve schemas safely, read traces and triage incidents.",
  },
  twitter: {
    card: "summary_large_image",
    title: "GraphQL Training for Teams",
    description:
      "What your team can do after training: ship a federated GraphQL server in C#, evolve schemas safely, read traces and triage incidents.",
  },
};

// ---------------------------------------------------------------------------
// Data
// ---------------------------------------------------------------------------

interface Outcome {
  readonly index: string;
  readonly title: string;
  readonly summary: string;
  readonly proof: readonly string[];
}

const OUTCOMES: readonly Outcome[] = [
  {
    index: "01",
    title: "Can author a federated GraphQL server in C#",
    summary:
      "Your team can stand up a Hot Chocolate server, model real domains, and compose subgraphs into a Fusion gateway without copy-pasting from samples.",
    proof: [
      "Types, resolvers, and DataLoader patterns",
      "Subgraph boundaries and entity resolution",
      "Fusion composition from a working CI step",
    ],
  },
  {
    index: "02",
    title: "Can ship safe schema changes",
    summary:
      "Your team treats the schema as a contract. They know what breaks published clients, how to deprecate, and how to land a rename without a war room.",
    proof: [
      "Breaking vs additive change rules",
      "Deprecation, replacement, and removal workflow",
      "Schema checks wired into pull requests",
    ],
  },
  {
    index: "03",
    title: "Can read traces and triage incidents",
    summary:
      "Your team can open a trace, follow a slow request through resolvers and downstream calls, and reason about errors without guessing.",
    proof: [
      "Request, resolver, and downstream spans",
      "Error categorisation and partial-data semantics",
      "From a noisy graph to a one-line root cause",
    ],
  },
];

interface Offer {
  readonly kind: string;
  readonly tag: string;
  readonly description: string;
  readonly perks: readonly string[];
  readonly cta: { readonly title: string; readonly link: string };
  readonly featured?: boolean;
}

const OFFERS: readonly Offer[] = [
  {
    kind: "Corporate Training",
    tag: "Curriculum, tuned to your team",
    description:
      "Get your team trained in GraphQL, any of our products, and even React/Relay. Beginner Team? Advanced Team? Or Mixed? Don't panic! Our curriculum is designed to teach in-depth and works really well, but isn't set in stone.",
    perks: [
      "Level up their proficiency",
      "Catered to different skills",
      "Overcome challenges they have been wrestling with",
      "Get everybody on the same technical page",
    ],
    cta: {
      title: "Talk to us about Training",
      link: "mailto:contact@chillicream.com?subject=Corporate%20Training",
    },
  },
  {
    kind: "Corporate Workshop",
    tag: "Hands on, end to end",
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
    cta: {
      title: "Talk to us about a Workshop",
      link: "mailto:contact@chillicream.com?subject=Corporate%20Workshop",
    },
    featured: true,
  },
];

interface Step {
  readonly index: string;
  readonly title: string;
  readonly summary: string;
  readonly bullets: readonly string[];
}

const STEPS: readonly Step[] = [
  {
    index: "01",
    title: "Kickoff call",
    summary:
      "We meet your team lead and a couple of engineers to understand the codebase, the day-to-day pain, and the skill spread in the room.",
    bullets: [
      "What you ship, who consumes it",
      "Where the team is strong, where it stalls",
      "What success looks like by the end of the week",
    ],
  },
  {
    index: "02",
    title: "Custom curriculum",
    summary:
      "We send back a written agenda with topics, exercises, and the real systems we will touch. You review, we adjust. Nothing surprises you on day one.",
    bullets: [
      "Topic list mapped to your outcomes",
      "Exercises based on your domain, not a toy app",
      "Prerequisites your team should brush up on",
    ],
  },
  {
    index: "03",
    title: "On-site or remote week",
    summary:
      "We run the workshop in your office or over video. Mornings cover concepts, afternoons are hands on, with us in the room while your team writes code.",
    bullets: [
      "Concept blocks with code we walk through together",
      "Pair and group exercises against your repo",
      "Open Q&A blocks for things nobody asked yet",
    ],
  },
  {
    index: "04",
    title: "Follow-up",
    summary:
      "A short written recap with what was covered, what is open, and a list of next moves. One follow-up call to unstick anything the team hit afterwards.",
    bullets: [
      "Written recap of topics, exercises, decisions",
      "Open questions captured for your next sprint",
      "Follow-up call within four weeks of the workshop",
    ],
  },
];

const HONEST_DOES: readonly string[] = [
  "Lift the floor across the team in a week, not a quarter.",
  "Replace folk wisdom with patterns we have shipped in production.",
  "Give your senior engineers a shared vocabulary with the rest of the team.",
  "Hand back a written curriculum the team can keep using.",
];

const HONEST_DOES_NOT: readonly string[] = [
  "Turn a junior into a senior in five days.",
  "Refactor your codebase for you while we are on site.",
  "Replace ongoing mentoring, code review, or pairing inside your team.",
  "Cover every framework and language a mixed stack uses.",
];

interface FaqItem {
  readonly q: string;
  readonly a: string;
}

const FAQ: readonly FaqItem[] = [
  {
    q: "How long is a typical engagement?",
    a: "Most workshops run as a focused week, with three to five days of contact time depending on depth and team size. Shorter, two-day deep dives on a single topic (Fusion, schema design, observability) are also common when a team has already done the basics.",
  },
  {
    q: "What team size works best?",
    a: "Six to twelve engineers in the room is the sweet spot. We have run sessions with three, and we have run sessions with twenty plus, but past about fifteen the hands-on time per person drops, so we usually split larger groups across two weeks.",
  },
  {
    q: "What should the team know going in?",
    a: "For the Hot Chocolate and Fusion tracks, working knowledge of C# and ASP.NET Core. For the Relay track, comfort with React and TypeScript. We do not require prior GraphQL experience, mixed-level rooms are normal, and we tune the agenda to the spread.",
  },
  {
    q: "How is pricing handled?",
    a: "Pricing is on request and depends on team size, on-site versus remote, travel, and the custom curriculum work up front. We send a fixed quote after the kickoff call so there are no surprises mid-engagement.",
  },
  {
    q: "On-site, remote, or hybrid?",
    a: "All three work. On-site is the highest-bandwidth format and we recommend it when budget allows. Remote runs over video with shared editors. Hybrid (a few engineers on site, the rest remote) is fine as long as the on-site room has a decent camera and mic.",
  },
  {
    q: "How far out should we book?",
    a: "Four to eight weeks is comfortable, since the custom curriculum needs a real back-and-forth before day one. We sometimes accept shorter lead times, but that usually means a tighter scope.",
  },
];

// ---------------------------------------------------------------------------
// Sections
// ---------------------------------------------------------------------------

function Hero() {
  return (
    <section className="relative pt-16 pb-12 sm:pt-24 sm:pb-16">
      <div className="border-cc-card-border bg-cc-card-bg/40 text-cc-ink-dim mx-auto mb-8 inline-flex w-full max-w-3xl items-center justify-center gap-3 rounded-full border px-4 py-2 font-mono text-xs tracking-widest uppercase">
        <span className="bg-cc-accent inline-block h-1.5 w-1.5 rounded-full" />
        <span>Training for engineering teams</span>
      </div>
      <h1 className="text-cc-heading font-heading mx-auto max-w-4xl text-center text-5xl leading-tight font-bold tracking-tight sm:text-6xl lg:text-7xl">
        Your team, after one week with us:{" "}
        <span className="text-cc-accent">shipping</span>, not{" "}
        <span className="text-cc-accent">guessing</span>.
      </h1>
      <p className="text-cc-prose mx-auto mt-6 max-w-2xl text-center text-base sm:text-lg">
        Outcome-led GraphQL training delivered by the engineers behind Hot
        Chocolate, Fusion, and Nitro. We measure success by what your team can
        do on Monday, not by how many slides we got through.
      </p>
      <div className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row">
        <SolidButton href="mailto:contact@chillicream.com?subject=Training">
          Book a kickoff call
        </SolidButton>
        <OutlineButton href="#outcomes">See the outcomes</OutlineButton>
      </div>
    </section>
  );
}

function OutcomeCard({ outcome }: { outcome: Outcome }) {
  return (
    <article className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover relative flex flex-col rounded-2xl border p-6 transition-colors sm:p-7">
      <div className="flex items-baseline justify-between">
        <span className="text-cc-nav-label font-mono text-xs tracking-widest uppercase">
          Outcome {outcome.index}
        </span>
        <span className="text-cc-accent font-mono text-xs tracking-widest uppercase">
          After training
        </span>
      </div>
      <h3 className="text-cc-heading font-heading mt-4 text-2xl leading-tight font-semibold">
        {outcome.title}
      </h3>
      <p className="text-cc-prose mt-3 text-sm">{outcome.summary}</p>
      <ul className="border-cc-card-border mt-6 space-y-3 border-t pt-5">
        {outcome.proof.map((line) => (
          <li
            key={line}
            className="text-cc-prose flex items-start gap-3 text-sm"
          >
            <span className="text-cc-accent mt-0.5 inline-flex shrink-0 items-center justify-center">
              <CheckIcon size={14} />
            </span>
            <span>{line}</span>
          </li>
        ))}
      </ul>
    </article>
  );
}

function Outcomes() {
  return (
    <section
      id="outcomes"
      aria-labelledby="outcomes-heading"
      className="py-12 sm:py-16"
    >
      <header className="mx-auto mb-10 max-w-3xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
          What your team will know
        </div>
        <h2
          id="outcomes-heading"
          className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
        >
          Three outcomes we hold ourselves to.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base">
          Not topics covered. Not slides delivered. Concrete things your team
          can do back at the keyboard when the week is over.
        </p>
      </header>
      <div className="grid gap-4 md:grid-cols-3">
        {OUTCOMES.map((outcome) => (
          <OutcomeCard key={outcome.index} outcome={outcome} />
        ))}
      </div>
    </section>
  );
}

function OfferCard({ offer }: { offer: Offer }) {
  return (
    <article
      className={[
        "bg-cc-card-bg relative flex flex-col rounded-2xl border p-6 transition-colors sm:p-7",
        offer.featured
          ? "border-cc-accent/60 ring-cc-accent/10 ring-1"
          : "border-cc-card-border hover:border-cc-card-border-hover",
      ].join(" ")}
    >
      <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
        {offer.tag}
      </div>
      <h3 className="text-cc-heading font-heading text-2xl font-semibold">
        {offer.kind}
      </h3>
      <p className="text-cc-prose mt-3 text-sm">{offer.description}</p>
      <ul className="mt-6 grow space-y-3">
        {offer.perks.map((perk) => (
          <li
            key={perk}
            className="text-cc-prose flex items-start gap-3 text-sm"
          >
            <span className="text-cc-accent mt-0.5 inline-flex shrink-0 items-center justify-center">
              <CheckIcon size={14} />
            </span>
            <span>{perk}</span>
          </li>
        ))}
      </ul>
      <div className="mt-8">
        {offer.featured ? (
          <SolidButton href={offer.cta.link} className="w-full">
            {offer.cta.title}
          </SolidButton>
        ) : (
          <OutlineButton href={offer.cta.link} className="w-full">
            {offer.cta.title}
          </OutlineButton>
        )}
      </div>
    </article>
  );
}

function Offers() {
  return (
    <section id="offers" aria-labelledby="offers-heading" className="py-16">
      <header className="mx-auto mb-10 max-w-3xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
          Two ways to engage
        </div>
        <h2
          id="offers-heading"
          className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
        >
          Training that adapts, or a workshop that ships a thing.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base">
          Training reshapes day to day proficiency across the team. A workshop
          drives one focused production-shaped result in a week.
        </p>
      </header>
      <div className="grid gap-5 md:grid-cols-2">
        {OFFERS.map((offer) => (
          <OfferCard key={offer.kind} offer={offer} />
        ))}
      </div>
    </section>
  );
}

function TimelineStep({ step, last }: { step: Step; last: boolean }) {
  return (
    <li className="relative pl-12 sm:pl-16">
      <span
        aria-hidden
        className="border-cc-card-border bg-cc-surface text-cc-accent absolute top-0 left-0 inline-flex h-9 w-9 items-center justify-center rounded-full border font-mono text-xs tracking-widest uppercase sm:h-12 sm:w-12 sm:text-sm"
      >
        {step.index}
      </span>
      {!last && (
        <span
          aria-hidden
          className="bg-cc-card-border absolute top-9 bottom-[-2rem] left-[1.0625rem] w-px sm:top-12 sm:left-[1.4375rem]"
        />
      )}
      <h3 className="text-cc-heading font-heading text-xl leading-tight font-semibold sm:text-2xl">
        {step.title}
      </h3>
      <p className="text-cc-prose mt-2 text-sm sm:text-base">{step.summary}</p>
      <ul className="text-cc-ink-dim mt-4 space-y-2 text-sm">
        {step.bullets.map((b) => (
          <li key={b} className="flex items-start gap-2">
            <span
              aria-hidden
              className="bg-cc-accent/70 mt-2 inline-block h-1 w-1 shrink-0 rounded-full"
            />
            <span>{b}</span>
          </li>
        ))}
      </ul>
    </li>
  );
}

function Timeline() {
  return (
    <section id="how" aria-labelledby="how-heading" className="py-16">
      <header className="mx-auto mb-10 max-w-3xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
          How a workshop runs
        </div>
        <h2
          id="how-heading"
          className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
        >
          Four steps, from first call to the recap.
        </h2>
        <p className="text-cc-ink-dim mt-4 text-base">
          Every workshop follows the same backbone, so you know what each week
          buys you before we book a date.
        </p>
      </header>
      <div className="border-cc-card-border bg-cc-card-bg/40 rounded-2xl border p-8 sm:p-12">
        <ol className="space-y-12 sm:space-y-14">
          {STEPS.map((step, i) => (
            <TimelineStep
              key={step.index}
              step={step}
              last={i === STEPS.length - 1}
            />
          ))}
        </ol>
      </div>
    </section>
  );
}

function HonestyBand() {
  return (
    <section aria-labelledby="honesty-heading" className="py-16">
      <div className="border-cc-card-border bg-cc-surface relative overflow-hidden rounded-2xl border p-8 sm:p-12">
        <div
          aria-hidden
          className="pointer-events-none absolute inset-0 opacity-60"
          style={{
            background:
              "radial-gradient(60% 80% at 85% 50%, rgba(94,234,212,0.10), transparent 70%), radial-gradient(50% 80% at 10% 20%, rgba(124,146,198,0.08), transparent 70%)",
          }}
        />
        <header className="relative mx-auto mb-10 max-w-3xl text-center">
          <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
            Honest about what this is
          </div>
          <h2
            id="honesty-heading"
            className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
          >
            What training does, and what it does not.
          </h2>
          <p className="text-cc-ink-dim mt-4 text-base">
            Training is a force multiplier on a team that already wants to ship.
            It is not a substitute for the work that comes after.
          </p>
        </header>
        <div className="relative grid gap-6 sm:grid-cols-2">
          <div>
            <div className="text-cc-success mb-3 inline-flex items-center gap-2 font-mono text-xs tracking-widest uppercase">
              <span className="bg-cc-success inline-block h-1.5 w-1.5 rounded-full" />
              What training does
            </div>
            <ul className="text-cc-prose space-y-2 text-sm">
              {HONEST_DOES.map((line) => (
                <li key={line} className="flex items-start gap-3">
                  <span className="text-cc-success mt-0.5 inline-flex shrink-0 items-center justify-center">
                    <CheckIcon size={14} />
                  </span>
                  <span>{line}</span>
                </li>
              ))}
            </ul>
          </div>
          <div>
            <div className="text-cc-warning mb-3 inline-flex items-center gap-2 font-mono text-xs tracking-widest uppercase">
              <span className="bg-cc-warning inline-block h-1.5 w-1.5 rounded-full" />
              What training does not
            </div>
            <ul className="text-cc-prose space-y-2 text-sm">
              {HONEST_DOES_NOT.map((line) => (
                <li key={line} className="flex items-start gap-3">
                  <span
                    aria-hidden
                    className="text-cc-warning mt-0.5 inline-flex h-3.5 w-3.5 shrink-0 items-center justify-center"
                  >
                    <svg viewBox="0 0 16 16" width={14} height={14}>
                      <path
                        d="M4 4 L12 12 M12 4 L4 12"
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="2"
                        strokeLinecap="round"
                      />
                    </svg>
                  </span>
                  <span>{line}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </section>
  );
}

function Faq() {
  return (
    <section id="faq" aria-labelledby="faq-heading" className="py-16">
      <header className="mx-auto mb-10 max-w-3xl text-center">
        <div className="text-cc-nav-label mb-3 font-mono text-xs tracking-widest uppercase">
          Before you book
        </div>
        <h2
          id="faq-heading"
          className="text-cc-heading font-heading text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
        >
          The questions managers ask first.
        </h2>
      </header>
      <dl className="border-cc-card-border bg-cc-card-bg/40 divide-cc-card-border divide-y rounded-2xl border">
        {FAQ.map((item) => (
          <div
            key={item.q}
            className="grid gap-4 p-6 sm:grid-cols-[1fr_1.6fr] sm:p-8"
          >
            <dt className="text-cc-heading font-heading text-lg font-semibold">
              {item.q}
            </dt>
            <dd className="text-cc-prose text-sm sm:text-base">{item.a}</dd>
          </div>
        ))}
      </dl>
    </section>
  );
}

function ClosingCta() {
  return (
    <section aria-labelledby="closing-heading" className="py-16">
      <div className="border-cc-card-border bg-cc-card-bg/50 rounded-2xl border p-10 text-center sm:p-14">
        <h2
          id="closing-heading"
          className="text-cc-heading font-heading mx-auto max-w-2xl text-3xl leading-tight font-semibold tracking-tight sm:text-4xl"
        >
          Ready to put a week of training on the calendar?
        </h2>
        <p className="text-cc-ink-dim mx-auto mt-4 max-w-xl text-base">
          Tell us the team size, the rough goal, and the window you have. We
          reply with a kickoff call slot and a draft agenda.
        </p>
        <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
          <SolidButton href="mailto:contact@chillicream.com?subject=Training">
            Email contact@chillicream.com
          </SolidButton>
          <OutlineButton href="#outcomes">Re-read the outcomes</OutlineButton>
        </div>
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Page
// ---------------------------------------------------------------------------

export default function TrainingOutcomesPreviewPage() {
  return (
    <>
      <Hero />
      <Outcomes />
      <Offers />
      <Timeline />
      <HonestyBand />
      <Faq />
      <ClosingCta />
    </>
  );
}
