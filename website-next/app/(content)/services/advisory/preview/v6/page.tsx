import type { Metadata } from "next";
import type { ReactNode } from "react";

import { CheckIcon } from "@/src/components/CheckIcon";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { CoffeeTray } from "@/src/icons/CoffeeTray";
import { DripBrewer } from "@/src/icons/DripBrewer";
import { Espresso } from "@/src/icons/Espresso";
import { FrenchPress } from "@/src/icons/FrenchPress";
import { PourOver } from "@/src/icons/PourOver";

export const metadata: Metadata = {
  title: "GraphQL Advisory Consulting by ChilliCream",
  description:
    "GraphQL advisory consulting at $300 per hour, or scoped contracting, pulled by the engineers behind Hot Chocolate, Fusion, and Nitro. Reserve a seat at the bar.",
  keywords: [
    "GraphQL advisory consulting",
    "GraphQL consulting",
    "GraphQL contracting",
    "Hot Chocolate consulting",
    "Fusion consulting",
    "Nitro consulting",
    "ChilliCream advisory",
  ],
  openGraph: {
    title: "GraphQL Advisory Consulting by ChilliCream",
    description:
      "GraphQL advisory consulting at $300 per hour, or scoped contracting, from the engineers behind Hot Chocolate, Fusion, and Nitro.",
  },
  robots: { index: false, follow: false },
};

const BOOKING_URL = "https://calendly.com/chillicream/60min";
const CONSULTING_MAILTO = "mailto:contact@chillicream.com?subject=Consulting";
const CONTRACTING_MAILTO = "mailto:contact@chillicream.com?subject=Contracting";

const RESERVE_LABEL = "Reserve a seat at the bar";
const EMAIL_LABEL = "Email the bar";

interface Tier {
  readonly id: "consulting" | "contracting";
  readonly eyebrow: string;
  readonly name: string;
  readonly brewStyle: string;
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
    eyebrow: "Single shot, on the hour",
    name: "Espresso",
    brewStyle: "Consulting",
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
    eyebrow: "Slow, scoped, deliberate",
    name: "Pour Over",
    brewStyle: "Contracting",
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
  readonly label: string;
  readonly title: string;
  readonly description: string;
  readonly bullets: readonly string[];
  readonly icon: ReactNode;
}

const ENGAGEMENT_STEPS: readonly EngagementStep[] = [
  {
    index: "01",
    label: "Order",
    title: "Introductory call",
    description:
      "A 60-minute working call. You walk us through the system, the goal, and the constraints. We ask the hard questions and tell you whether we are the right fit.",
    bullets: [
      "Senior engineer, no sales handoff",
      "Mutual NDA available on request",
      "Written recap with next steps",
    ],
    icon: <DripBrewer className="text-cc-accent h-10 w-10" />,
  },
  {
    index: "02",
    label: "Ticket",
    title: "Proposal",
    description:
      "A written proposal that matches your need: hourly retainer for consulting, or a scoped statement of work for contracting with deliverables, milestones, and a target timeline.",
    bullets: [
      "Hourly retainer or fixed scope",
      "Clear deliverables and milestones",
      "Written, scoped, and signable",
    ],
    icon: <CoffeeTray className="text-cc-accent h-10 w-10" />,
  },
  {
    index: "03",
    label: "Pull",
    title: "Kickoff",
    description:
      "Contract signed, channel opened, work starts. You get a direct line to the engineers doing the work, a shared backlog, and a weekly checkpoint.",
    bullets: [
      "Shared Slack or Teams channel",
      "Weekly checkpoint and written status",
      "Direct access to the engineers doing the work",
    ],
    icon: <FrenchPress className="text-cc-accent h-10 w-10" />,
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

const QC_DELIVERABLES: readonly string[] = [
  "Architecture decision records, written down and shareable.",
  "Schema review with line-level comments in your repo.",
  "Working proof of concept code you can keep and run.",
  "Production-ready implementations, embedded in your build.",
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

export default function AdvisoryPreviewV6Page() {
  return (
    <>
      <Hero />
      <TodaysMenu />
      <HowWePullIt />
      <BehindTheBar />
      <QualityControl />
      <Faq />
      <ReadyToOrder />
    </>
  );
}

function SteamingCupMark() {
  return (
    <svg
      viewBox="0 0 56 56"
      aria-hidden="true"
      className="text-cc-accent mx-auto mb-4 h-9 w-9"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.6}
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M22 8 C 20 12, 24 14, 22 18" opacity="0.7" />
      <path d="M28 6 C 26 10, 30 12, 28 16" opacity="0.9" />
      <path d="M34 8 C 32 12, 36 14, 34 18" opacity="0.7" />
      <path d="M14 24 H 40 V 36 a 8 8 0 0 1 -8 8 H 22 a 8 8 0 0 1 -8 -8 Z" />
      <path d="M40 28 h 4 a 4 4 0 0 1 0 8 h -4" />
    </svg>
  );
}

function Hero() {
  return (
    <section className="pt-10 pb-12 text-center sm:pt-16 sm:pb-16">
      <SteamingCupMark />
      <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
        On the menu today
      </p>
      <h1 className="font-heading text-cc-heading text-h2 sm:text-h1 mt-5 font-semibold tracking-tight">
        Talk to the engineers who built your GraphQL stack.
      </h1>
      <p className="text-cc-ink mx-auto mt-6 max-w-2xl text-base text-pretty sm:text-lg">
        Hourly consulting at $300 per hour, or full contracting engagements,
        delivered by the team behind Hot Chocolate, Fusion, and Nitro. Bring a
        question, a design, or a deadline. We meet you where the work is.
      </p>
      <div className="mt-9 flex flex-wrap items-center justify-center gap-3">
        <SolidButton href={BOOKING_URL}>{RESERVE_LABEL}</SolidButton>
        <OutlineButton href={CONSULTING_MAILTO}>{EMAIL_LABEL}</OutlineButton>
      </div>
      <HeroStats />
    </section>
  );
}

function HeroStats() {
  const items = [
    { label: "Hourly rate", value: "$300" },
    { label: "Intro call", value: "60 min" },
    { label: "Engagements", value: "2 tiers" },
  ];
  return (
    <dl className="border-cc-card-border bg-cc-card-border mx-auto mt-12 grid max-w-2xl grid-cols-3 gap-px overflow-hidden rounded-2xl border">
      {items.map((item) => (
        <div
          key={item.label}
          className="bg-cc-surface px-4 py-5 text-center sm:px-6"
        >
          <dt className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            {item.label}
          </dt>
          <dd className="font-heading text-cc-heading mt-2 text-xl font-semibold sm:text-2xl">
            {item.value}
          </dd>
        </div>
      ))}
    </dl>
  );
}

function TodaysMenu() {
  return (
    <section aria-labelledby="menu-heading" className="pt-6 pb-16 sm:pb-24">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Today&apos;s menu
        </p>
        <h2
          id="menu-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Two ways to order.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          Pulled by the same engineers, served two ways. Pick the pour that
          matches the question you are bringing in.
        </p>
      </div>

      <div className="mt-10 grid gap-6 lg:grid-cols-2 lg:items-stretch">
        {TIERS.map((tier) => (
          <TierCard key={tier.id} tier={tier} />
        ))}
      </div>
    </section>
  );
}

function TierCard({ tier }: { readonly tier: Tier }) {
  if (tier.highlight) {
    return (
      <div
        className="relative isolate rounded-3xl p-[1.5px]"
        style={{
          background:
            "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)",
        }}
      >
        <HousePourPill />
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

function TierIcon({ tierId }: { readonly tierId: Tier["id"] }) {
  if (tierId === "consulting") {
    return (
      <span className="text-cc-accent inline-flex h-12 w-12 items-center justify-center">
        <Espresso className="h-12 w-12" />
      </span>
    );
  }
  return (
    <span className="text-cc-accent inline-flex h-12 w-12 items-center justify-center">
      <PourOver className="h-12 w-12" />
    </span>
  );
}

function TierCardBody({ tier }: { readonly tier: Tier }) {
  return (
    <>
      <div className="flex items-center justify-between gap-4">
        <div>
          <p className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            {tier.eyebrow}
          </p>
          <h3 className="font-heading text-cc-heading text-h3 mt-3 font-semibold">
            {tier.name}
          </h3>
          <p className="text-cc-nav-label mt-1 font-mono text-[0.65rem] tracking-[0.18em] uppercase">
            {tier.brewStyle}
          </p>
        </div>
        <TierIcon tierId={tier.id} />
      </div>

      <p className="text-cc-ink-dim mt-4 text-sm leading-relaxed sm:text-base">
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

function HousePourPill() {
  return (
    <span className="bg-cc-surface text-cc-accent border-cc-accent absolute top-0 left-9 z-10 -translate-y-1/2 rounded-full border px-4 py-1 font-mono text-[0.65rem] tracking-[0.18em] whitespace-nowrap uppercase">
      House pour
    </span>
  );
}

function HowWePullIt() {
  return (
    <section
      aria-labelledby="engagement-heading"
      className="border-cc-card-border bg-cc-card-bg/40 rounded-3xl border p-6 sm:p-10"
    >
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          How we pull it
        </p>
        <h2
          id="engagement-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          From first call to first commit in three steps.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          No long sales cycle. You speak to an engineer, you get a written
          proposal, you kick off.
        </p>
      </div>

      <ol className="mt-10 grid gap-6 md:grid-cols-3">
        {ENGAGEMENT_STEPS.map((step) => (
          <EngagementCard key={step.index} step={step} />
        ))}
      </ol>
    </section>
  );
}

function EngagementCard({ step }: { readonly step: EngagementStep }) {
  return (
    <li className="bg-cc-surface border-cc-card-border relative flex h-full flex-col rounded-2xl border p-6">
      <div className="flex items-start justify-between gap-4">
        <span className="text-cc-accent font-mono text-xs tracking-[0.18em] uppercase">
          {step.label} / Step {step.index}
        </span>
        {step.icon}
      </div>
      <h3 className="font-heading text-cc-heading mt-3 text-lg font-semibold">
        {step.title}
      </h3>
      <p className="text-cc-ink mt-3 text-sm leading-relaxed">
        {step.description}
      </p>
      <ul className="mt-5 flex flex-col gap-2">
        {step.bullets.map((bullet) => (
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
    </li>
  );
}

function BehindTheBar() {
  return (
    <section aria-labelledby="team-heading" className="mt-20 sm:mt-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Behind the bar
        </p>
        <h2
          id="team-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          The team behind Hot Chocolate, Fusion, and Nitro.
        </h2>
        <p className="text-cc-ink mx-auto mt-4 max-w-2xl text-base">
          ChilliCream advisory is not a generalist consultancy that learned
          GraphQL last quarter. The engineers on the call are the ones who write
          the framework you depend on.
        </p>
      </div>

      <div className="mt-10 grid gap-6 md:grid-cols-3">
        {CREDENTIALS.map((column) => (
          <CredentialColumnCard key={column.title} column={column} />
        ))}
      </div>

      <div className="border-cc-card-border bg-cc-card-bg/60 mt-10 grid gap-6 rounded-3xl border p-6 sm:grid-cols-3 sm:p-8">
        <ProductBadge
          name="Hot Chocolate"
          tagline="The .NET GraphQL server we maintain."
        />
        <ProductBadge
          name="Fusion"
          tagline="Federation and composition for GraphQL subgraphs."
        />
        <ProductBadge
          name="Nitro"
          tagline="The observability and CI platform for the stack."
        />
      </div>
    </section>
  );
}

function CredentialColumnCard({
  column,
}: {
  readonly column: CredentialColumn;
}) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 flex h-full flex-col rounded-2xl border p-6">
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

function ProductBadge({
  name,
  tagline,
}: {
  readonly name: string;
  readonly tagline: string;
}) {
  return (
    <div className="flex flex-col">
      <span className="text-cc-nav-label font-mono text-[0.65rem] tracking-[0.18em] uppercase">
        Product
      </span>
      <span className="font-heading text-cc-heading mt-2 text-lg font-semibold">
        {name}
      </span>
      <span className="text-cc-ink-dim mt-1 text-sm">{tagline}</span>
    </div>
  );
}

function QualityControl() {
  return (
    <section aria-labelledby="qc-heading" className="mt-20 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/60 rounded-3xl border p-6 sm:p-10">
        <div className="flex flex-col gap-6 md:flex-row md:items-start md:gap-10">
          <div className="md:w-1/3">
            <div className="flex items-center gap-4">
              <span className="text-cc-accent inline-flex h-10 w-10 flex-none items-center justify-center">
                <CoffeeTray className="h-10 w-10" />
              </span>
              <div>
                <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
                  Quality control on the bar
                </p>
                <h2
                  id="qc-heading"
                  className="font-heading text-cc-heading mt-2 text-lg font-semibold sm:text-xl"
                >
                  What we actually deliver.
                </h2>
              </div>
            </div>
            <p className="text-cc-ink mt-4 text-sm leading-relaxed">
              Every engagement ends with something you can run, read, or merge.
              We do not bill for slideware, and we do not leave with code only
              we can maintain.
            </p>
          </div>

          <ul className="grid flex-1 gap-3 sm:grid-cols-2">
            {QC_DELIVERABLES.map((item) => (
              <li
                key={item}
                className="border-cc-card-border bg-cc-surface flex items-start gap-3 rounded-2xl border p-4"
              >
                <span className="text-cc-accent mt-[5px] flex-none">
                  <CheckIcon />
                </span>
                <span className="text-cc-ink text-sm leading-relaxed">
                  {item}
                </span>
              </li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
}

function Faq() {
  return (
    <section aria-labelledby="faq-heading" className="mt-20 sm:mt-28">
      <div className="text-center">
        <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
          Ask the barista
        </p>
        <h2
          id="faq-heading"
          className="font-heading text-cc-heading text-h4 sm:text-h3 mt-3 font-semibold"
        >
          Honest answers before you book a call.
        </h2>
      </div>

      <dl className="mt-10 grid gap-4 md:grid-cols-2">
        {FAQ.map((item) => (
          <FaqEntry key={item.question} item={item} />
        ))}
      </dl>
    </section>
  );
}

function FaqEntry({ item }: { readonly item: FaqItem }) {
  return (
    <div className="border-cc-card-border bg-cc-card-bg/60 rounded-2xl border p-6">
      <dt className="font-heading text-cc-heading text-base font-semibold">
        {item.question}
      </dt>
      <dd className="text-cc-ink mt-3 text-sm leading-relaxed">
        {item.answer}
      </dd>
    </div>
  );
}

function ReadyToOrder() {
  return (
    <section aria-labelledby="contact-heading" className="mt-20 mb-8 sm:mt-28">
      <div className="border-cc-card-border bg-cc-card-bg/70 grid gap-8 rounded-3xl border p-8 sm:p-12 md:grid-cols-[1.4fr_1fr] md:items-center">
        <div>
          <p className="text-cc-nav-label font-mono text-xs tracking-[0.18em] uppercase">
            Ready when you are
          </p>
          <h2
            id="contact-heading"
            className="font-heading text-cc-heading text-h4 mt-3 font-semibold"
          >
            One call is usually enough to know.
          </h2>
          <p className="text-cc-ink mt-4 text-base">
            Book a 60-minute call. You walk us through what you are building, we
            ask the questions, and you leave with a clear next step. No
            commitment beyond the hour. If we are not the right fit, we will
            tell you on the call.
          </p>
          <ContactSpec />
        </div>
        <div className="flex flex-col gap-3 md:items-end">
          <SolidButton href={BOOKING_URL}>{RESERVE_LABEL}</SolidButton>
          <OutlineButton href={CONSULTING_MAILTO}>{EMAIL_LABEL}</OutlineButton>
        </div>
      </div>
    </section>
  );
}

function ContactSpec() {
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
