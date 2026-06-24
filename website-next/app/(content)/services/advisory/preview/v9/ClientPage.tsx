"use client";

import type { ReactNode } from "react";
import { motion, useReducedMotion, type Variants } from "motion/react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONSULTING_MAILTO = "mailto:contact@chillicream.com?subject=Consulting";
const CONTRACTING_MAILTO = "mailto:contact@chillicream.com?subject=Contracting";

const HERO_HEADLINE = "Talk to the engineers who built your GraphQL stack.";

// The transcript voice. Every section eyebrow is a speaker label so the page
// keeps reading like a captured advisory call without re-running any motion.
const BRAND_SPECTRUM =
  "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

interface Tier {
  readonly id: "consulting" | "contracting";
  readonly eyebrow: string;
  readonly name: string;
  readonly tagline: string;
  readonly priceLine: string;
  readonly priceNote: string;
  readonly bestFor: string;
  readonly perks: readonly string[];
  readonly primaryCta: { readonly label: string; readonly href: string };
  readonly secondaryCta: { readonly label: string; readonly href: string };
  readonly highlight?: boolean;
}

const TIERS: readonly Tier[] = [
  {
    id: "consulting",
    eyebrow: "Hourly engagements",
    name: "Consulting",
    tagline:
      "Hourly consulting services to get the help you need at any stage of your project. This is the best way to get started.",
    priceLine: "$300",
    priceNote: "per hour",
    bestFor:
      "Teams that already own the build and need a senior GraphQL engineer on call for design, troubleshooting, and review.",
    perks: [
      "Mentoring and guidance",
      "Architecture",
      "Troubleshooting",
      "Code Review",
      "Best practices education",
    ],
    primaryCta: { label: "Book a 60-min call", href: BOOKING_URL },
    secondaryCta: { label: "Email us", href: CONSULTING_MAILTO },
    highlight: true,
  },
  {
    id: "contracting",
    eyebrow: "Scoped engagements",
    name: "Contracting",
    tagline:
      "Options for teams who do not have the time, bandwidth, and/or expertise to implement their own GraphQL solutions.",
    priceLine: "Custom",
    priceNote: "scope & timeline",
    bestFor:
      "Teams that want our engineers to deliver a working result, from a proof of concept to a production rollout.",
    perks: ["Proof of concept", "Implementation"],
    primaryCta: { label: "Talk to an Expert", href: CONTRACTING_MAILTO },
    secondaryCta: { label: "Book an intro call", href: BOOKING_URL },
  },
];

interface EngagementStep {
  readonly index: string;
  readonly title: string;
  readonly description: string;
  readonly bullets: readonly string[];
}

const ENGAGEMENT_STEPS: readonly EngagementStep[] = [
  {
    index: "01",
    title: "Introductory call",
    description:
      "A 60-minute working call. You walk us through the system, the goal, and the constraints. We ask the hard questions and tell you whether we are the right fit.",
    bullets: [
      "Senior engineer, no sales handoff",
      "Mutual NDA available on request",
      "Written recap with next steps",
    ],
  },
  {
    index: "02",
    title: "Proposal",
    description:
      "A written proposal that matches your need: hourly retainer for consulting, or a scoped statement of work for contracting with deliverables, milestones, and a target timeline.",
    bullets: [
      "Hourly retainer or fixed scope",
      "Clear deliverables and milestones",
      "Written, scoped, and signable",
    ],
  },
  {
    index: "03",
    title: "Kickoff",
    description:
      "Contract signed, channel opened, work starts. You get a direct line to the engineers doing the work, a shared backlog, and a weekly checkpoint.",
    bullets: [
      "Shared Slack or Teams channel",
      "Weekly checkpoint and written status",
      "Direct access to the engineers doing the work",
    ],
  },
];

interface CredentialColumn {
  readonly title: string;
  readonly body: string;
  readonly bullets: readonly string[];
}

const CREDENTIALS: readonly CredentialColumn[] = [
  {
    title: "Who you work with",
    body: "Senior engineers from the core team. The same people who maintain Hot Chocolate, design Fusion, and ship Nitro.",
    bullets: [
      "Direct line to the maintainers",
      "Pull-request authors on the core code",
      "Same team across consulting and contracting",
    ],
  },
  {
    title: "What we work on",
    body: "The full GraphQL stack we build: schema design, federation with Fusion, ASP.NET Core integration, performance and resolver tuning, MCP, and Nitro observability and CI.",
    bullets: [
      "Schema and federation design",
      "Fusion composition and rollout",
      "Nitro observability, CI, and persisted ops",
    ],
  },
  {
    title: "How we work",
    body: "In your repo, in your channels, in your timezone window. Written status every week. Honest answers when something is not the right fit, even when that means a smaller engagement.",
    bullets: [
      "Embedded in your codebase",
      "Written weekly status reports",
      "We will say no when no is the right answer",
    ],
  },
];

interface FaqItem {
  readonly question: string;
  readonly answer: string;
}

const FAQ: readonly FaqItem[] = [
  {
    question: "What is the hourly rate for consulting?",
    answer:
      "Consulting is billed at $300 per hour. We bill in blocks of time agreed up front, on a retainer or a small purchase order, so you never get a surprise invoice. Contracting engagements are scoped separately as a statement of work.",
  },
  {
    question: "How small is too small for an engagement?",
    answer:
      "A single 60-minute call is fine. Many teams start with one or two hours to unblock a specific decision (schema shape, Fusion composition, auth model) and only return when the next question lands. There is no minimum retainer to talk to us.",
  },
  {
    question: "Do you sign an NDA?",
    answer:
      "Yes. We will sign a mutual NDA before the introductory call when you ask, and we are comfortable with most standard agreements. Customer code, schemas, and traces never leave the engagement.",
  },
  {
    question: "How quickly can you start?",
    answer:
      "Consulting hours usually start within the same week, sometimes the same day. Contracting engagements depend on scope and current bandwidth, and we will tell you the realistic start date on the introductory call rather than promise a slot we cannot honor.",
  },
  {
    question: "What outcomes can I expect?",
    answer:
      "Concrete, written deliverables tied to your goal: an architecture decision record, a schema review with line-level comments, a working proof of concept, or a production-ready implementation. We do not bill for slideware.",
  },
  {
    question: "Who actually does the work?",
    answer:
      "The engineers who build Hot Chocolate, Fusion, and Nitro. The same people who write the framework code, review the pull requests, and answer the hard issues on GitHub are the people on your call.",
  },
];

export function ClientPage() {
  return (
    <div className="relative">
      {/* Static vertical hairline at the left rail position (lg+ only). */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-y-0 left-44 hidden w-px lg:block"
        style={{
          background:
            "linear-gradient(to bottom, transparent 0%, var(--color-cc-card-border) 18%, var(--color-cc-card-border) 82%, transparent 100%)",
        }}
      />
      <Hero />
      <OpeningLine />
      <TierGrid />
      <EngagementTranscript />
      <CredentialsTriad />
      <ProductsStrip />
      <Faq />
      <ClosingBand />
    </div>
  );
}

/**
 * A transcript row: a mono speaker label sits in the left rail on lg+, and the
 * content flows in the center column. This keeps the conversational frame
 * consistent across every section without any motion.
 */
function TranscriptRow({
  speaker,
  children,
}: {
  readonly speaker: string;
  readonly children: ReactNode;
}) {
  return (
    <div className="lg:grid lg:grid-cols-[11rem_minmax(0,1fr)] lg:gap-x-8">
      <div className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase lg:pt-1 lg:text-right">
        {speaker}
      </div>
      <div className="mt-2 max-w-3xl lg:mt-0">{children}</div>
    </div>
  );
}

function Hero() {
  const reduceMotion = useReducedMotion();
  const characters = Array.from(HERO_HEADLINE);

  // Stagger the headline glyphs once on mount. No scroll coupling, no loop.
  const headlineVariants: Variants = {
    hidden: {},
    visible: {
      transition: { staggerChildren: 0.028, delayChildren: 0.35 },
    },
  };
  const charVariants: Variants = {
    hidden: { opacity: 0 },
    visible: { opacity: 1, transition: { duration: 0.001 } },
  };

  return (
    <section className="relative isolate pt-10 pb-14 sm:pt-16 sm:pb-20">
      {/* Subtle radial glow behind the headline. Static, decorative. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute top-12 left-1/2 -z-10 h-[640px] w-[640px] -translate-x-1/2 lg:left-[28rem]"
        style={{
          background:
            "radial-gradient(circle, rgba(94, 234, 212, 0.06) 0%, transparent 70%)",
        }}
      />

      <TranscriptRow speaker="Transcript 00:00:01">
        <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 font-semibold tracking-tight">
          {reduceMotion ? (
            HERO_HEADLINE
          ) : (
            <motion.span
              variants={headlineVariants}
              initial="hidden"
              animate="visible"
              aria-label={HERO_HEADLINE}
            >
              {characters.map((char, index) => (
                <motion.span
                  key={`${char}-${index}`}
                  variants={charVariants}
                  aria-hidden="true"
                >
                  {char === " " ? " " : char}
                </motion.span>
              ))}
              <Caret />
            </motion.span>
          )}
        </h1>

        <motion.div
          initial={reduceMotion ? false : { opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: reduceMotion ? 0 : 2.3, duration: 0.5 }}
        >
          <p className="text-cc-ink mt-6 max-w-2xl text-base text-pretty sm:text-lg">
            Hourly consulting at $300 per hour, or full contracting engagements,
            delivered by the team behind Hot Chocolate, Fusion, and Nitro. Bring
            a question, a design, or a deadline. We meet you where the work is.
          </p>
          <p className="text-cc-nav-label mt-4 font-mono text-xs tracking-[0.12em]">
            <span className="text-cc-accent">$300 / hour</span>
            <span className="text-cc-ink-faint mx-2">|</span>
            consulting rate, contracting scoped separately
          </p>
          <div className="mt-9 flex flex-wrap items-center gap-3">
            <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
            <OutlineButton href={CONSULTING_MAILTO}>Email us</OutlineButton>
          </div>
        </motion.div>
      </TranscriptRow>
    </section>
  );
}

/**
 * A blinking caret that rides the last glyph of the typed headline. The blink
 * is a time-driven loop, not coupled to scroll, and only starts after the
 * headline finishes typing.
 */
function Caret() {
  return (
    <motion.span
      aria-hidden="true"
      className="bg-cc-accent ml-1 inline-block h-[0.8em] w-[3px] translate-y-[0.06em] align-baseline"
      initial={{ opacity: 0 }}
      animate={{ opacity: [0, 1, 1, 0, 0] }}
      transition={{
        duration: 1.1,
        repeat: Infinity,
        ease: "linear",
        delay: 2.1,
      }}
    />
  );
}

function OpeningLine() {
  return (
    <section className="pb-14 sm:pb-20">
      <TranscriptRow speaker="Client:">
        <blockquote className="border-cc-accent/40 border-l-2 pl-5">
          <p className="font-heading text-cc-heading text-h5 sm:text-h4 leading-snug font-medium text-pretty">
            &ldquo;We need senior GraphQL eyes on a Fusion rollout. We can build
            it, but we want the people who designed composition in the room
            before we commit the schema.&rdquo;
          </p>
        </blockquote>
      </TranscriptRow>
    </section>
  );
}

function TierGrid() {
  return (
    <section aria-labelledby="tiers-heading" className="pb-16 sm:pb-24">
      <h2 id="tiers-heading" className="sr-only">
        Engagement tiers
      </h2>
      <TranscriptRow speaker="Advisor:">
        <p className="font-heading text-cc-heading text-h4 mb-8 font-semibold">
          Two ways to engage.
        </p>
        <div className="grid gap-6 lg:grid-cols-2 lg:items-stretch">
          {TIERS.map((tier) => (
            <TierCard key={tier.id} tier={tier} />
          ))}
        </div>
      </TranscriptRow>
    </section>
  );
}

function TierCard({ tier }: { readonly tier: Tier }) {
  if (tier.highlight) {
    return (
      <div
        className="relative isolate rounded-3xl p-[1.5px]"
        style={{ background: BRAND_SPECTRUM }}
      >
        <StartHerePill />
        <div className="bg-cc-surface flex h-full flex-col rounded-[calc(1.5rem-1.5px)] p-7 sm:p-9">
          <TierCardBody tier={tier} />
        </div>
      </div>
    );
  }

  return (
    <div className="bg-cc-card-bg border-cc-card-border hover:border-cc-card-border-hover flex h-full flex-col rounded-3xl border p-7 transition-colors sm:p-9">
      <TierCardBody tier={tier} />
    </div>
  );
}

function TierCardBody({ tier }: { readonly tier: Tier }) {
  return (
    <>
      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        {tier.eyebrow}
      </p>
      <h3 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
        {tier.name}
      </h3>
      <p className="text-cc-ink-dim mt-3 text-sm leading-relaxed sm:text-base">
        {tier.tagline}
      </p>

      <div className="mt-6 flex items-baseline gap-2">
        <span className="font-heading text-cc-heading text-h4 font-semibold">
          {tier.priceLine}
        </span>
        <span className="text-cc-nav-label font-mono text-xs">
          {tier.priceNote}
        </span>
      </div>

      <div
        aria-hidden="true"
        className="border-cc-ink-faint my-6 border-t border-dashed"
      />

      <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Best for
      </p>
      <p className="text-cc-ink mt-2 text-sm leading-relaxed">{tier.bestFor}</p>

      <p className="text-cc-nav-label mt-6 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        What is included
      </p>
      <ul className="mt-3 flex flex-1 flex-col gap-3">
        {tier.perks.map((perk) => (
          <li key={perk} className="flex items-start gap-3">
            <span className="text-cc-accent mt-[5px] flex-none">
              <CheckIcon />
            </span>
            <span className="text-cc-ink text-sm">{perk}</span>
          </li>
        ))}
      </ul>

      <div className="mt-8 flex flex-col gap-3 sm:flex-row">
        <SolidButton href={tier.primaryCta.href} className="w-full sm:flex-1">
          {tier.primaryCta.label}
        </SolidButton>
        <OutlineButton
          href={tier.secondaryCta.href}
          className="w-full sm:flex-1"
        >
          {tier.secondaryCta.label}
        </OutlineButton>
      </div>
    </>
  );
}

function StartHerePill() {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-9 z-10 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      Start here
    </span>
  );
}

function EngagementTranscript() {
  return (
    <section aria-labelledby="engagement-heading" className="pb-16 sm:pb-24">
      <h2 id="engagement-heading" className="sr-only">
        How an engagement starts
      </h2>
      <ol className="flex flex-col gap-12">
        {ENGAGEMENT_STEPS.map((step) => (
          <li key={step.index}>
            <TranscriptRow speaker={`Step ${step.index}`}>
              <h3 className="font-heading text-cc-heading text-h5 font-semibold">
                {step.title}
              </h3>
              <p className="text-cc-ink mt-3 text-sm leading-relaxed sm:text-base">
                {step.description}
              </p>
              <ul className="mt-5 flex flex-col gap-2">
                {step.bullets.map((bullet) => (
                  <li
                    key={bullet}
                    className="text-cc-ink-dim flex items-start gap-2 text-sm"
                  >
                    <span className="text-cc-accent mt-[4px] flex-none">
                      <CheckIcon size={12} />
                    </span>
                    <span>{bullet}</span>
                  </li>
                ))}
              </ul>
            </TranscriptRow>
          </li>
        ))}
      </ol>
    </section>
  );
}

function CredentialsTriad() {
  return (
    <section aria-labelledby="team-heading" className="pb-16 sm:pb-24">
      <h2 id="team-heading" className="sr-only">
        Who you work with
      </h2>
      <TranscriptRow speaker="Advisor:">
        <p className="font-heading text-cc-heading text-h4 font-semibold">
          Who you work with.
        </p>
        <p className="text-cc-ink mt-3 max-w-2xl text-base">
          ChilliCream advisory is not a generalist consultancy that learned
          GraphQL last quarter. The engineers on the call are the ones who write
          the framework you depend on.
        </p>
        <div className="mt-8 grid gap-6 md:grid-cols-3">
          {CREDENTIALS.map((column) => (
            <CredentialColumnCard key={column.title} column={column} />
          ))}
        </div>
      </TranscriptRow>
    </section>
  );
}

function CredentialColumnCard({
  column,
}: {
  readonly column: CredentialColumn;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg flex h-full flex-col rounded-2xl border p-6">
      <h3 className="font-heading text-cc-heading text-base font-semibold">
        {column.title}
      </h3>
      <p className="text-cc-ink mt-3 text-sm leading-relaxed">{column.body}</p>
      <ul className="mt-5 flex flex-col gap-2">
        {column.bullets.map((bullet) => (
          <li
            key={bullet}
            className="text-cc-ink-dim flex items-start gap-2 text-xs"
          >
            <span className="text-cc-accent mt-[4px] flex-none">
              <CheckIcon size={12} />
            </span>
            <span>{bullet}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

function ProductsStrip() {
  const products: readonly {
    readonly name: string;
    readonly tagline: string;
  }[] = [
    {
      name: "Hot Chocolate",
      tagline: "The .NET GraphQL server we maintain.",
    },
    {
      name: "Fusion",
      tagline: "Composition at planning time, with a gateway you run yourself.",
    },
    {
      name: "Nitro",
      tagline: "The observability and CI platform for the stack.",
    },
  ];
  return (
    <section aria-labelledby="products-heading" className="pb-16 sm:pb-24">
      <h2 id="products-heading" className="sr-only">
        The products we build
      </h2>
      <TranscriptRow speaker="Stack:">
        <div className="border-cc-card-border bg-cc-card-bg grid gap-6 rounded-3xl border p-6 sm:grid-cols-3 sm:p-8">
          {products.map((product) => (
            <div key={product.name} className="flex flex-col">
              <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
                Product
              </span>
              <span className="font-heading text-cc-heading mt-2 text-lg font-semibold">
                {product.name}
              </span>
              <span className="text-cc-ink-dim mt-1 text-sm">
                {product.tagline}
              </span>
            </div>
          ))}
        </div>
      </TranscriptRow>
    </section>
  );
}

function Faq() {
  return (
    <section aria-labelledby="faq-heading" className="pb-16 sm:pb-24">
      <h2 id="faq-heading" className="sr-only">
        Frequently asked
      </h2>
      <TranscriptRow speaker="FAQ">
        <p className="font-heading text-cc-heading text-h4 mb-8 font-semibold">
          Honest answers before you book a call.
        </p>
        <dl className="grid gap-x-8 gap-y-8 md:grid-cols-2">
          {FAQ.map((item) => (
            <FaqEntry key={item.question} item={item} />
          ))}
        </dl>
      </TranscriptRow>
    </section>
  );
}

function FaqEntry({ item }: { readonly item: FaqItem }) {
  return (
    <div className="grid grid-cols-[2rem_minmax(0,1fr)] gap-x-3">
      <div>
        <span className="text-cc-accent font-mono text-xs tracking-[0.18em] uppercase">
          Q.
        </span>
      </div>
      <dt className="font-heading text-cc-heading text-base font-semibold">
        {item.question}
      </dt>
      <div>
        <span className="text-cc-nav-label mt-3 block font-mono text-xs tracking-[0.18em] uppercase">
          A.
        </span>
      </div>
      <dd className="text-cc-ink mt-3 text-sm leading-relaxed">
        {item.answer}
      </dd>
    </div>
  );
}

function ClosingBand() {
  return (
    <section aria-labelledby="contact-heading" className="pb-8">
      <h2 id="contact-heading" className="sr-only">
        Ready when you are
      </h2>
      <TranscriptRow speaker="Advisor:">
        <div className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center">
          <div>
            <p className="font-heading text-cc-heading text-h4 font-semibold">
              Ready when you are.
            </p>
            <p className="text-cc-ink mt-4 text-base">
              Book a 60-minute call. You walk us through what you are building,
              we ask the questions, and you leave with a clear next step. No
              commitment beyond the hour. If we are not the right fit, we will
              tell you on the call.
            </p>
            <ClosingSpec />
          </div>
          <div className="flex flex-col gap-3 md:items-end">
            <SolidButton href={BOOKING_URL}>Book a 60-min call</SolidButton>
            <OutlineButton href={CONSULTING_MAILTO}>Email us</OutlineButton>
          </div>
        </div>
      </TranscriptRow>
    </section>
  );
}

function ClosingSpec() {
  const items: readonly {
    readonly label: string;
    readonly value: ReactNode;
  }[] = [
    { label: "Rate", value: "$300 / hour for consulting" },
    { label: "Contracting", value: "Scoped statement of work" },
    { label: "NDA", value: "Mutual NDA on request" },
    { label: "Start", value: "Often the same week" },
  ];
  return (
    <ul className="mt-6 grid gap-3 sm:grid-cols-2">
      {items.map((item) => (
        <li key={item.label} className="flex items-start gap-3">
          <span className="text-cc-accent mt-[5px] flex-none">
            <CheckIcon />
          </span>
          <span className="text-cc-ink text-sm">
            <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
              {item.label}
            </span>
            <br />
            {item.value}
          </span>
        </li>
      ))}
    </ul>
  );
}
